using System;
using JetBrains.Annotations;
using Lykke.AzureStorage.Tables;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;

namespace Lykke.Job.BlockchainCashinDetector.AzureRepositories
{
    internal class CashinEntity : AzureTableEntity
    {
        #region Indices

        public static class Indices
        {
            public class Started : AzureTableEntity
            {
                [UsedImplicitly(ImplicitUseKindFlags.Assign)]
                public Guid OperationId { get; set; }

                public static string GetPartitionKey(string blockchainType, string depositWalletAddress)
                {
                    // Adds hash to distribute all records to the different partitions
                    var hash = HashTools.GetPartitionKeyHash(depositWalletAddress);

                    return $"{blockchainType}-{hash}";
                }

                public static string GetRowKey(string depositWalletAddress, string blockchainAssetId)
                {
                    return $"{depositWalletAddress}-{blockchainAssetId}";
                }

                public static Started IndexFromDomain(CashinAggregate aggregate)
                {
                    return new Started
                    {
                        PartitionKey = GetPartitionKey(aggregate.BlockchainType, aggregate.DepositWalletAddress),
                        RowKey = GetRowKey(aggregate.DepositWalletAddress, aggregate.BlockchainAssetId),
                        OperationId = aggregate.OperationId
                    };
                }
            }
        }

        #endregion


        #region Fields

        [UsedImplicitly]
        public CashinState State { get; set; }
        [UsedImplicitly]
        public DateTime StartMoment { get; set; }
        [UsedImplicitly]
        public DateTime? MatchingEngineEnrollementMoment { get; set; }
        [UsedImplicitly]
        public DateTime? FinishMoment { get; set; }

        [UsedImplicitly]
        public Guid OperationId { get; set; }
        [UsedImplicitly]
        public string BlockchainType { get; set; }
        [UsedImplicitly]
        public string HotWalletAddress { get; set; }
        [UsedImplicitly]
        public string DepositWalletAddress { get; set; }
        [UsedImplicitly]
        public string BlockchainAssetId { get; set; }
        [UsedImplicitly]
        public decimal Amount { get; set; }

        [UsedImplicitly]
        public string ClientId { get; set; }
        [UsedImplicitly]
        public string AssetId { get; set; }
        [UsedImplicitly]
        public string TransactionHash { get; set; }
        [UsedImplicitly]
        public DateTime? TransactionTimestamp { get; set; }
        [UsedImplicitly]
        public decimal? Fee { get; set; }
        [UsedImplicitly]
        public string Error { get; set; }

        #endregion


        #region Keys

        public static string GetPartitionKey(Guid operationId)
        {
            // Use hash to distribute all records to the different partitions
            var hash = HashTools.GetPartitionKeyHash(operationId.ToString());

            return $"{hash}";
        }

        public static string GetRowKey(Guid operationId)
        {
            return $"{operationId:D}";
        }

        #endregion


        #region Conversion

        public static CashinEntity FromDomain(Guid operationId, CashinAggregate aggregate)
        {
            return new CashinEntity
            {
                ETag = string.IsNullOrEmpty(aggregate.Version) ? "*" : aggregate.Version,
                PartitionKey = GetPartitionKey(operationId),
                RowKey = GetRowKey(operationId),
                State = aggregate.State,
                StartMoment = aggregate.StartMoment,
                MatchingEngineEnrollementMoment = aggregate.MatchingEngineEnrollementMoment,
                FinishMoment = aggregate.FinishMoment,
                OperationId = operationId,
                BlockchainType = aggregate.BlockchainType,
                HotWalletAddress = aggregate.HotWalletAddress,
                DepositWalletAddress = aggregate.DepositWalletAddress,
                BlockchainAssetId = aggregate.BlockchainAssetId,
                Amount = aggregate.Amount,
                ClientId = aggregate.ClientId,
                AssetId = aggregate.AssetId,
                TransactionHash = aggregate.TransactionHash,
                TransactionTimestamp = aggregate.TransactionTimestamp,
                Fee = aggregate.Fee,
                Error = aggregate.Error
            };
        }
        
        public static CashinEntity FromDomain(CashinAggregate aggregate)
        {
            return FromDomain(aggregate.OperationId, aggregate);
        }

        public CashinAggregate ToDomain()
        {
            return CashinAggregate.Restore(
                ETag,
                State,
                StartMoment,
                MatchingEngineEnrollementMoment,
                FinishMoment,
                OperationId,
                BlockchainType,
                HotWalletAddress,
                DepositWalletAddress,
                BlockchainAssetId,
                Amount,
                ClientId,
                AssetId,
                TransactionHash,
                TransactionTimestamp,
                Fee,
                Error);
        }

        #endregion
    }
}
