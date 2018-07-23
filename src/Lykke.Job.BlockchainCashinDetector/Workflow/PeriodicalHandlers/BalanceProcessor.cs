using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Contract;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.Job.BlockchainCashinDetector.Core.Services.BLockchains;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.BlockchainApi.Client;
using Lykke.Service.BlockchainApi.Client.Models;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.PeriodicalHandlers
{
    public class BalanceProcessor
    {
        private readonly string _blockchainType;
        private readonly ILog _log;
        private readonly string _hotWalletAddress;
        private readonly IBlockchainApiClient _blockchainApiClient;
        private readonly ICqrsEngine _cqrsEngine;
        private readonly IEnrolledBalanceRepository _enrolledBalanceRepository;
        private readonly ICashinRepository _cashinRepository;
        private readonly IDepositWalletLockRepository _depositWalletLockRepository;
        private readonly IChaosKitty _chaosKitty;
        private readonly IReadOnlyDictionary<string, Asset> _assets;
        private readonly HashSet<string> _warningAssets;

        private IReadOnlyDictionary<string, BlockchainAsset> _blockchainAssets;

        public BalanceProcessor(
            string blockchainType,
            ILog log, 
            IHotWalletsProvider hotWalletsProvider,
            IBlockchainApiClient blockchainApiClient,
            ICqrsEngine cqrsEngine,
            IEnrolledBalanceRepository enrolledBalanceRepository,
            ICashinRepository cashinRepository,
            IDepositWalletLockRepository depositWalletLockRepository,
            IChaosKitty chaosKitty,
            IReadOnlyDictionary<string, Asset> assets,
            IReadOnlyDictionary<string, BlockchainAsset> blockchainAssets)
        {
            _blockchainType = blockchainType;
            _log = log;
            _hotWalletAddress = hotWalletsProvider.GetHotWalletAddress(blockchainType);
            _blockchainApiClient = blockchainApiClient;
            _cqrsEngine = cqrsEngine;
            _enrolledBalanceRepository = enrolledBalanceRepository;
            _cashinRepository = cashinRepository;
            _depositWalletLockRepository = depositWalletLockRepository;
            _chaosKitty = chaosKitty;
            _assets = assets;
            _blockchainAssets = blockchainAssets;

            _warningAssets = new HashSet<string>();
        }

        public Task ProcessAsync(int batchSize)
        {
            return _blockchainApiClient.EnumerateWalletBalanceBatchesAsync(
                batchSize,
                assetId => GetAssetAccuracy(assetId, batchSize),
                async batch => 
                {
                    await ProcessBalancesBatchAsync(batch);
                    return true;
                });
        }

        private async Task ProcessBalancesBatchAsync(IReadOnlyList<WalletBalance> batch)
        {
            var enrolledBalances = await GetEnrolledBalancesAsync(batch);

            foreach (var balance in batch)
            {
                await ProcessBalanceAsync(balance, enrolledBalances);
            }
        }

        private async Task ProcessBalanceAsync(
            WalletBalance depositWallet,
            IReadOnlyDictionary<string, EnrolledBalance> enrolledBalances)
        {
            if (!_assets.TryGetValue(depositWallet.AssetId, out var asset))
            {
                if (!_warningAssets.Contains(depositWallet.AssetId))
                {
                    _log.WriteWarning(nameof(ProcessBalanceAsync), depositWallet, "Lykke asset for the blockchain asset is not found");

                    _warningAssets.Add(depositWallet.AssetId);
                }

                return;
            }

            enrolledBalances.TryGetValue(depositWallet.Address, out var enrolledBalance);

            var cashinCouldBeStarted = CashinAggregate.CouldBeStarted(
                depositWallet.Balance,
                depositWallet.Block,
                enrolledBalance?.Balance ?? 0,
                enrolledBalance?.Block ?? 0,
                asset.Accuracy);

            if (!cashinCouldBeStarted)
            {
                return;
            }

            var operationId = await _depositWalletLockRepository.LockAsync(
                _blockchainType, 
                depositWallet.Address, 
                depositWallet.AssetId, 
                CashinAggregate.GetNextId);

            _chaosKitty.Meow(operationId);

            var aggregate = await _cashinRepository.GetOrAddAsync
            (
                _blockchainType,
                depositWallet.Address,
                depositWallet.AssetId,
                operationId,
                () => CashinAggregate.WaitForActualBalance
                (
                    operationId: operationId,
                    assetId: asset.Id,
                    assetAccuracy: asset.Accuracy,
                    blockchainAssetId: depositWallet.AssetId,
                    blockchainType: _blockchainType,
                    cashinMinimalAmount: (decimal)asset.CashinMinimalAmount,
                    depositWalletAddress: depositWallet.Address,
                    hotWalletAddress: _hotWalletAddress
                )
            );

            var isCashinStarted = aggregate.Start
            (
                balanceAmount: depositWallet.Balance,
                balanceBlock: depositWallet.Block,
                enrolledBalanceAmount: enrolledBalance?.Balance ?? 0,
                enrolledBalanceBlock: enrolledBalance?.Block ?? 0
            );

            if (isCashinStarted)
            {
                if (!aggregate.MeAmount.HasValue)
                {
                    throw new InvalidOperationException("ME operation amount should be not null here");
                }

                _cqrsEngine.SendCommand
                (
                    new EnrollToMatchingEngineCommand
                    {
                        AssetId = aggregate.AssetId,
                        BlockchainAssetId = aggregate.BlockchainAssetId,
                        BlockchainType = aggregate.BlockchainType,
                        DepositWalletAddress = aggregate.DepositWalletAddress,
                        OperationId = aggregate.OperationId,
                        MatchingEngineOperationAmount = aggregate.MeAmount.Value
                    },
                    BlockchainCashinDetectorBoundedContext.Name,
                    BlockchainCashinDetectorBoundedContext.Name
                );

                _chaosKitty.Meow(aggregate.OperationId);

                await _cashinRepository.SaveAsync(aggregate);
            }
        }

        private async Task<IReadOnlyDictionary<string, EnrolledBalance>> GetEnrolledBalancesAsync(IEnumerable<WalletBalance> balances)
        {
            var walletKeys = balances.Select(x => new DepositWalletKey
            (
                blockchainAssetId: x.AssetId,
                blockchainType: _blockchainType,
                depositWalletAddress: x.Address
            ));

            return (await _enrolledBalanceRepository.GetAsync(walletKeys))
                .ToDictionary(
                    x => x.DepositWalletAddress,
                    y => y);
        }

        private int GetAssetAccuracy(string assetId, int batchSize)
        {
            if (!_blockchainAssets.TryGetValue(assetId, out var asset))
            {
                // Unknown asset, tries to refresh cached assets

                _blockchainAssets = _blockchainApiClient
                    .GetAllAssetsAsync(batchSize)
                    .GetAwaiter()
                    .GetResult();

                if (!_blockchainAssets.TryGetValue(assetId, out asset))
                {
                    throw new InvalidOperationException($"Asset {assetId} not found");
                }
            }

            return asset.Accuracy;
        }

    }
}
