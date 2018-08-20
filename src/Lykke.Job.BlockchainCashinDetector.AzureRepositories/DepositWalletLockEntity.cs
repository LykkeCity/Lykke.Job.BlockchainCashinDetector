using System;
using Common;
using JetBrains.Annotations;
using Lykke.AzureStorage.Tables;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;

namespace Lykke.Job.BlockchainCashinDetector.AzureRepositories
{
    internal class DepositWalletLockEntity : AzureTableEntity
    {
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public Guid OperationId { get; set; }
        
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public decimal Balance { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public long Block { get; set; }

        public static string GetPartitionKey(DepositWalletKey key)
        {
            // Adds hash to distribute all records to the different partitions
            var hash = key.DepositWalletAddress.CalculateHexHash32(3);

            return $"{key.BlockchainType}-{hash}";
        }

        public static string GetRowKey(DepositWalletKey key)
        {
            return $"{key.DepositWalletAddress}-{key.BlockchainAssetId}";
        }

        public static DepositWalletLockEntity Create(
            DepositWalletKey key,
            decimal balance,
            long block,
            Guid operationId)
        {
            return new DepositWalletLockEntity
            {
                PartitionKey = GetPartitionKey(key),
                RowKey = GetRowKey(key),
                OperationId = operationId,
                Balance = balance,
                Block = block
            };
        }
    }
}
