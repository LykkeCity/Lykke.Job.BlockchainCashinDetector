using System;

namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public enum CashinState
    {
        WaitingForActualBalance,
        Started,
        ClientRetrieved,
        OperationAccepted,
        OperationRejected,
        EnrolledToMatchingEngine,
        EnrolledBalanceSet,
        DustEnrolledBalanceSet,
        OperationCompleted,
        OperationFailed,
        EnrolledBalanceReset,
        OutdatedBalance,
        DepositWalletLockIsReleased
    }
}
