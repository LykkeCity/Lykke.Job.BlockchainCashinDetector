using System;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Common.Log;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.SettingsReader;

namespace Lykke.Job.BlockchainCashinDetector.AzureRepositories
{
    [PublicAPI]
    public class CashinRepository : ICashinRepository
    {
        private readonly INoSQLTableStorage<CashinEntity> _storage;
        private readonly IChaosKitty _chaosKitty;

        public static ICashinRepository Create(
            IReloadingManager<string> connectionString, 
            ILogFactory log,
            IChaosKitty chaosKitty)
        {
            if (chaosKitty == null) throw new ArgumentNullException(nameof(chaosKitty));
            var storage = AzureTableStorage<CashinEntity>.Create(
                connectionString,
                "Cashin",
                log);

            return new CashinRepository(storage, chaosKitty);
        }

        private CashinRepository(
            INoSQLTableStorage<CashinEntity> storage, 
            IChaosKitty chaosKitty)
        {
            _storage = storage;
            _chaosKitty = chaosKitty;
        }

        public async Task<CashinAggregate> GetOrAddAsync(
            string blockchainType, 
            string depositWalletAddress, 
            string blockchainAssetId,
            Guid operationId,
            Func<CashinAggregate> newAggregateFactory)
        {          
            var partitionKey = CashinEntity.GetPartitionKey(operationId);
            var rowKey = CashinEntity.GetRowKey(operationId);

            var startedEntity = await _storage.GetOrInsertAsync(
                partitionKey,
                rowKey,
                () =>
                {
                    var newAggregate = newAggregateFactory();

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
            var entity = CashinEntity.FromDomain(aggregate);
            
            await _storage.ReplaceAsync(entity);

            _chaosKitty.Meow(aggregate.OperationId);
        }
    }
}
