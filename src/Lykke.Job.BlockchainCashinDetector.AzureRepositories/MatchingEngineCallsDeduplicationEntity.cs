using System;
using Lykke.AzureStorage.Tables;

namespace Lykke.Job.BlockchainCashinDetector.AzureRepositories
{
    internal class MatchingEngineCallsDeduplicationEntity : AzureTableEntity
    {
        public static string GetPartitionKey(Guid operationId)
        {
            // Adds hash to distribute all records to the different partitions
            var hash = HashTools.GetPartitionKeyHash(operationId.ToString());

            return hash;
        }

        public static string GetRowKey(Guid operationId)
        {
            return operationId.ToString("D");
        }
    }
}
