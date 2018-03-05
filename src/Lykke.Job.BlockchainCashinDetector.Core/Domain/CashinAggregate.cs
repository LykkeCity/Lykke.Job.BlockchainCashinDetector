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
        public DateTime? EnrolledBalanceIncreasedMoment { get; private set; }
        public DateTime? EnrolledBalanceResetMoment { get; private set; }
        public DateTime? OperationFinishMoment { get; private set; }
        

        public Guid OperationId { get; }
        public string BlockchainType { get; }
        public string HotWalletAddress { get; }
        public string DepositWalletAddress { get; }
        public string BlockchainAssetId { get; }
        public decimal TransactionAmount { get; }
        public string AssetId { get; }
        public decimal OperationAmount { get; }

        public Guid? ClientId { get; private set; }
        public string TransactionHash { get; private set; }
        public decimal? ActualTransactionAmount { get; private set; }
        public long? TransactionBlock { get; private set; }
        public decimal? Fee { get; private set; }
        public string Error { get; private set; }

        public bool IsFinished => Result == CashinResult.Success || Result == CashinResult.Failure;
        public bool IsDustCashin => TransactionAmount == 0;


        private CashinAggregate(
            string blockchainType, 
            string hotWalletAddress, 
            string depositWalletAddress, 
            string blockchainAssetId, 
            decimal transactionAmount, 
            string assetId,
            decimal operationAmount)
        {
            CreationMoment = DateTime.UtcNow;

            OperationId = Guid.NewGuid();
            BlockchainType = blockchainType;
            HotWalletAddress = hotWalletAddress;
            DepositWalletAddress = depositWalletAddress;
            BlockchainAssetId = blockchainAssetId;
            TransactionAmount = transactionAmount;
            AssetId = assetId;
            OperationAmount = operationAmount;

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
            DateTime? enrolledBalanceIncreasedMoment,
            DateTime? enrolledBalanceResetMoment,
            DateTime? operationFinishMoment,
            Guid operationId,
            string blockchainType,
            string hotWalletAddress,
            string depositWalletAddress,
            string blockchainAssetId,
            decimal transactionAmount,
            decimal operationAmount,
            Guid? clientId,
            string assetId,
            string transactionHash,
            decimal? actualTransactionAmount,
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
            EnrolledBalanceIncreasedMoment = enrolledBalanceIncreasedMoment;
            EnrolledBalanceResetMoment = enrolledBalanceResetMoment;
            OperationFinishMoment = operationFinishMoment;

            OperationId = operationId;
            BlockchainType = blockchainType;
            HotWalletAddress = hotWalletAddress;
            DepositWalletAddress = depositWalletAddress;
            BlockchainAssetId = blockchainAssetId;
            TransactionAmount = transactionAmount;
            OperationAmount = operationAmount;

            ClientId = clientId;
            AssetId = assetId;
            TransactionHash = transactionHash;
            ActualTransactionAmount = actualTransactionAmount;
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
            string assetId,
            decimal operationAmount)
        {
            return new CashinAggregate(blockchainType, hotWalletAddress, depositWalletAddress, blockchainAssetId, amount, assetId, operationAmount);
        }

        public static CashinAggregate Restore(
            string version,
            CashinState state,
            CashinResult result,
            DateTime creationMoment,
            DateTime? startMoment,
            DateTime? matchingEngineEnrollementMoment,
            DateTime? enrolledBalanceIncreasedMoment,
            DateTime? enrolledBalanceResetMoment,
            DateTime? operationFinishMoment,
            Guid operationId,
            string blockchainType,
            string hotWalletAddress,
            string depositWalletAddress,
            string blockchainAssetId,
            decimal transactionAmount,
            decimal operationAmount,
            Guid? clientId,
            string assetId,
            string transactionHash,
            decimal? actualTransactionAmount,
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
                enrolledBalanceIncreasedMoment,
                enrolledBalanceResetMoment,
                operationFinishMoment,
                operationId,
                blockchainType,
                hotWalletAddress,
                depositWalletAddress,
                blockchainAssetId,
                transactionAmount,
                operationAmount,
                clientId,
                assetId,
                transactionHash,
                actualTransactionAmount,
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

        public bool OnTransactionCompleted(string transactionHash, long transactionBlock, decimal actualTransactionAmount, decimal fee)
        {
            var expectedStates = new[]
            {
                CashinState.ClientOperationStartIsRegistered,
                CashinState.EnrolledBalanceIncreased
            };

            if (!SwitchState(expectedStates, CashinState.OperationIsFinished))
            {
                return false;
            }

            OperationFinishMoment = DateTime.UtcNow;
            
            TransactionHash = transactionHash;
            ActualTransactionAmount = actualTransactionAmount;
            TransactionBlock = transactionBlock;
            Fee = fee;
            
            return true;
        }

        public bool OnEnrolledBalanceReset()
        {
            if (!SwitchState(CashinState.OperationIsFinished, CashinState.EnrolledBalanceReset))
            {
                return false;
            }

            EnrolledBalanceResetMoment = DateTime.UtcNow;

            Result = CashinResult.Success;
            OperationFinishMoment = DateTime.UtcNow;

            return true;
        }

        public bool OnTransactionFailed(string error)
        {
            var expectedStates = new[]
            {
                CashinState.ClientOperationStartIsRegistered,
                CashinState.EnrolledBalanceIncreased
            };

            if (!SwitchState(expectedStates, CashinState.OperationIsFinished))
            {
                return false;
            }

            Error = error;

            MarkOperationAsFinished(false);

            return true;
        }

        public bool OnEnrolledBalanceIncreased()
        {
            var nextState = IsDustCashin
                ? CashinState.OperationIsFinished
                : CashinState.EnrolledBalanceIncreased;

            if (!SwitchState(CashinState.EnrolledToMatchingEngine, nextState))
            {
                return false;
            }
            
            EnrolledBalanceIncreasedMoment = DateTime.UtcNow;

            if (IsDustCashin)
            {
                MarkOperationAsFinished(true);
            }

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

        private void MarkOperationAsFinished(bool isSuccessful)
        {
            OperationFinishMoment = DateTime.UtcNow;
            Result = isSuccessful ? CashinResult.Success : CashinResult.Failure;
        }
    }
}
