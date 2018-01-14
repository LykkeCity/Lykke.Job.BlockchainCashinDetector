using System;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.SettingsReader;

namespace Lykke.Job.BlockchainCashinDetector.AzureRepositories
{
    public class MatchingEngineCallsDeduplicationRepository : IMatchingEngineCallsDeduplicationRepository
    {
        private readonly INoSQLTableStorage<MatchingEngineCallsDeduplicationEntity> _storage;

        public static IMatchingEngineCallsDeduplicationRepository Create(IReloadingManager<string> connectionString, ILog log)
        {
            var storage = AzureTableStorage<MatchingEngineCallsDeduplicationEntity>.Create(
                connectionString,
                "CashinMatchinEngineCallsDeduplication",
                log);

            return new MatchingEngineCallsDeduplicationRepository(storage);
        }

        private MatchingEngineCallsDeduplicationRepository(INoSQLTableStorage<MatchingEngineCallsDeduplicationEntity> storage)
        {
            _storage = storage;
        }

        public Task InsertOrReplaceAsync(Guid operationId)
        {
            return _storage.InsertOrReplaceAsync(new MatchingEngineCallsDeduplicationEntity
            {
                PartitionKey = MatchingEngineCallsDeduplicationEntity.GetPartitionKey(operationId),
                RowKey = MatchingEngineCallsDeduplicationEntity.GetRowKey(operationId)
            });
        }

        public async Task<bool> IsExists(Guid operationId)
        {
            var partitionKey = MatchingEngineCallsDeduplicationEntity.GetPartitionKey(operationId);
            var rowKey = MatchingEngineCallsDeduplicationEntity.GetRowKey(operationId);

            return await _storage.GetDataAsync(partitionKey, rowKey) != null;
        }
    }
}
