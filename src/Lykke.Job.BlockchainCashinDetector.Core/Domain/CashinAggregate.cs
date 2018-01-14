using System;

namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public class CashinAggregate
    {
        public string Version { get; }

        public CashinState State { get; private set; }

        public DateTime CreationMoment { get; }
        public DateTime? StartMoment { get; private set; }
        public DateTime? MatchingEngineEnrollementMoment { get; private set; }
        public DateTime? FinishMoment { get; private set; }

        public Guid OperationId { get; }
        public string BlockchainType { get; }
        public string HotWalletAddress { get; }
        public string DepositWalletAddress { get; }
        public string BlockchainAssetId { get; }
        public decimal Amount { get; }

        public string ClientId { get; private set; }
        public string AssetId { get; private set; }
        public string TransactionHash { get; private set; }
        public DateTime? TransactionTimestamp { get; private set; }
        public decimal? Fee { get; private set; }
        public string Error { get; private set; }

        public bool IsFinished => State == CashinState.Failed || State == CashinState.Completed;

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
        }

        private CashinAggregate(
            string version,
            CashinState state,
            DateTime creationMoment,
            DateTime? startMoment,
            DateTime? matchingEngineEnrollementMoment,
            DateTime? finishMoment,
            Guid operationId,
            string blockchainType,
            string hotWalletAddress,
            string depositWalletAddress,
            string blockchainAssetId,
            decimal amount,
            string clientId,
            string assetId,
            string transactionHash,
            DateTime? transactionTimestamp,
            decimal? fee,
            string error)
        {
            Version = version;
            State = state;

            CreationMoment = creationMoment;
            StartMoment = startMoment;
            MatchingEngineEnrollementMoment = matchingEngineEnrollementMoment;
            FinishMoment = finishMoment;

            OperationId = operationId;
            BlockchainType = blockchainType;
            HotWalletAddress = hotWalletAddress;
            DepositWalletAddress = depositWalletAddress;
            BlockchainAssetId = blockchainAssetId;
            Amount = amount;

            ClientId = clientId;
            AssetId = assetId;
            TransactionHash = transactionHash;
            TransactionTimestamp = transactionTimestamp;
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
            DateTime creationMoment,
            DateTime? startMoment,
            DateTime? matchingEngineEnrollementMoment,
            DateTime? finishMoment,
            Guid operationId,
            string blockchainType,
            string hotWalletAddress,
            string depositWalletAddress,
            string blockchainAssetId,
            decimal amount,
            string clientId,
            string assetId,
            string transactionHash,
            DateTime? transactionTimestamp,
            decimal? fee,
            string error)
        {
            return new CashinAggregate(
                version,
                state,
                creationMoment,
                startMoment,
                matchingEngineEnrollementMoment,
                finishMoment,
                operationId,
                blockchainType,
                hotWalletAddress,
                depositWalletAddress,
                blockchainAssetId,
                amount,
                clientId,
                assetId,
                transactionHash,
                transactionTimestamp,
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

        public bool OnEnrolledToMatchingEngine(string clientId, string assetId)
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
        
        public bool OnOperationComplete(string transactionHash, DateTime transactionTimestamp, decimal fee)
        {
            if (State != CashinState.EnrolledToMatchingEnging)
            {
                return false;
            }

            FinishMoment = DateTime.UtcNow;

            TransactionHash = transactionHash;
            TransactionTimestamp = transactionTimestamp;
            Fee = fee;

            State = CashinState.Completed;

            return true;
        }

        public bool OnOperationFailed(DateTime transactionTimestamp, string error)
        {
            if (State != CashinState.EnrolledToMatchingEnging)
            {
                return false;
            }

            FinishMoment = DateTime.UtcNow;

            TransactionTimestamp = transactionTimestamp;
            Error = error;

            State = CashinState.Failed;

            return true;
        }
    }
}
