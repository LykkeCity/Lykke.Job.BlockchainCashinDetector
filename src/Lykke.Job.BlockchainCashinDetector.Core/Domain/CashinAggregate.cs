using System;
using System.Collections.Generic;
using System.Linq;

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


        public Guid OperationId { get; }
        public string BlockchainType { get; }
        public string HotWalletAddress { get; }
        public string DepositWalletAddress { get; }
        public string BlockchainAssetId { get; }
        public decimal Amount { get; }
        public string AssetId { get; }

        public Guid? ClientId { get; private set; }
        public string TransactionHash { get; private set; }
        public decimal? TransactionAmount { get; private set; }
        public long? TransactionBlock { get; private set; }
        public decimal? Fee { get; private set; }
        public string Error { get; private set; }

        public bool IsFinished => Result == CashinResult.Success || Result == CashinResult.Failure;

        private CashinAggregate(
            string blockchainType, 
            string hotWalletAddress, 
            string depositWalletAddress, 
            string blockchainAssetId, 
            decimal amount, 
            string assetId)
        {
            CreationMoment = DateTime.UtcNow;

            OperationId = Guid.NewGuid();
            BlockchainType = blockchainType;
            HotWalletAddress = hotWalletAddress;
            DepositWalletAddress = depositWalletAddress;
            BlockchainAssetId = blockchainAssetId;
            Amount = amount;
            AssetId = assetId;

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
            long? transactionBlock,
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
            TransactionBlock = transactionBlock;
            Fee = fee;
            Error = error;
        }

        public static CashinAggregate StartNew(
            string blockchainType, 
            string hotWalletAddress, 
            string depositWalletAddress, 
            string blockchainAssetId, 
            decimal amount, 
            string assetId)
        {
            return new CashinAggregate(blockchainType, hotWalletAddress, depositWalletAddress, blockchainAssetId, amount, assetId);
        }

        public static CashinAggregate Restore(
            string version,
            CashinState state,
            CashinResult result,
            DateTime creationMoment,
            DateTime? startMoment,
            DateTime? matchingEngineEnrollementMoment,
            DateTime? operationFinishMoment,
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
            long? transactionBlock,
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
                operationFinishMoment,
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
                transactionBlock,
                fee,
                error);
        }

        public bool Start()
        {
            if (!SwitchState(CashinState.Starting, CashinState.Started))
            {
                return false;
            }

            StartMoment = DateTime.UtcNow;

            return true;
        }

        public bool OnEnrolledToMatchingEngine(Guid clientId)
        {
            if (!SwitchState(CashinState.Started, CashinState.EnrolledToMatchingEngine))
            {
                return false;
            }

            MatchingEngineEnrollementMoment = DateTime.UtcNow;

            ClientId = clientId;

            return true;
        }

        public bool OnClientOperationStartRegistered()
        {
            if (!SwitchState(CashinState.EnrolledToMatchingEngine, CashinState.ClientOperationStartIsRegistered))
            {
                return false;
            }

            return true;
        }

        public bool OnOperationCompleted(string transactionHash, long transactionBlock, decimal transactionAmount, decimal fee)
        {
            if (!SwitchState(
                new[] {CashinState.ClientOperationStartIsRegistered, CashinState.EnrolledToMatchingEngine},
                CashinState.OperationIsFinished))
            {
                return false;
            }

            OperationFinishMoment = DateTime.UtcNow;
            
            TransactionHash = transactionHash;
            TransactionAmount = transactionAmount;
            TransactionBlock = transactionBlock;
            Fee = fee;

            Result = CashinResult.Success;

            return true;
        }

        public bool OnOperationFailed(string error)
        {
            if (!SwitchState(
                new[] {CashinState.ClientOperationStartIsRegistered, CashinState.EnrolledToMatchingEngine},
                CashinState.OperationIsFinished))
            {
                return false;
            }

            OperationFinishMoment = DateTime.UtcNow;

            Error = error;

            Result = CashinResult.Failure;

            return true;
        }

        public bool OnMatchingEngineDeduplicationLockRemoved()
        {
            if (!SwitchState(CashinState.OperationIsFinished, CashinState.MatchingEngineDeduplicationLockIsRemoved))
            {
                return false;
            }

            return true;
        }

        private bool SwitchState(CashinState expectedState, CashinState nextState)
        {
            return SwitchState(new[] {expectedState}, nextState);
        }

        private bool SwitchState(IList<CashinState> expectedStates, CashinState nextState)
        {
            if (expectedStates.Contains(State))
            {
                State = nextState;

                return true;
            }

            if (State < expectedStates.Max())
            {
                // Throws to retry and wait until aggregate will be in the required state
                throw new InvalidAggregateStateException(State, expectedStates, nextState);
            }

            if (State > expectedStates.Min())
            {
                // Aggregate already in the next state, so this event can be just ignored
                return false;
            }

            throw new InvalidOperationException("This shouldn't be happened");
        }
    }
}
