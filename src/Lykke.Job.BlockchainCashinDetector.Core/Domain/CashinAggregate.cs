using System;
using System.Collections.Generic;
using System.Linq;
using Common;

namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public class CashinAggregate
    {
        public string Version { get; }
        
        public CashinState State { get; private set; }
        public CashinResult Result { get; private set; }
        
        public DateTime CreationMoment { get; }
        public DateTime? StartMoment { get; private set; }
        public DateTime? BalanceOutdatingMoment { get; private set; }
        public DateTime? MatchingEngineEnrollementMoment { get; private set; }
        public DateTime? EnrolledBalanceSetMoment { get; private set; }
        public DateTime? EnrolledBalanceResetMoment { get; private set; }
        public DateTime? OperationFinishMoment { get; private set; }
        public DateTime? DepositWalletLockReleasedMoment{ get; private set; }
        
        public Guid OperationId { get; }
        public string BlockchainType { get; }
        public string HotWalletAddress { get; }
        public string DepositWalletAddress { get; }
        public string BlockchainAssetId { get; }
        public string AssetId { get; }
        public int AssetAccuracy { get; }
        public decimal CashinMinimalAmount { get; }
        
        public Guid? ClientId { get; private set; }
        public string TransactionHash { get; private set; }
        public long? TransactionBlock { get; private set; }
        public decimal? Fee { get; private set; }
        public string Error { get; private set; }
        public decimal? BalanceAmount { get; private set; }
        public long? BalanceBlock { get; private set; }
        public decimal? EnrolledBalanceAmount { get; private set; }
        public long? EnrolledBalanceBlock { get; private set; }
        public decimal? OperationAmount { get; private set; }
        public double? MeAmount { get; private set; }
        public decimal? TransactionAmount { get; private set; }
        public bool? IsDustCashin { get; private set; }
               
        private CashinAggregate(
            Guid operationId,
            string assetId,
            int assetAccuracy,
            string blockchainAssetId,
            string blockchainType,
            decimal cashinMinimalAmount,
            string depositWalletAddress,
            string hotWalletAddress,
            DateTime creationMoment,            
            CashinResult result,
            CashinState state,
            string version)
        {
            OperationId = operationId;
            AssetId = assetId;
            AssetAccuracy = assetAccuracy;
            BlockchainAssetId = blockchainAssetId;
            BlockchainType = blockchainType;
            CashinMinimalAmount = cashinMinimalAmount;
            DepositWalletAddress = depositWalletAddress;
            HotWalletAddress = hotWalletAddress;
            CreationMoment = creationMoment;
            Result = result;
            State = state;
            Version = version;
        }

        public static CashinAggregate Restore(
            Guid? clientId,
            string assetId,
            int assetAccuracy,
            decimal? balanceAmount,
            long? balanceBlock,
            string blockchainAssetId,
            string blockchainType,
            decimal cashinMinimalAmount,
            DateTime creationMoment,
            string depositWalletAddress,
            decimal? enrolledBalanceAmount,
            long? enrolledBalanceBlock,
            DateTime? enrolledBalanceResetMoment,
            DateTime? enrolledBalanceSetMoment,
            string error,
            decimal? fee,
            string hotWalletAddress,
            DateTime? matchingEngineEnrollementMoment,
            decimal? operationAmount,
            double? meAmount,
            DateTime? operationFinishMoment,
            DateTime? depositWalletLockReleasedMoment,
            Guid operationId,
            CashinResult result,
            DateTime? startMoment,
            DateTime? balanceOutdatingMoment,
            decimal? transactionAmount,
            long? transactionBlock,
            string transactionHash,
            CashinState state,
            bool? isDustCashin,
            string version)
        {
            return new CashinAggregate
            (
                assetId: assetId,
                assetAccuracy: assetAccuracy,
                blockchainAssetId: blockchainAssetId,
                blockchainType: blockchainType,
                cashinMinimalAmount: cashinMinimalAmount,
                depositWalletAddress: depositWalletAddress,
                hotWalletAddress: hotWalletAddress,
                creationMoment: creationMoment,
                operationId: operationId,
                result: result,
                state: state,
                version: version
            )
            {
                StartMoment = startMoment,
                BalanceOutdatingMoment = balanceOutdatingMoment,
                MatchingEngineEnrollementMoment = matchingEngineEnrollementMoment,
                EnrolledBalanceSetMoment = enrolledBalanceSetMoment,
                EnrolledBalanceResetMoment = enrolledBalanceResetMoment,
                OperationFinishMoment = operationFinishMoment,
                DepositWalletLockReleasedMoment = depositWalletLockReleasedMoment,

                ClientId = clientId,
                TransactionHash = transactionHash,
                TransactionBlock = transactionBlock,
                Fee = fee,
                Error = error,
                BalanceAmount = balanceAmount,
                BalanceBlock = balanceBlock,
                EnrolledBalanceAmount = enrolledBalanceAmount,
                EnrolledBalanceBlock = enrolledBalanceBlock,
                OperationAmount = operationAmount,
                MeAmount = meAmount,
                TransactionAmount = transactionAmount,
                IsDustCashin = isDustCashin
            };
        }

        public static Guid GetNextId()
        {
            return Guid.NewGuid();
        }

        public static CashinAggregate StartWaitingForActualBalance(
            Guid operationId,
            string assetId,
            int assetAccuracy,
            string blockchainAssetId,
            string blockchainType, 
            decimal cashinMinimalAmount,
            string depositWalletAddress,
            string hotWalletAddress)
        {
            return new CashinAggregate
            (
                operationId: operationId,
                assetId: assetId,
                assetAccuracy: assetAccuracy,
                blockchainAssetId: blockchainAssetId,
                blockchainType: blockchainType,
                cashinMinimalAmount: cashinMinimalAmount,
                depositWalletAddress: depositWalletAddress,
                hotWalletAddress: hotWalletAddress,
                creationMoment: DateTime.UtcNow,
                result: CashinResult.Unknown,
                state: CashinState.WaitingForActualBalance,
                version: null
            );
        }

        public static bool CouldBeStarted(decimal balanceAmount, long balanceBlock, decimal enrolledBalanceAmount, long enrolledBalanceBlock, int assetAccuracy)
        {
            return CouldBeStarted(balanceAmount, balanceBlock, enrolledBalanceAmount, enrolledBalanceBlock, assetAccuracy, out var _, out var _);
        }

        public TransitionResult Start(decimal balanceAmount, long balanceBlock, decimal enrolledBalanceAmount, long enrolledBalanceBlock)
        {
            var couldBeStarted = CouldBeStarted(
                balanceAmount, 
                balanceBlock, 
                enrolledBalanceAmount, 
                enrolledBalanceBlock, 
                AssetAccuracy, 
                out var operationAmount,
                out var matchingEngineOperationAmount);

            var nextState = couldBeStarted
                ? CashinState.Started
                : CashinState.OutdatedBalance;

            switch (SwitchState(CashinState.WaitingForActualBalance, nextState))
            {
                case TransitionResult.AlreadyInFutureState:
                    return TransitionResult.AlreadyInFutureState;

                case TransitionResult.AlreadyInTargetState:
                    return TransitionResult.AlreadyInTargetState;
            }

            if(couldBeStarted)
            {
                BalanceAmount = balanceAmount;
                BalanceBlock = balanceBlock;
                EnrolledBalanceAmount = enrolledBalanceAmount;
                EnrolledBalanceBlock = enrolledBalanceBlock;
                OperationAmount = operationAmount;
                MeAmount = matchingEngineOperationAmount;
                IsDustCashin = balanceAmount <= CashinMinimalAmount;

                StartMoment = DateTime.UtcNow;

                return TransitionResult.Switched;
            }

            Result = CashinResult.OutdatedBalance;

            BalanceOutdatingMoment = DateTime.UtcNow;

            return TransitionResult.Switched;
        }

        public TransitionResult OnEnrolledToMatchingEngine(Guid clientId)
        {
            switch (SwitchState(CashinState.Started, CashinState.EnrolledToMatchingEngine))
            {
                case TransitionResult.AlreadyInFutureState:
                    return TransitionResult.AlreadyInFutureState;

                case TransitionResult.AlreadyInTargetState:
                    return TransitionResult.AlreadyInTargetState;
            }

            ClientId = clientId;

            MatchingEngineEnrollementMoment = DateTime.UtcNow;
            
            return TransitionResult.Switched;
        }

        public TransitionResult OnTransactionCompleted(string transactionHash, long transactionBlock, decimal transactionAmount, decimal fee)
        {
            switch (SwitchState(CashinState.EnrolledBalanceSet, CashinState.OperationCompleted))
            {
                case TransitionResult.AlreadyInFutureState:
                    return TransitionResult.AlreadyInFutureState;

                case TransitionResult.AlreadyInTargetState:
                    return TransitionResult.AlreadyInTargetState;
            }

            OperationFinishMoment = DateTime.UtcNow;

            TransactionAmount = transactionAmount;
            TransactionHash = transactionHash;
            TransactionBlock = transactionBlock;
            Fee = fee;
            
            MarkOperationAsFinished(true);

            return TransitionResult.Switched;
        }

        public TransitionResult OnEnrolledBalanceReset()
        {
            switch (SwitchState(CashinState.OperationCompleted, CashinState.EnrolledBalanceReset))
            {
                case TransitionResult.AlreadyInFutureState:
                    return TransitionResult.AlreadyInFutureState;

                case TransitionResult.AlreadyInTargetState:
                    return TransitionResult.AlreadyInTargetState;
            }

            EnrolledBalanceResetMoment = DateTime.UtcNow;

            return TransitionResult.Switched;
        }

        public TransitionResult OnTransactionFailed(string error)
        {
            switch (SwitchState(CashinState.EnrolledBalanceSet, CashinState.OperationFailed))
            {
                case TransitionResult.AlreadyInFutureState:
                    return TransitionResult.AlreadyInFutureState;

                case TransitionResult.AlreadyInTargetState:
                    return TransitionResult.AlreadyInTargetState;
            }

            Error = error;

            MarkOperationAsFinished(false);

            return TransitionResult.Switched;
        }

        public void OnTransactionFailedCodeMapp(CashinResult errorCode)
        {
            Result = errorCode;
        }

        public TransitionResult OnEnrolledBalanceSet()
        {
            if (!IsDustCashin.HasValue)
            {
                throw new InvalidOperationException("IsDustCashin should be not null here");
            }

            var nextState = IsDustCashin.Value
                ? CashinState.DustEnrolledBalanceSet
                : CashinState.EnrolledBalanceSet;

            switch (SwitchState(CashinState.EnrolledToMatchingEngine, nextState))
            {
                case TransitionResult.AlreadyInFutureState:
                    return TransitionResult.AlreadyInFutureState;

                case TransitionResult.AlreadyInTargetState:
                    return TransitionResult.AlreadyInTargetState;
            }
            
            EnrolledBalanceSetMoment = DateTime.UtcNow;

            // ReSharper disable once PossibleInvalidOperationException
            if (IsDustCashin.Value)
            {
                MarkOperationAsFinished(true);
            }

            return TransitionResult.Switched;
        }
        
        public TransitionResult OnDepositWalletLockReleased()
        {
            var validStates = new[]
            {
                CashinState.OutdatedBalance,
                CashinState.DustEnrolledBalanceSet,
                CashinState.EnrolledBalanceReset,
                CashinState.OperationFailed                
            };

            switch (SwitchState(validStates, CashinState.DepositWalletLockIsReleased))
            {
                case TransitionResult.AlreadyInFutureState:
                    return TransitionResult.AlreadyInFutureState;

                case TransitionResult.AlreadyInTargetState:
                    return TransitionResult.AlreadyInTargetState;
            }
            
            DepositWalletLockReleasedMoment = DateTime.UtcNow;

            return TransitionResult.Switched;
        }

        private TransitionResult SwitchState(CashinState expectedState, CashinState nextState)
        {
            return SwitchState(new[] {expectedState}, nextState);
        }

        private TransitionResult SwitchState(IList<CashinState> expectedStates, CashinState nextState)
        {
            if (expectedStates.Contains(State))
            {
                State = nextState;

                return TransitionResult.Switched;
            }

            if (State < expectedStates.Max())
            {
                // Throws to retry and wait until aggregate will be in the required state
                throw new InvalidAggregateStateException(State, expectedStates, nextState);
            }

            if (State > expectedStates.Min())
            {
                // Aggregate already in the next state, so this event can be just ignored
                return State == nextState
                    ? TransitionResult.AlreadyInTargetState
                    : TransitionResult.AlreadyInFutureState;
            }

            throw new InvalidOperationException("This shouldn't be happened");
        }

        private static bool CouldBeStarted(
            decimal balanceAmount, 
            long balanceBlock, 
            decimal enrolledBalanceAmount, 
            long enrolledBalanceBlock, 
            int assetAccuracy,
            out decimal operationAmount,
            out double matchingEngineOperationAmount)
        {
            operationAmount = 0;
            matchingEngineOperationAmount = 0;

            if (balanceBlock < enrolledBalanceBlock)
            {
                // This balance was already processed
                return false;
            }

            operationAmount = balanceAmount - enrolledBalanceAmount;

            if (operationAmount <= 0)
            {
                // Nothing to transfer
                return false;
            }

            matchingEngineOperationAmount = ((double)operationAmount).TruncateDecimalPlaces(assetAccuracy);

            if (matchingEngineOperationAmount <= 0)
            {
                // Nothing to enroll to the ME
                return false;
            }

            return true;
        }

        private void MarkOperationAsFinished(bool isSuccessful)
        {
            OperationFinishMoment = DateTime.UtcNow;
            Result = isSuccessful ? CashinResult.Success : CashinResult.Failure;
        }
    }
}
