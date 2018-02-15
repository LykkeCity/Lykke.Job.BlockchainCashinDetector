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

        public async Task InsertOrReplaceAsync(string blockchainType, string blockchainAssetId, string depositWalletAddress, long block)
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

            await _storage.InsertOrReplaceAsync(entity);
        }

        public async Task<long?> TryGetAsync(string blockchainType, string blockchainAssetId, string depositWalletAddress)
        {
            var entity = await _storage.GetDataAsync
            (
                DepositBalanceDetectionsDeduplicationEntity.GetPartitionKey(blockchainType, blockchainAssetId, depositWalletAddress),
                DepositBalanceDetectionsDeduplicationEntity.GetRowKey(depositWalletAddress)
            );

            return entity?.Block;
        }
    }
}
