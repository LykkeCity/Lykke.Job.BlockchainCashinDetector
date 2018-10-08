﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Contract;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.Job.BlockchainCashinDetector.Core.Services.BLockchains;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.BlockchainApi.Client;
using Lykke.Service.BlockchainApi.Client.Models;
using Lykke.Service.BlockchainWallets.Client;
using Lykke.Service.BlockchainWallets.Contract;

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
        private readonly IReadOnlyDictionary<string, Asset> _assets;
        private readonly HashSet<string> _warningAssets;

        private IReadOnlyDictionary<string, BlockchainAsset> _blockchainAssets;
        private readonly IBlockchainWalletsClient _blockchainWalletsClient;

        public BalanceProcessor(
            string blockchainType,
            ILogFactory logFactory, 
            IHotWalletsProvider hotWalletsProvider,
            IBlockchainApiClient blockchainApiClient,
            ICqrsEngine cqrsEngine,
            IEnrolledBalanceRepository enrolledBalanceRepository,
            IReadOnlyDictionary<string, Asset> assets,
            IReadOnlyDictionary<string, BlockchainAsset> blockchainAssets,
            IBlockchainWalletsClient blockchainWalletsClient)
        {
            _blockchainType = blockchainType;
            _log = logFactory.CreateLog(this);
            _hotWalletAddress = hotWalletsProvider.GetHotWalletAddress(blockchainType);
            _blockchainApiClient = blockchainApiClient;
            _cqrsEngine = cqrsEngine;
            _enrolledBalanceRepository = enrolledBalanceRepository;
            _assets = assets;
            _blockchainAssets = blockchainAssets;
            _blockchainWalletsClient = blockchainWalletsClient;

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
                await ProcessBalance(balance, enrolledBalances);
            }
        }

        private async Task ProcessBalance(
            WalletBalance depositWallet,
            IReadOnlyDictionary<string, EnrolledBalance> enrolledBalances)
        {
            if (!_assets.TryGetValue(depositWallet.AssetId, out var asset))
            {
                if (!_warningAssets.Contains(depositWallet.AssetId))
                {
                    _log.WriteWarning(nameof(ProcessBalance), depositWallet,
                        "Lykke asset for the blockchain asset is not found");

                    _warningAssets.Add(depositWallet.AssetId);
                }

                return;
            }

            enrolledBalances.TryGetValue(GetEnrolledBalancesDictionaryKey(depositWallet.Address, depositWallet.AssetId), out var enrolledBalance);

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

            var wallet = await _blockchainWalletsClient.GetWalletAsync(_blockchainType, depositWallet.Address);

            if (wallet == null)
            {
                _log.WriteWarning(nameof(ProcessBalance), depositWallet,
                    "Blockchain wallet was not found, but deposit was detected");

                return;
            }

            _cqrsEngine.SendCommand
            (
                new LockDepositWalletCommand
                {
                    BlockchainType = _blockchainType,
                    BlockchainAssetId = depositWallet.AssetId,
                    DepositWalletAddress = depositWallet.Address,
                    DepositWalletBalance = depositWallet.Balance,
                    DepositWalletBlock = depositWallet.Block,
                    AssetId = asset.Id,
                    AssetAccuracy = asset.Accuracy,
                    CashinMinimalAmount = (decimal)asset.CashinMinimalAmount,
                    HotWalletAddress = _hotWalletAddress
                },
                BlockchainCashinDetectorBoundedContext.Name,
                BlockchainCashinDetectorBoundedContext.Name
            );
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
                    x => GetEnrolledBalancesDictionaryKey(x.Key.DepositWalletAddress, x.Key.BlockchainAssetId),
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

        private string GetEnrolledBalancesDictionaryKey(string address, string assetId)
        {
            return $"{address}:{assetId}";
        }
    }
}
