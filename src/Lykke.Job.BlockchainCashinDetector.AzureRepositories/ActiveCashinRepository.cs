using System;
using System.Net;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Job.BlockchainCashinDetector.Core.Domain.Cashin;
using Lykke.SettingsReader;
using Microsoft.WindowsAzure.Storage;

namespace Lykke.Job.BlockchainCashinDetector.AzureRepositories
{
    [PublicAPI]
    public class ActiveCashinRepository : IActiveCashinRepository
    {
        private readonly INoSQLTableStorage<ActiveCashinEntity> _storage;

        public static IActiveCashinRepository Create(IReloadingManager<string> connectionString, ILog log)
        {
            var storage = AzureTableStorage<ActiveCashinEntity>.Create(
                connectionString,
                "ActiveCashins",
                log);

            return new ActiveCashinRepository(storage);
        }

        private ActiveCashinRepository(INoSQLTableStorage<ActiveCashinEntity> storage)
        {
            _storage = storage;
        }

        public async Task<Guid> GetOrAdd(string blockchainType, string fromAddress, string assetId, Func<Guid> generateOperationId)
        {
            var partitionKey = ActiveCashinEntity.GetPartitionKey(blockchainType, fromAddress);
            var rowKey = ActiveCashinEntity.GetRowKey(fromAddress, assetId);

            var entity = await _storage.GetOrInsertAsync(partitionKey, rowKey, () =>
                new ActiveCashinEntity
                {
                    PartitionKey = ActiveCashinEntity.GetPartitionKey(blockchainType, fromAddress),
                    RowKey = ActiveCashinEntity.GetRowKey(fromAddress, assetId),
                    OperationId = generateOperationId()
                }
            );

            return entity.OperationId;
        }

        public async Task<bool> TryRemoveAsync(string blockchainType, string fromAddress, string assetId, Guid operationId)
        {
            var partitionKey = ActiveCashinEntity.GetPartitionKey(blockchainType, fromAddress);
            var rowKey = ActiveCashinEntity.GetRowKey(fromAddress, assetId);

            var entity = await _storage.GetDataAsync(partitionKey, rowKey);

            if (entity != null)
            {
                // Exactly given operation should be removed

                if (entity.OperationId == operationId)
                {
                    try
                    {
                        await _storage.DeleteAsync(entity);

                        return true;
                    }
                    catch (StorageException e) when (
                        // Concurrency errors
                        (HttpStatusCode) e.RequestInformation.HttpStatusCode == HttpStatusCode.PreconditionFailed ||
                        (HttpStatusCode) e.RequestInformation.HttpStatusCode == HttpStatusCode.NotFound)
                    {
                    }
                }
            }

            return false;
        }
    }
}
