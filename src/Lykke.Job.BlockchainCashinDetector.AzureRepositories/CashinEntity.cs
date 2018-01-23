using System;
using Common;
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
                    var hash = depositWalletAddress.CalculateHexHash32(3);

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
        public CashinResult Result { get; set; }

        [UsedImplicitly]
        public DateTime CreationMoment { get; set; }
        [UsedImplicitly]
        public DateTime? StartMoment { get; set; }
        [UsedImplicitly]
        public DateTime? MatchingEngineEnrollementMoment { get; set; }
        [UsedImplicitly]
        public DateTime? OperationFinishMoment { get; set; }
        [UsedImplicitly]
        public DateTime? MatchingEngineDeduplicationLockRemovingMoment { get; set; }

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
        public Guid? ClientId { get; set; }
        [UsedImplicitly]
        public string AssetId { get; set; }
        [UsedImplicitly]
        public string TransactionHash { get; set; }
        [UsedImplicitly]
        public decimal? TransactionAmount { get; set; }
        [UsedImplicitly]
        public decimal? Fee { get; set; }
        [UsedImplicitly]
        public string Error { get; set; }

        #endregion


        #region Keys

        public static string GetPartitionKey(Guid operationId)
        {
            // Use hash to distribute all records to the different partitions
            var hash = operationId.ToString().CalculateHexHash32(3);

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
                Result = aggregate.Result,
                CreationMoment = aggregate.CreationMoment,
                StartMoment = aggregate.StartMoment,
                MatchingEngineEnrollementMoment = aggregate.MatchingEngineEnrollementMoment,
                MatchingEngineDeduplicationLockRemovingMoment = aggregate.MatchingEngineDeduplicationLockRemovingMoment,
                OperationFinishMoment = aggregate.OperationFinishMoment,
                OperationId = operationId,
                BlockchainType = aggregate.BlockchainType,
                HotWalletAddress = aggregate.HotWalletAddress,
                DepositWalletAddress = aggregate.DepositWalletAddress,
                BlockchainAssetId = aggregate.BlockchainAssetId,
                Amount = aggregate.Amount,
                ClientId = aggregate.ClientId,
                AssetId = aggregate.AssetId,
                TransactionHash = aggregate.TransactionHash,
                TransactionAmount = aggregate.TransactionAmount,
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
                Result,
                CreationMoment,
                StartMoment,
                MatchingEngineEnrollementMoment,
                OperationFinishMoment,
                MatchingEngineDeduplicationLockRemovingMoment,
                OperationId,
                BlockchainType,
                HotWalletAddress,
                DepositWalletAddress,
                BlockchainAssetId,
                Amount,
                ClientId,
                AssetId,
                TransactionHash,
                TransactionAmount,
                Fee,
                Error);
        }

        #endregion
    }
}
