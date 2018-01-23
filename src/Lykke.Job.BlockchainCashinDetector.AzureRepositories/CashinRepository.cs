using System;
using System.Net;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Job.BlockchainCashinDetector.Core;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.SettingsReader;
using Microsoft.WindowsAzure.Storage;

namespace Lykke.Job.BlockchainCashinDetector.AzureRepositories
{
    [PublicAPI]
    public class CashinRepository : ICashinRepository
    {
        private readonly ILog _log;
        private readonly INoSQLTableStorage<CashinEntity> _storage;
        private readonly INoSQLTableStorage<CashinEntity.Indices.Started> _indexByStartedStorage;

        public static ICashinRepository Create(IReloadingManager<string> connectionString, ILog log)
        {
            var storage = AzureTableStorage<CashinEntity>.Create(
                connectionString,
                "Cashin",
                log);

            var indexByStartedStorage = AzureTableStorage<CashinEntity.Indices.Started>.Create(
                connectionString,
                "CashinIdxByStarted",
                log);

            return new CashinRepository(log, storage, indexByStartedStorage);
        }

        private CashinRepository(
            ILog log,
            INoSQLTableStorage<CashinEntity> storage,
            INoSQLTableStorage<CashinEntity.Indices.Started> indexByStartedStorage)
        {
            _log = log;
            _storage = storage;
            _indexByStartedStorage = indexByStartedStorage;
        }

        public async Task<CashinAggregate> GetOrAddAsync(
            string blockchainType, 
            string depositWalletAddress, 
            string blockchainAssetId, 
            Func<CashinAggregate> newAggregateFactory)
        {
            // Gets operation id of the current started cashin for the given blockchain, deposit wallet and asset,
            // if any, or insert operation id of the new aggregate, if no started cashin is found.

            CashinAggregate newAggregate = null;
            
            var indexByStartedPartitionKey = CashinEntity
                .Indices
                .Started
                .GetPartitionKey(blockchainType, depositWalletAddress);
            var indexByStartedRowKey = CashinEntity
                .Indices
                .Started
                .GetRowKey(depositWalletAddress, blockchainAssetId);

            var indexByStarted = await _indexByStartedStorage.GetOrInsertAsync(
                indexByStartedPartitionKey,
                indexByStartedRowKey,
                // ReSharper disable once ImplicitlyCapturedClosure
                () =>
                {
                    newAggregate = newAggregateFactory();

                    return CashinEntity.Indices.Started.IndexFromDomain(newAggregate);
                });

            var partitionKey = CashinEntity.GetPartitionKey(indexByStarted.OperationId);
            var rowKey = CashinEntity.GetRowKey(indexByStarted.OperationId);

            ChaosKitty.Meow(indexByStarted.OperationId);

            // Gets existing started or inserts new cashin

            var startedEntity = await _storage.GetOrInsertAsync(
                partitionKey,
                rowKey,
                () =>
                {
                    if (newAggregate == null)
                    {
                        // Inconsistent storage state. Index by started was retrieved
                        // from the storage, but cashin entity is missed in the storage,
                        // so creates new aggregate but existing (indexed) operation ID 
                        // will be used for the new cashin entity.

                        _log.WriteWarning(nameof(GetOrAddAsync), new
                            {
                                operationId = indexByStarted.OperationId,
                                blockchainType,
                                depositWalletAddress,
                                blockchainAssetId
                            },
                            "Inconsistent storage state detected. Index by started is found, but cashin entity is missed. Indexed operation ID will be used for the new cashin");

                        newAggregate = newAggregateFactory();

                        return CashinEntity.FromDomain(indexByStarted.OperationId, newAggregate);
                    }

                    return CashinEntity.FromDomain(newAggregate);
                });

            return startedEntity.ToDomain();
        }

        public async Task<CashinAggregate> GetAsync(Guid operationId)
        {
            var aggregate = await TryGetAsync(operationId);

            if (aggregate == null)
            {
                throw new InvalidOperationException($"Cashin with operation ID [{operationId}] is not found");
            }

            return aggregate;
        }

        public async Task<CashinAggregate> TryGetAsync(Guid operationId)
        {
            var partitionKey = CashinEntity.GetPartitionKey(operationId);
            var rowKey = CashinEntity.GetRowKey(operationId);

            var entity = await _storage.GetDataAsync(partitionKey, rowKey);

            return entity?.ToDomain();
        }

        public async Task SaveAsync(CashinAggregate aggregate)
        {
            if (aggregate.IsFinished)
            {
                var indexPartitionKey = CashinEntity
                    .Indices
                    .Started
                    .GetPartitionKey(aggregate.BlockchainType, aggregate.DepositWalletAddress);
                var indexRowKey = CashinEntity
                    .Indices
                    .Started.GetRowKey(aggregate.DepositWalletAddress, aggregate.BlockchainAssetId);

                var indexByStarted = await _indexByStartedStorage.GetDataAsync(indexPartitionKey, indexRowKey);

                if (indexByStarted != null)
                {
                    // Exactly the given operation should be removed

                    if (indexByStarted.OperationId == aggregate.OperationId)
                    {
                        try
                        {
                            await _indexByStartedStorage.DeleteAsync(indexByStarted);
                        }
                        catch (StorageException e) when (e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                        {
                            // Index has been already removed, so just ignores this exception
                        }
                    }
                }
            }

            ChaosKitty.Meow(aggregate.OperationId);

            var entity = CashinEntity.FromDomain(aggregate);
            
            await _storage.ReplaceAsync(entity);
        }
    }
}
