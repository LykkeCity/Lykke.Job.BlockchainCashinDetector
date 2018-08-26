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
    public class EnrolledBalanceRepository : IEnrolledBalanceRepository
    {
        private readonly INoSQLTableStorage<EnrolledBalanceEntity> _storage;


        public static IEnrolledBalanceRepository Create(IReloadingManager<string> connectionString, ILog log)
        {
            var storage = AzureTableStorage<EnrolledBalanceEntity>.Create(
                connectionString,
                "EnrolledBalance",
                log);
            
            return new EnrolledBalanceRepository(storage);
        }

        private EnrolledBalanceRepository(
            INoSQLTableStorage<EnrolledBalanceEntity> storage)
        {
            _storage = storage;
        }


        public async Task<IEnumerable<EnrolledBalance>> GetAsync(IEnumerable<DepositWalletKey> keys)
        {
            var entityKeys = keys.Select(key => new Tuple<string, string>
            (
                EnrolledBalanceEntity.GetPartitionKey(key),
                EnrolledBalanceEntity.GetRowKey(key)
            ));

            return (await _storage.GetDataAsync(entityKeys))
                .Select(e => EnrolledBalance.Create
                (
                    new DepositWalletKey(e.BlockchainAssetId, e.BlockchainType, e.DepositWalletAddress),
                    e.Balance,
                    e.Block
                ));
        }

        public async Task SetBalanceAsync(DepositWalletKey key, decimal balance, long balanceBlock)
        {
            var partitionKey = EnrolledBalanceEntity.GetPartitionKey(key);
            var rowKey = EnrolledBalanceEntity.GetRowKey(key);
            
            EnrolledBalanceEntity CreateEntity()
            {
                return new EnrolledBalanceEntity
                {
                    PartitionKey = partitionKey,
                    RowKey = rowKey,
                    BlockchainType = key.BlockchainType,
                    BlockchainAssetId = key.BlockchainAssetId,
                    DepositWalletAddress = key.DepositWalletAddress,
                    Balance = balance,
                    Block = balanceBlock
                };
            }
            
            // ReSharper disable once ImplicitlyCapturedClosure
            bool UpdateEntity(EnrolledBalanceEntity entity)
            {
                if (balanceBlock >= entity.Block)
                {
                    entity.Balance = balance;
                    return true;
                }
                
                return false;
            }

            await _storage.InsertOrModifyAsync
            (
                partitionKey,
                rowKey,
                CreateEntity,
                UpdateEntity
            );
        }

        public async Task ResetBalanceAsync(DepositWalletKey key, long transactionBlock)
        {
            var entity = new EnrolledBalanceEntity
            {
                PartitionKey = EnrolledBalanceEntity.GetPartitionKey(key),
                RowKey = EnrolledBalanceEntity.GetRowKey(key),
                BlockchainType = key.BlockchainType,
                BlockchainAssetId = key.BlockchainAssetId,
                DepositWalletAddress = key.DepositWalletAddress,
                Balance = 0,
                Block = transactionBlock
            };

            await _storage.InsertOrReplaceAsync
            (
                entity,
                x => x.Block < transactionBlock
            );
        }

        public async Task<EnrolledBalance> TryGetAsync(DepositWalletKey key)
        {
            var partitionKey = EnrolledBalanceEntity.GetPartitionKey(key);
            var rowKey = EnrolledBalanceEntity.GetRowKey(key);

            var entity = await _storage.GetDataAsync(partitionKey, rowKey);

            return entity != null
                ? EnrolledBalance.Create(key, entity.Balance, entity.Block)
                : null;
        }
    }
}
