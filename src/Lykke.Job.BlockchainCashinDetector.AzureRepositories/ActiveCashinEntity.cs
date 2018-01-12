using System;
using Lykke.AzureStorage.Tables;

namespace Lykke.Job.BlockchainCashinDetector.AzureRepositories
{
    public class ActiveCashinEntity : AzureTableEntity
    {
        public Guid OperationId { get; set; }

        public static string GetPartitionKey(string blockchainType, string fromAddress)
        {
            // Adds hash to distribute all records to the different partitions
            var hash = (fromAddress.GetHashCode() & 0xFFF).ToString("X3");

            return $"{blockchainType}-{hash}";
        }

        public static string GetRowKey(string fromAddress, string assetId)
        {
            return $"{fromAddress}-{assetId}";
        }
    }
}
