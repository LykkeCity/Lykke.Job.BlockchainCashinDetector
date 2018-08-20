using System;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.SettingsReader;

namespace Lykke.Job.BlockchainCashinDetector.AzureRepositories
{
    public class DepositWalletLockRepository : IDepositWalletLockRepository
    {
        private readonly INoSQLTableStorage<DepositWalletLockEntity> _storage;

        public static IDepositWalletLockRepository Create(IReloadingManager<string> connectionString, ILog log)
        {
            var storage = AzureTableStorage<DepositWalletLockEntity>.Create(
                connectionString,
                "DepositWalletLock",
                log);

            return new DepositWalletLockRepository(storage);
        }

        private DepositWalletLockRepository(INoSQLTableStorage<DepositWalletLockEntity> storage)
        {
            _storage = storage;
        }

        public async Task<DepositWalletLock> LockAsync(
            DepositWalletKey key,
            decimal balance,
            long block,
            Func<Guid> operationIdFactory)
        {
            var partitionKey = DepositWalletLockEntity.GetPartitionKey(key);
            var rowKey = DepositWalletLockEntity.GetRowKey(key);

            var entity = await _storage.GetOrInsertAsync
            (
                partitionKey,
                rowKey,
                // ReSharper disable once ImplicitlyCapturedClosure
                () => DepositWalletLockEntity.Create
                (
                    key,
                    balance,
                    block,
                    operationIdFactory()
                )
            );

            return DepositWalletLock.Create(key, entity.OperationId, entity.Balance, entity.Block);
        }

        public async Task ReleaseAsync(DepositWalletKey key, Guid operationId)
        {
            var partitionKey = DepositWalletLockEntity.GetPartitionKey(key);
            var rowKey = DepositWalletLockEntity.GetRowKey(key);

            await _storage.DeleteIfExistAsync(partitionKey, rowKey, e => e.OperationId == operationId);
        }
    }
}
