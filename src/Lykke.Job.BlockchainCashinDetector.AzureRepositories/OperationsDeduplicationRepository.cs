using System;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Job.BlockchainCashinDetector.Core.Domain.Cashin;
using Lykke.SettingsReader;

namespace Lykke.Job.BlockchainCashinDetector.AzureRepositories
{
    public class OperationsDeduplicationRepository : IOperationsDeduplicationRepository
    {
        private readonly INoSQLTableStorage<OperationDeduplicationEntity> _storage;

        public static IOperationsDeduplicationRepository Create(IReloadingManager<string> connectionString, ILog log)
        {
            var storage = AzureTableStorage<OperationDeduplicationEntity>.Create(
                connectionString,
                "CashinOperationsDeduplication",
                log);

            return new OperationsDeduplicationRepository(storage);
        }

        private OperationsDeduplicationRepository(INoSQLTableStorage<OperationDeduplicationEntity> storage)
        {
            _storage = storage;
        }

        public Task InsertOrReplaceAsync(Guid operationId)
        {
            return _storage.InsertOrReplaceAsync(new OperationDeduplicationEntity
            {
                PartitionKey = OperationDeduplicationEntity.GetPartitionKey(operationId),
                RowKey = OperationDeduplicationEntity.GetRowKey(operationId)
            });
        }

        public async Task<bool> IsExists(Guid operationId)
        {
            var partitionKey = OperationDeduplicationEntity.GetPartitionKey(operationId);
            var rowKey = OperationDeduplicationEntity.GetRowKey(operationId);

            return await _storage.GetDataAsync(partitionKey, rowKey) != null;
        }
    }
}
