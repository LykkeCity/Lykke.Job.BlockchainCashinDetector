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
        public DateTime? EnrolledBalanceSetMoment { get; set; }
        public DateTime? EnrolledBalanceResetMoment { get; set; }
        public DateTime? OperationFinishMoment { get; set; }
        
        public Guid OperationId { get; set; }
        public string BlockchainType { get; set; }
        public string HotWalletAddress { get; set; }
        public string DepositWalletAddress { get; set; }
        public string BlockchainAssetId { get; set; }
        public string AssetId { get; set; }
        public int AssetAccuracy { get; set; }
        public decimal BalanceAmount { get; set; }
        public long BalanceBlock { get; set; }
        public decimal CashinMinimalAmount { get; set; }
        
        public Guid? ClientId { get; set; }
        public string TransactionHash { get; set; }
        public long? TransactionBlock { get; set; }
        public decimal? Fee { get; set; }
        public string Error { get; set; }
        public decimal? EnrolledBalanceAmount { get; set; }
        public decimal? OperationAmount { get; set; }
        public double? MeAmount { get;set; }
        public decimal? TransactionAmount { get; set; }

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
                AssetId = aggregate.AssetId,
                AssetAccuracy =  aggregate.AssetAccuracy,
                BalanceAmount = aggregate.BalanceAmount,
                BalanceBlock = aggregate.BalanceBlock,
                BlockchainAssetId = aggregate.BlockchainAssetId,
                BlockchainType = aggregate.BlockchainType,
                CashinMinimalAmount = aggregate.CashinMinimalAmount,
                ClientId = aggregate.ClientId,
                CreationMoment = aggregate.CreationMoment,
                DepositWalletAddress = aggregate.DepositWalletAddress,
                EnrolledBalanceAmount = aggregate.EnrolledBalanceAmount,
                EnrolledBalanceResetMoment = aggregate.EnrolledBalanceResetMoment,
                EnrolledBalanceSetMoment = aggregate.EnrolledBalanceSetMoment,
                Error = aggregate.Error,
                Fee = aggregate.Fee,
                HotWalletAddress = aggregate.HotWalletAddress,
                MatchingEngineEnrollementMoment = aggregate.MatchingEngineEnrollementMoment,
                OperationFinishMoment = aggregate.OperationFinishMoment,
                OperationAmount = aggregate.OperationAmount,
                MeAmount = aggregate.MeAmount,
                OperationId = operationId,
                Result = aggregate.Result,
                StartMoment = aggregate.StartMoment,
                State = aggregate.State,
                TransactionAmount = aggregate.TransactionAmount,
                TransactionBlock = aggregate.TransactionBlock,
                TransactionHash = aggregate.TransactionHash,


                ETag = string.IsNullOrEmpty(aggregate.Version) ? "*" : aggregate.Version,
                PartitionKey = GetPartitionKey(operationId),
                RowKey = GetRowKey(operationId),
                
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
                clientId: ClientId,
                assetId: AssetId,
                assetAccuracy: AssetAccuracy,
                balanceAmount: BalanceAmount,
                balanceBlock: BalanceBlock,
                blockchainAssetId: BlockchainAssetId,
                blockchainType: BlockchainType,
                cashinMinimalAmount: CashinMinimalAmount,
                creationMoment: CreationMoment,
                depositWalletAddress: DepositWalletAddress,
                enrolledBalanceAmount: EnrolledBalanceAmount,
                enrolledBalanceResetMoment: EnrolledBalanceResetMoment,
                enrolledBalanceSetMoment: EnrolledBalanceSetMoment,
                error: Error,
                fee: Fee,
                hotWalletAddress: HotWalletAddress,
                matchingEngineEnrollementMoment: MatchingEngineEnrollementMoment,
                operationAmount: OperationAmount,
                meAmount: MeAmount,
                operationFinishMoment: OperationFinishMoment,
                operationId: OperationId,
                result: Result,
                startMoment: StartMoment,
                transactionAmount: TransactionAmount,
                transactionBlock: TransactionBlock,
                transactionHash: TransactionHash,
                state: State,
                version: ETag
            );
        }

        #endregion
    }
}
