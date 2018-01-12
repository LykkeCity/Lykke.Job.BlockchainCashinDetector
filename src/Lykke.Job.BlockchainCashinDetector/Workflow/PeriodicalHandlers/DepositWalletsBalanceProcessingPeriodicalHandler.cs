using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Contract;
using Lykke.Job.BlockchainCashinDetector.Core.Domain.Cashin;
using Lykke.Job.BlockchainCashinDetector.Core.Domain.Cashin.Commands;
using Lykke.Service.BlockchainApi.Client;
using Lykke.Service.BlockchainApi.Client.Models;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.PeriodicalHandlers
{
    [UsedImplicitly]
    public class DepositWalletsBalanceProcessingPeriodicalHandler : TimerPeriod
    {
        private readonly int _batchSize;
        private readonly string _blockchainType;
        private readonly IBlockchainApiClient _blockchainApiClient;
        private readonly IActiveCashinRepository _activeCashinRepository;
        private readonly ICqrsEngine _cqrsEngine;

        public DepositWalletsBalanceProcessingPeriodicalHandler(
            ILog log, 
            TimeSpan period, 
            int batchSize, 
            string blockchainType,
            IBlockchainApiClient blockchainApiClient,
            IActiveCashinRepository activeCashinRepository,
            ICqrsEngine cqrsEngine) :

            base(
                nameof(DepositWalletsBalanceProcessingPeriodicalHandler), 
                (int)period.TotalMilliseconds, 
                log.CreateComponentScope($"{nameof(DepositWalletsBalanceProcessingPeriodicalHandler)} : {blockchainType}"))
        {
            _batchSize = batchSize;
            _blockchainType = blockchainType;
            _blockchainApiClient = blockchainApiClient;
            _activeCashinRepository = activeCashinRepository;
            _cqrsEngine = cqrsEngine;
        }

        public override async Task Execute()
        {
            Log.WriteInfo(nameof(Execute), "", "Detecting cashin (0 -> Deposit Wallet)...");

            var stopwatch = Stopwatch.StartNew();
            var wallets = new HashSet<string>();
            var detectedCashinsCount = 0;

            var blockchainAssets = await _blockchainApiClient.GetAllAssetsAsync(_batchSize);
            
            var statistics = await _blockchainApiClient.EnumerateWalletBalanceBatchesAsync(
                _batchSize,
                assetId =>
                {
                    if (!blockchainAssets.TryGetValue(assetId, out var asset))
                    {
                        // Unknown asset, tries to refresh cached assets

                        blockchainAssets = _blockchainApiClient
                            .GetAllAssetsAsync(_batchSize)
                            .GetAwaiter()
                            .GetResult();

                        if (!blockchainAssets.TryGetValue(assetId, out asset))
                        {
                            throw new InvalidOperationException($"Asset {assetId} not found");
                        }
                    }

                    return asset.Accuracy;
                },
                async batch =>
                {
                    var cashinDetectionTasks = batch
                        .Select(TryToDetectCashin)
                        .ToList();

                    foreach (var balance in batch)
                    {
                        wallets.Add(balance.Address);
                    }

                    var detectionResults = await Task.WhenAll(cashinDetectionTasks);

                    detectedCashinsCount += detectionResults.Count(r => r);

                    return true;
                });

            Log.WriteInfo(nameof(Execute), new
            {
                balancesCount = statistics.ItemsCount,
                walletsCount = wallets.Count,
                batchesCount = statistics.BatchesCount,
                detectedCashinsCount,
                processingElapsed = statistics.Elapsed,
                totalElapsed = stopwatch.Elapsed
            }, "Done");
        }

        private async Task<bool> TryToDetectCashin(WalletBalance balance)
        {
            // Atomically gets operation ID of the existing active cashin,
            // or adds new active cashin 

            var operationId = await _activeCashinRepository.GetOrAdd(
                _blockchainType,
                balance.Address,
                balance.AssetId,
                Guid.NewGuid);

            ChaosKitty.Meow();

            // Cashin detected, sends command to enroll the cashin to the ME.
            // This command will be sended while balance is non zero.
            // The main thing here, is the same operationID for the same balance instance.
            // This allows to deduplicate the commands in the handler

            _cqrsEngine.SendCommand(
                new EnrollToMatchingEngineCommand
                {
                    OperationId = operationId,
                    BlockchainType = _blockchainType,
                    Amount = balance.Balance,
                    BlockchainDepositWalletAddress = balance.Address,
                    BlockchainAssetId = balance.AssetId
                },
                BlockchainCashinDetectorBoundedContext.Name,
                BlockchainCashinDetectorBoundedContext.Name);

            ChaosKitty.Meow();

            return true;
        }
    }
}
