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
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.Job.BlockchainCashinDetector.Core.Services.BLockchains;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Lykke.Service.Assets.Client;
using Lykke.Service.BlockchainApi.Client;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.PeriodicalHandlers
{
    [UsedImplicitly]
    public class DepositWalletsBalanceProcessingPeriodicalHandler : 
        TimerPeriod,
        IDepositWalletsBalanceProcessingPeriodicalHandler
    {
        private readonly int _batchSize;
        private readonly string _blockchainType;
        private readonly IBlockchainApiClient _blockchainApiClient;
        private readonly ICqrsEngine _cqrsEngine;
        private readonly IAssetsServiceWithCache _assetsService;
        private readonly IEnrolledBalanceRepository _enrolledBalanceRepository;


        public DepositWalletsBalanceProcessingPeriodicalHandler(
            ILog log, 
            TimeSpan period, 
            int batchSize, 
            string blockchainType,
            IBlockchainApiClientProvider blockchainApiClientProvider,
            ICqrsEngine cqrsEngine, 
            IAssetsServiceWithCache assetsService,
            IEnrolledBalanceRepository enrolledBalanceRepository) :

            base(
                nameof(DepositWalletsBalanceProcessingPeriodicalHandler), 
                (int)period.TotalMilliseconds, 
                log.CreateComponentScope($"{nameof(DepositWalletsBalanceProcessingPeriodicalHandler)} : {blockchainType}"))
        {
            _batchSize = batchSize;
            _blockchainType = blockchainType;
            _blockchainApiClient = blockchainApiClientProvider.Get(blockchainType);
            _cqrsEngine = cqrsEngine;
            _assetsService = assetsService;
            _enrolledBalanceRepository = enrolledBalanceRepository;
        }

        public override async Task Execute()
        {
            var assets = (await _assetsService.GetAllAssetsAsync(false))
                .Where(a => a.BlockchainIntegrationLayerId == _blockchainType)
                .ToDictionary(
                    a => a.BlockchainIntegrationLayerAssetId, 
                    a => a);
            var warningAssets = new HashSet<string>();

            var stopwatch = Stopwatch.StartNew();
            var wallets = new HashSet<string>();

            var blockchainAssets = await _blockchainApiClient.GetAllAssetsAsync(_batchSize);
            var tooSmallBalanceWalletsCount = 0;
            
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
                    var walletKeys = batch.Select(x => new DepositWalletKey
                    (
                        blockchainAssetId: x.AssetId,
                        blockchainType: _blockchainType,
                        depositWalletAddress: x.Address
                    ));

                    var enrolledBalances = (await _enrolledBalanceRepository.GetAsync(walletKeys))
                        .ToDictionary(x => x.DepositWalletAddress, y => new { y.Balance, y.Block });

                    foreach (var balance in batch)
                    {
                        if (!assets.TryGetValue(balance.AssetId, out var asset))
                        {
                            if (!warningAssets.Contains(balance.AssetId))
                            {
                                Log.WriteWarning(nameof(Execute), balance, "Lykke asset for the blockchain asset is not found");

                                warningAssets.Add(balance.AssetId);
                            }
                        }

                        if (asset != null && balance.Balance < (decimal)asset.CashinMinimalAmount)
                        {
                            ++tooSmallBalanceWalletsCount;
                        }

                        if (enrolledBalances.TryGetValue(balance.Address, out var enrolledBalance))
                        {
                            if (balance.Block < enrolledBalance.Block)
                            {
                                // We are not sure, that balance is actual
                                continue;
                            }

                            if (balance.Balance - enrolledBalance.Balance <= 0)
                            {
                                // Nothing to transfer
                                continue;
                            }
                        }
                        
                        if (asset != null)
                        {
                            _cqrsEngine.SendCommand
                            (
                                new DetectDepositBalanceCommand
                                {
                                    AssetId = asset.Id,
                                    AssetAccuracy = asset.Accuracy,
                                    BalanceAmount = balance.Balance,
                                    BalanceBlock = balance.Block,
                                    BlockchainAssetId = balance.AssetId,
                                    BlockchainType = _blockchainType,
                                    CashinMinimalAmount = (decimal) asset.CashinMinimalAmount,
                                    DepositWalletAddress = balance.Address
                                },
                                BlockchainCashinDetectorBoundedContext.Name,
                                BlockchainCashinDetectorBoundedContext.Name
                            );
                        }

                        wallets.Add(balance.Address);
                    }

                    return true;
                });

            if (statistics.ItemsCount > 0)
            {
                Log.WriteInfo(nameof(Execute), new
                {
                    balancesCount = statistics.ItemsCount,
                    walletsCount = wallets.Count,
                    tooSmallBalancesCount = tooSmallBalanceWalletsCount,
                    batchesCount = statistics.BatchesCount,
                    processingElapsed = statistics.Elapsed,
                    totalElapsed = stopwatch.Elapsed
                }, "Positive balance on the deposit wallets is detected");
            }
        }
    }
}
