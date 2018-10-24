using System;

namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public enum CashinState
    {
        WaitingForActualBalance,
        Started,
        EnrolledToMatchingEngine,
        EnrolledBalanceSet,
        DustEnrolledBalanceSet,
        OperationCompleted,
        OperationFailed,
        EnrolledBalanceReset,
        OutdatedBalance,
        DepositWalletLockIsReleased,
        Rejected
    }
}
