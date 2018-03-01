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

        // ReSharper disable MemberCanBePrivate.Global

        public CashinState State { get; set; }
        public CashinResult Result { get; set; }

        public DateTime CreationMoment { get; set; }
        public DateTime? StartMoment { get; set; }
        public DateTime? MatchingEngineEnrollementMoment { get; set; }
        public DateTime? EnrolledBalanceIncreasedMoment { get; set; }
        public DateTime? OperationFinishMoment { get; set; }
        public DateTime? ClientOperationFinishRegistrationMoment { get; set; }
        

        public Guid OperationId { get; set; }
        public string BlockchainType { get; set; }
        public string HotWalletAddress { get; set; }
        public string DepositWalletAddress { get; set; }
        public string BlockchainAssetId { get; set; }
        public decimal Amount { get; set; }
        public decimal OperationAmount { get; set; }

        public Guid? ClientId { get; set; }
        public string AssetId { get; set; }
        public string TransactionHash { get; set; }
        public decimal? TransactionAmount { get; set; }
        public long? TransactionBlock { get; set; }
        public decimal? Fee { get; set; }
        public string Error { get; set; }

        // ReSharper restore MemberCanBePrivate.Global

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
                EnrolledBalanceIncreasedMoment = aggregate.EnrolledBalanceIncreasedMoment,
                OperationFinishMoment = aggregate.OperationFinishMoment,
                OperationId = operationId,
                BlockchainType = aggregate.BlockchainType,
                HotWalletAddress = aggregate.HotWalletAddress,
                DepositWalletAddress = aggregate.DepositWalletAddress,
                BlockchainAssetId = aggregate.BlockchainAssetId,
                Amount = aggregate.Amount,
                OperationAmount = aggregate.OperationAmount,
                ClientId = aggregate.ClientId,
                AssetId = aggregate.AssetId,
                TransactionHash = aggregate.TransactionHash,
                TransactionAmount = aggregate.TransactionAmount,
                TransactionBlock = aggregate.TransactionBlock,
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
            return CashinAggregate.Restore
            (
                ETag,
                State,
                Result,
                CreationMoment,
                StartMoment,
                MatchingEngineEnrollementMoment,
                EnrolledBalanceIncreasedMoment,
                OperationFinishMoment,
                OperationId,
                BlockchainType,
                HotWalletAddress,
                DepositWalletAddress,
                BlockchainAssetId,
                Amount,
                OperationAmount,
                ClientId,
                AssetId,
                TransactionHash,
                TransactionAmount,
                TransactionBlock,
                Fee,
                Error
            );
        }

        #endregion
    }
}
