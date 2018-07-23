using System;
using Common;
using JetBrains.Annotations;
using Lykke.AzureStorage.Tables;

namespace Lykke.Job.BlockchainCashinDetector.AzureRepositories
{
    internal class DepositWalletLockEntity : AzureTableEntity
    {
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public Guid OperationId { get; set; }

        public static string GetPartitionKey(string blockchainType, string depositWalletAddress)
        {
            // Adds hash to distribute all records to the different partitions
            var hash = depositWalletAddress.CalculateHexHash32(3);

            return $"{blockchainType}-{hash}";
        }

        public static string GetRowKey(string depositWalletAddress, string blockchainAssetId)
        {
            return $"{depositWalletAddress}-{blockchainAssetId}";
        }

        public static DepositWalletLockEntity Create(string blockchainType, string depositWalletAddress, string blockchainAssetId, Guid operationId)
        {
            return new DepositWalletLockEntity
            {
                PartitionKey = GetPartitionKey(blockchainType, depositWalletAddress),
                RowKey = GetRowKey(depositWalletAddress, blockchainAssetId),
                OperationId = operationId
            };
        }
    }
}
