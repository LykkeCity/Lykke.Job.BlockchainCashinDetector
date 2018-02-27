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
                .Select(x => new EnrolledBalance
                (
                    balance: x.Balance,
                    blockchainType: x.BlockchainType,
                    blockchainAssetId: x.BlockchainAssetId,
                    depositWalletAddress: x.DepositWalletAddress,
                    block: x.Block
                ));
        }

        public async Task InсreaseBalanceAsync(string blockchainType, string blockchainAssetId, string depositWalletAddress, decimal amount)
        {
            var partitionKey = EnrolledBalanceEntity.GetPartitionKey(blockchainType, blockchainAssetId, depositWalletAddress);
            var rowKey = EnrolledBalanceEntity.GetRowKey(depositWalletAddress);

            var entity = await _storage.GetDataAsync(partitionKey, rowKey) ?? new EnrolledBalanceEntity
            {
                BlockchainType = blockchainType,
                BlockchainAssetId = blockchainAssetId,
                DepositWalletAddress = depositWalletAddress,

                PartitionKey = partitionKey,
                RowKey = rowKey
            };

            entity.Balance += amount;
            
            await _storage.InsertOrReplaceAsync
            (
                entity,
                x => x.Balance < entity.Balance
            );
        }

        public async Task ResetBalanceAsync(string blockchainType, string blockchainAssetId, string depositWalletAddress, long block)
        {
            var entity = new EnrolledBalanceEntity
            {
                Balance = 0,
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
    }
}
