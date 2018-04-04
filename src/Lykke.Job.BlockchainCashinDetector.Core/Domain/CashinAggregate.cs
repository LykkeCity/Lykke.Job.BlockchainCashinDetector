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
        public DateTime? EnrolledBalanceSetMoment { get; private set; }
        public DateTime? EnrolledBalanceResetMoment { get; private set; }
        public DateTime? OperationFinishMoment { get; private set; }
        

        public Guid OperationId { get; }
        public string BlockchainType { get; }
        public string HotWalletAddress { get; }
        public string DepositWalletAddress { get; }
        public string BlockchainAssetId { get; }
        public string AssetId { get; }
        public decimal BalanceAmount { get; }
        public long BalanceBlock { get; }
        public decimal CashinMinimalAmount { get; }


        public Guid? ClientId { get; private set; }
        public string TransactionHash { get; private set; }
        public long? TransactionBlock { get; private set; }
        public decimal? Fee { get; private set; }
        public string Error { get; private set; }
        public decimal? EnrolledBalanceAmount { get; private set; }
        public decimal? OperationAmount { get; private set; }
        public decimal? TransactionAmount { get; private set; }


        public bool IsFinished => Result == CashinResult.Success || Result == CashinResult.Failure;
        public bool IsDustCashin => BalanceAmount <= CashinMinimalAmount;


        private CashinAggregate(
            string assetId,
            decimal balanceAmount,
            long balanceBlock,
            string blockchainAssetId,
            string blockchainType,
            decimal cashinMinimalAmount,
            string depositWalletAddress,
            string hotWalletAddress)
        {
            AssetId = assetId;
            BalanceAmount = balanceAmount;
            BalanceBlock = balanceBlock;
            BlockchainAssetId = blockchainAssetId;
            BlockchainType = blockchainType;
            CashinMinimalAmount = cashinMinimalAmount;
            DepositWalletAddress = depositWalletAddress;
            HotWalletAddress = hotWalletAddress;

            CreationMoment = DateTime.UtcNow;
            OperationId = Guid.NewGuid();
            Result = CashinResult.Unknown;
            State = CashinState.Starting;
        }
        
        private CashinAggregate(
            string assetId,
            decimal balanceAmount,
            long balanceBlock,
            string blockchainAssetId,
            string blockchainType,
            decimal cashinMinimalAmount,
            DateTime creationMoment,
            string depositWalletAddress,
            string hotWalletAddress,
            Guid operationId,
            string version) : this(
            assetId: assetId,
            balanceAmount: balanceAmount,
            balanceBlock: balanceBlock,
            blockchainAssetId: blockchainAssetId,
            blockchainType: blockchainType,
            cashinMinimalAmount: cashinMinimalAmount,
            depositWalletAddress: depositWalletAddress,
            hotWalletAddress: hotWalletAddress)
        {
            CreationMoment = creationMoment;
            OperationId = operationId;
            Version = version;
        }

        public static CashinAggregate StartNew(
            string assetId,
            decimal balanceAmount,
            long balanceBlock,
            string blockchainAssetId,
            string blockchainType, 
            decimal cashinMinimalAmount,
            string depositWalletAddress,
            string hotWalletAddress)
        {
            return new CashinAggregate
            (
                assetId: assetId,
                balanceAmount: balanceAmount,
                balanceBlock: balanceBlock,
                blockchainAssetId: blockchainAssetId,
                blockchainType: blockchainType,
                cashinMinimalAmount: cashinMinimalAmount,
                depositWalletAddress: depositWalletAddress,
                hotWalletAddress: hotWalletAddress
            );
        }

        public static CashinAggregate Restore(
            Guid? clientId,
            string assetId,
            decimal balanceAmount,
            long balanceBlock,
            string blockchainAssetId,
            string blockchainType,
            decimal cashinMinimalAmount,
            DateTime creationMoment,
            string depositWalletAddress,
            decimal? enrolledBalanceAmount,
            DateTime? enrolledBalanceResetMoment,
            DateTime? enrolledBalanceSetMoment,
            string error,
            decimal? fee,
            string hotWalletAddress,
            DateTime? matchingEngineEnrollementMoment,
            decimal? operationAmount,
            DateTime? operationFinishMoment,
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
                balanceAmount: balanceAmount,
                balanceBlock: balanceBlock,
                blockchainAssetId: blockchainAssetId,
                blockchainType: blockchainType,
                cashinMinimalAmount: cashinMinimalAmount,
                creationMoment: creationMoment,
                depositWalletAddress: depositWalletAddress,
                hotWalletAddress: hotWalletAddress,
                operationId: operationId,
                version: version
            )
            {
                ClientId = clientId,
                EnrolledBalanceAmount = enrolledBalanceAmount,
                EnrolledBalanceResetMoment = enrolledBalanceResetMoment,
                EnrolledBalanceSetMoment = enrolledBalanceSetMoment,
                Error = error,
                Fee = fee,
                MatchingEngineEnrollementMoment = matchingEngineEnrollementMoment,
                OperationAmount = operationAmount,
                OperationFinishMoment = operationFinishMoment,
                Result = result,
                StartMoment = startMoment,
                State = state,
                TransactionAmount = transactionAmount,
                TransactionBlock = transactionBlock,
                TransactionHash = transactionHash
            };
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

        public bool OnEnrolledToMatchingEngine(Guid clientId, decimal enrolledBalanceAmount, decimal operationAmount)
        {
            if (!SwitchState(CashinState.Started, CashinState.EnrolledToMatchingEngine))
            {
                return false;
            }

            ClientId = clientId;
            EnrolledBalanceAmount = enrolledBalanceAmount;
            OperationAmount = operationAmount;

            MatchingEngineEnrollementMoment = DateTime.UtcNow;
            
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

        public bool OnTransactionCompleted(string transactionHash, long transactionBlock, decimal transactionAmount, decimal fee)
        {
            var expectedStates = new[]
            {
                CashinState.ClientOperationStartIsRegistered,
                CashinState.EnrolledBalanceSet
            };

            if (!SwitchState(expectedStates, CashinState.OperationIsFinished))
            {
                return false;
            }

            OperationFinishMoment = DateTime.UtcNow;

            TransactionAmount = transactionAmount;
            TransactionHash = transactionHash;
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
                CashinState.EnrolledBalanceSet
            };

            if (!SwitchState(expectedStates, CashinState.OperationIsFinished))
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
                ? CashinState.OperationIsFinished
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
