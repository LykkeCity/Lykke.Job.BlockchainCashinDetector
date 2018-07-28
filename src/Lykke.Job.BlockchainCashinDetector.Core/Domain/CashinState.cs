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
        DepositWalletLockIsReleased
    }
}
