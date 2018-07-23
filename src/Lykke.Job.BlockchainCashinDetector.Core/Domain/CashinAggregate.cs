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

        public bool IsDustCashin => BalanceAmount <= CashinMinimalAmount;
               
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
            decimal? transactionAmount,
            long? transactionBlock,
            string transactionHash,
            CashinState state,
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
                TransactionAmount = transactionAmount                
            };
        }

        public static Guid GetNextId()
        {
            return Guid.NewGuid();
        }

        public static CashinAggregate WaitForActualBalance(
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

        public bool Start(decimal balanceAmount, long balanceBlock, decimal enrolledBalanceAmount, long enrolledBalanceBlock)
        {
            var couldBeStarted = CouldBeStarted(
                balanceAmount, 
                balanceBlock, 
                enrolledBalanceAmount, 
                enrolledBalanceBlock, 
                AssetAccuracy, 
                out var operationAmount,
                out var matchingEngineOperationAmount);

            if(!couldBeStarted)
            {
                return false;
            }

            if (!SwitchState(CashinState.WaitingForActualBalance, CashinState.Started))
            {
                return false;
            }

            BalanceAmount = balanceAmount;
            BalanceBlock = balanceBlock;
            EnrolledBalanceAmount = enrolledBalanceAmount;
            EnrolledBalanceBlock = enrolledBalanceBlock;
            OperationAmount = operationAmount;
            MeAmount = matchingEngineOperationAmount;

            StartMoment = DateTime.UtcNow;

            return true;
        }

        public bool OnEnrolledToMatchingEngine(Guid clientId)
        {
            if (!SwitchState(CashinState.Started, CashinState.EnrolledToMatchingEngine))
            {
                return false;
            }

            ClientId = clientId;

            MatchingEngineEnrollementMoment = DateTime.UtcNow;
            
            return true;
        }

        public bool OnTransactionCompleted(string transactionHash, long transactionBlock, decimal transactionAmount, decimal fee)
        {
            if (!SwitchState(CashinState.EnrolledBalanceSet, CashinState.OperationCompleted))
            {
                return false;
            }

            OperationFinishMoment = DateTime.UtcNow;

            TransactionAmount = transactionAmount;
            TransactionHash = transactionHash;
            TransactionBlock = transactionBlock;
            Fee = fee;
            
            MarkOperationAsFinished(true);

            return true;
        }

        public bool OnEnrolledBalanceReset()
        {
            if (!SwitchState(CashinState.OperationCompleted, CashinState.EnrolledBalanceReset))
            {
                return false;
            }

            EnrolledBalanceResetMoment = DateTime.UtcNow;

            return true;
        }

        public bool OnTransactionFailed(string error)
        {
            if (!SwitchState(CashinState.EnrolledBalanceSet, CashinState.OperationFailed))
            {
                return false;
            }

            Error = error;

            MarkOperationAsFinished(false);

            return true;
        }

        public bool OnEnrolledBalanceSet()
        {
            var nextState = IsDustCashin 
                ? CashinState.DustEnrolledBalanceSet
                : CashinState.EnrolledBalanceSet;

            if (!SwitchState(CashinState.EnrolledToMatchingEngine, nextState))
            {
                return false;
            }
            
            EnrolledBalanceSetMoment = DateTime.UtcNow;

            if (IsDustCashin)
            {
                MarkOperationAsFinished(true);
            }

            return true;
        }
        
        public bool OnDepositWalletLockReleased()
        {
            var validStates = new[]
            {
                CashinState.DustEnrolledBalanceSet,
                CashinState.EnrolledBalanceReset,
                CashinState.OperationFailed                
            };

            if (!SwitchState(validStates, CashinState.DepositWalletLockIsReleased))
            {
                return false;
            }
            
            DepositWalletLockReleasedMoment = DateTime.UtcNow;

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
