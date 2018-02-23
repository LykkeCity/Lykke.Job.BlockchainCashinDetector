using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.AzureRepositories;
using Lykke.Job.BlockchainCashinDetector.Contract;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.Job.BlockchainCashinDetector.Core.Services.BLockchains;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Lykke.Service.Assets.Client;
using Lykke.Service.BlockchainApi.Client;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.PeriodicalHandlers
{
    [UsedImplicitly]
    public class DepositWalletsBalanceProcessingPeriodicalHandler : TimerPeriod
    {
        private readonly int _batchSize;
        private readonly string _blockchainType;
        private readonly IBlockchainApiClient _blockchainApiClient;
        private readonly ICqrsEngine _cqrsEngine;
        private readonly IAssetsServiceWithCache _assetsService;
        private readonly IDepositBalanceDetectionsDeduplicationRepository _deduplicationRepository;

        public DepositWalletsBalanceProcessingPeriodicalHandler(
            ILog log, 
            TimeSpan period, 
            int batchSize, 
            string blockchainType,
            IBlockchainApiClientProvider blockchainApiClientProvider,
            ICqrsEngine cqrsEngine, 
            IAssetsServiceWithCache assetsService,
            IDepositBalanceDetectionsDeduplicationRepository deduplicationRepository) :

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
            _deduplicationRepository = deduplicationRepository;
        }

        public override async Task Execute()
        {
            var assets = (await _assetsService.GetAllAssetsAsync(false))
                .Where(a => a.BlockchainIntegrationLayerId == _blockchainType)
                .ToDictionary(
                    a => a.BlockchainIntegrationLayerAssetId, 
                    a => a);

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

                    var deduplicationLocks = (await _deduplicationRepository.GetAsync(walletKeys))
                        .ToDictionary(x => x.DepositWalletAddress, y => y.Block);

                    foreach (var balance in batch)
                    {
                        if (deduplicationLocks.TryGetValue(balance.Address, out var latestTransactionBlock) && latestTransactionBlock >= balance.Block)
                        {
                            continue;
                        }
                        
                        if (assets.TryGetValue(balance.AssetId, out var asset))
                        {
                            if (balance.Balance < (decimal)asset.CashinMinimalAmount)
                            {
                                ++tooSmallBalanceWalletsCount;
                            }
                            else
                            {
                                // Enough balance on the deposit wallet is detected, sends command to let the cashin saga
                                // detect it.

                                _cqrsEngine.SendCommand(
                                    new DetectDepositBalanceCommand
                                    {
                                        BlockchainType = _blockchainType,
                                        Amount = balance.Balance,
                                        DepositWalletAddress = balance.Address,
                                        BlockchainAssetId = balance.AssetId,
                                        AssetId = asset.Id
                                    },
                                    BlockchainCashinDetectorBoundedContext.Name,
                                    BlockchainCashinDetectorBoundedContext.Name);
                            }
                        }
                        else
                        {
                            Log.WriteWarning(nameof(Execute), balance, "Lykke asset for the blockchain asset is not found");
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
