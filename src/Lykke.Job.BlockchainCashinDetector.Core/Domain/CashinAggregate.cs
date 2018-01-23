using System;

namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public class CashinAggregate
    {
        public string Version { get; }

        public CashinState State { get; private set; }
        public CashinResult Result { get; private set; }

        public DateTime CreationMoment { get; }
        public DateTime? StartMoment { get; private set; }
        public DateTime? MatchingEngineEnrollementMoment { get; private set; }
        public DateTime? OperationFinishMoment { get; private set; }
        public DateTime? MatchingEngineDeduplicationLockRemovingMoment { get; private set; }

        public Guid OperationId { get; }
        public string BlockchainType { get; }
        public string HotWalletAddress { get; }
        public string DepositWalletAddress { get; }
        public string BlockchainAssetId { get; }
        public decimal Amount { get; }

        public Guid? ClientId { get; private set; }
        public string AssetId { get; private set; }
        public string TransactionHash { get; private set; }
        public decimal? TransactionAmount { get; private set; }
        public decimal? Fee { get; private set; }
        public string Error { get; private set; }

        public bool IsFinished => Result == CashinResult.Success || Result == CashinResult.Failure;

        private CashinAggregate(
            string blockchainType, 
            string hotWalletAddress, 
            string depositWalletAddress, 
            string blockchainAssetId, 
            decimal amount)
        {
            CreationMoment = DateTime.UtcNow;

            OperationId = Guid.NewGuid();
            BlockchainType = blockchainType;
            HotWalletAddress = hotWalletAddress;
            DepositWalletAddress = depositWalletAddress;
            BlockchainAssetId = blockchainAssetId;
            Amount = amount;

            State = CashinState.Starting;
            Result = CashinResult.Unknown;
        }

        private CashinAggregate(
            string version,
            CashinState state,
            CashinResult result,
            DateTime creationMoment,
            DateTime? startMoment,
            DateTime? matchingEngineEnrollementMoment,
            DateTime? operationFinishMoment,
            DateTime? matchingEngineDeduplicationLockRemovingMoment,
            Guid operationId,
            string blockchainType,
            string hotWalletAddress,
            string depositWalletAddress,
            string blockchainAssetId,
            decimal amount,
            Guid? clientId,
            string assetId,
            string transactionHash,
            decimal? transactionAmount,
            decimal? fee,
            string error)
        {
            Version = version;
            State = state;
            Result = result;

            CreationMoment = creationMoment;
            StartMoment = startMoment;
            MatchingEngineEnrollementMoment = matchingEngineEnrollementMoment;
            OperationFinishMoment = operationFinishMoment;
            MatchingEngineDeduplicationLockRemovingMoment = matchingEngineDeduplicationLockRemovingMoment;

            OperationId = operationId;
            BlockchainType = blockchainType;
            HotWalletAddress = hotWalletAddress;
            DepositWalletAddress = depositWalletAddress;
            BlockchainAssetId = blockchainAssetId;
            Amount = amount;

            ClientId = clientId;
            AssetId = assetId;
            TransactionHash = transactionHash;
            TransactionAmount = transactionAmount;
            Fee = fee;
            Error = error;
        }

        public static CashinAggregate StartNew(
            string blockchainType,
            string hotWalletAddress,
            string depositWalletAddress,
            string blockchainAssetId,
            decimal amount)
        {
            return new CashinAggregate(blockchainType, hotWalletAddress, depositWalletAddress, blockchainAssetId, amount);
        }

        public static CashinAggregate Restore(
            string version,
            CashinState state,
            CashinResult result,
            DateTime creationMoment,
            DateTime? startMoment,
            DateTime? matchingEngineEnrollementMoment,
            DateTime? finishMoment,
            DateTime? matchingEngineDeduplicationLockRemovingMoment,
            Guid operationId,
            string blockchainType,
            string hotWalletAddress,
            string depositWalletAddress,
            string blockchainAssetId,
            decimal amount,
            Guid? clientId,
            string assetId,
            string transactionHash,
            decimal? transactionAmount,
            decimal? fee,
            string error)
        {
            return new CashinAggregate(
                version,
                state,
                result,
                creationMoment,
                startMoment,
                matchingEngineEnrollementMoment,
                finishMoment,
                matchingEngineDeduplicationLockRemovingMoment,
                operationId,
                blockchainType,
                hotWalletAddress,
                depositWalletAddress,
                blockchainAssetId,
                amount,
                clientId,
                assetId,
                transactionHash,
                transactionAmount,
                fee,
                error);
        }

        public bool Start()
        {
            if (State != CashinState.Starting)
            {
                return false;
            }

            StartMoment = DateTime.UtcNow;

            State = CashinState.Started;

            return true;
        }

        public bool OnEnrolledToMatchingEngine(Guid clientId, string assetId)
        {
            if (State != CashinState.Started)
            {
                return false;
            }

            MatchingEngineEnrollementMoment = DateTime.UtcNow;

            ClientId = clientId;
            AssetId = assetId;

            State = CashinState.EnrolledToMatchingEnging;

            return true;
        }
        
        public bool OnOperationComplete(string transactionHash, decimal transactionAmount, decimal fee)
        {
            if (State != CashinState.EnrolledToMatchingEnging)
            {
                return false;
            }

            OperationFinishMoment = DateTime.UtcNow;

            TransactionHash = transactionHash;
            TransactionAmount = transactionAmount;
            Fee = fee;

            State = CashinState.OperationIsFinished;
            Result = CashinResult.Success;

            return true;
        }

        public bool OnOperationFailed(string error)
        {
            if (State != CashinState.EnrolledToMatchingEnging)
            {
                return false;
            }

            OperationFinishMoment = DateTime.UtcNow;

            Error = error;

            State = CashinState.OperationIsFinished;
            Result = CashinResult.Failure;

            return true;
        }

        public bool OnMatchingEngineDeduplicationLockRemoved()
        {
            if (State != CashinState.OperationIsFinished)
            {
                return false;
            }

            MatchingEngineDeduplicationLockRemovingMoment = DateTime.UtcNow;

            State = CashinState.MatchingEngineDeduplicationLockIsRemoved;

            return true;
        }
    }
}
