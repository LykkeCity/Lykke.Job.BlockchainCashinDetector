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
        private INoSQLTableStorage<DepositWalletLockEntity> _storage;

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

        public async Task<Guid> LockAsync(string blockchainType, string depositWalletAddress, string blockchainAssetId, Func<Guid> operationIdFactory)
        {
            var partitionKey = DepositWalletLockEntity.GetPartitionKey(blockchainType, depositWalletAddress);
            var rowKey = DepositWalletLockEntity.GetRowKey(depositWalletAddress, blockchainAssetId);

            var entity = await _storage.GetOrInsertAsync(
                partitionKey,
                rowKey,
                // ReSharper disable once ImplicitlyCapturedClosure
                () =>
                {
                    return DepositWalletLockEntity.Create(blockchainType, depositWalletAddress, blockchainAssetId, operationIdFactory());
                });

            return entity.OperationId;
        }

        public async Task ReleaseAsync(string blockchainType, string depositWalletAddress, string blockchainAssetId, Guid operationId)
        {
            var partitionKey = DepositWalletLockEntity.GetPartitionKey(blockchainType, depositWalletAddress);
            var rowKey = DepositWalletLockEntity.GetRowKey(depositWalletAddress, blockchainAssetId);

            await _storage.DeleteIfExistAsync(partitionKey, rowKey, e => e.OperationId == operationId);
        }
    }
}
