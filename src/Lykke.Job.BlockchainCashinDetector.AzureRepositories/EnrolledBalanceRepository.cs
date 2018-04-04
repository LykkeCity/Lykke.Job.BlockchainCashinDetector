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
        private readonly ILog _log;
        private readonly INoSQLTableStorage<EnrolledBalanceEntity> _storage;


        public static IEnrolledBalanceRepository Create(IReloadingManager<string> connectionString, ILog log)
        {
            var storage = AzureTableStorage<EnrolledBalanceEntity>.Create(
                connectionString,
                "EnrolledBalance",
                log);
            
            return new EnrolledBalanceRepository(log, storage);
        }

        private EnrolledBalanceRepository(
            ILog log,
            INoSQLTableStorage<EnrolledBalanceEntity> storage)
        {
            _log = log;
            _storage = storage;
        }


        public async Task<IEnumerable<EnrolledBalance>> GetAsync(IEnumerable<DepositWalletKey> keys)
        {
            var entityKeys = keys.Select(x => new Tuple<string, string>
            (
                EnrolledBalanceEntity.GetPartitionKey(x.BlockchainType, x.BlockchainAssetId, x.DepositWalletAddress),
                EnrolledBalanceEntity.GetRowKey(x.DepositWalletAddress)
            ));

            return (await _storage.GetDataAsync(entityKeys))
                .Select(ConvertEntityToDto);
        }

        public async Task SetBalanceAsync(string blockchainType, string blockchainAssetId, string depositWalletAddress, decimal amount)
        {
            var partitionKey = EnrolledBalanceEntity.GetPartitionKey(blockchainType, blockchainAssetId, depositWalletAddress);
            var rowKey = EnrolledBalanceEntity.GetRowKey(depositWalletAddress);
            
            EnrolledBalanceEntity CreateEntity()
            {
                return new EnrolledBalanceEntity
                {
                    Balance = amount,
                    BlockchainType = blockchainType,
                    BlockchainAssetId = blockchainAssetId,
                    DepositWalletAddress = depositWalletAddress,

                    PartitionKey = partitionKey,
                    RowKey = rowKey
                };
            }
            
            // ReSharper disable once ImplicitlyCapturedClosure
            bool UpdateEntity(EnrolledBalanceEntity entity)
            {
                entity.Balance += amount;

                return true;
            }

            await _storage.InsertOrModifyAsync
            (
                partitionKey,
                rowKey,
                CreateEntity,
                UpdateEntity
            );
        }

        public async Task ResetBalanceAsync(string blockchainType, string blockchainAssetId, string depositWalletAddress, long block)
        {
            var entity = new EnrolledBalanceEntity
            {
                Balance = 0,
                Block = block,
                BlockchainType = blockchainType,
                BlockchainAssetId = blockchainAssetId,
                DepositWalletAddress = depositWalletAddress,

                PartitionKey = EnrolledBalanceEntity.GetPartitionKey(blockchainType, blockchainAssetId, depositWalletAddress),
                RowKey = EnrolledBalanceEntity.GetRowKey(depositWalletAddress)
            };

            await _storage.InsertOrReplaceAsync
            (
                entity,
                x => x.Block < entity.Block
            );
        }

        public async Task<EnrolledBalance> TryGetAsync(DepositWalletKey key)
        {
            var partitionKey = EnrolledBalanceEntity.GetPartitionKey(key.BlockchainType, key.BlockchainAssetId, key.DepositWalletAddress);
            var rowKey = EnrolledBalanceEntity.GetRowKey(key.DepositWalletAddress);

            var entity = await _storage.GetDataAsync(partitionKey, rowKey);

            return (entity != null) ? ConvertEntityToDto(entity) : null;
        }

        private static EnrolledBalance ConvertEntityToDto(EnrolledBalanceEntity entity)
        {
            return new EnrolledBalance
            (
                balance: entity.Balance,
                blockchainType: entity.BlockchainType,
                blockchainAssetId: entity.BlockchainAssetId,
                depositWalletAddress: entity.DepositWalletAddress,
                block: entity.Block
            );
        }
    }
}
