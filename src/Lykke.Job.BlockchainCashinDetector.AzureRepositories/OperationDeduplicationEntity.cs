using System;
using Lykke.AzureStorage.Tables;

namespace Lykke.Job.BlockchainCashinDetector.AzureRepositories
{
    internal class OperationDeduplicationEntity : AzureTableEntity
    {
        public static string GetPartitionKey(Guid operationId)
        {
            // Adds hash to distribute all records to the different partitions
            var hash = (operationId.GetHashCode() & 0xFFF).ToString("X3");

            return hash;
        }

        public static string GetRowKey(Guid operationId)
        {
            return operationId.ToString("D");
        }
    }
}
