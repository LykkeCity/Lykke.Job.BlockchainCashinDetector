using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.SettingsReader;

namespace Lykke.Job.BlockchainCashinDetector.AzureRepositories
{
    public class DepositBalanceDetectionsDeduplicationRepository : IDepositBalanceDetectionsDeduplicationRepository
    {
        private readonly INoSQLTableStorage<DepositBalanceDetectionsDeduplicationEntity> _storage;

        public static IDepositBalanceDetectionsDeduplicationRepository Create(IReloadingManager<string> connectionString, ILog log)
        {
            var storage = AzureTableStorage<DepositBalanceDetectionsDeduplicationEntity>.Create(
                connectionString,
                "DepositBalanceDetectionsDeduplication",
                log);

            return new DepositBalanceDetectionsDeduplicationRepository(storage);
        }

        private DepositBalanceDetectionsDeduplicationRepository(INoSQLTableStorage<DepositBalanceDetectionsDeduplicationEntity> storage)
        {
            _storage = storage;
        }

        public async Task<IEnumerable<DepositBalanceDetectionsDeduplicationLock>> GetAsync(IEnumerable<DepositWalletKey> keys)
        {
            var entities = await _storage.GetDataAsync(keys.Select(x => new Tuple<string, string>
                (
                    DepositBalanceDetectionsDeduplicationEntity.GetPartitionKey(x.BlockchainType, x.BlockchainAssetId, x.DepositWalletAddress),
                    DepositBalanceDetectionsDeduplicationEntity.GetRowKey(x.DepositWalletAddress)
                )
            ));

            return entities.Select(x => new DepositBalanceDetectionsDeduplicationLock
            (
                block: x.Block,
                blockchainType: x.BlockchainType,
                blockchainAssetId: x.BlockchainAssetId,
                depositWalletAddress: x.DepositWalletAddress
            ));
        }

        public async Task InсreaseBlockNumberAsync(string blockchainType, string blockchainAssetId, string depositWalletAddress, long block)
        {
            var entity = new DepositBalanceDetectionsDeduplicationEntity
            {
                Block = block,
                BlockchainAssetId = blockchainAssetId,
                BlockchainType = blockchainType,
                DepositWalletAddress = depositWalletAddress,

                PartitionKey = DepositBalanceDetectionsDeduplicationEntity.GetPartitionKey(blockchainType, blockchainAssetId, depositWalletAddress),
                RowKey = DepositBalanceDetectionsDeduplicationEntity.GetRowKey(depositWalletAddress)
            };

            await _storage.InsertOrReplaceAsync
            (
                entity,
                x => entity.Block > x.Block
            );
        }
    }
}
