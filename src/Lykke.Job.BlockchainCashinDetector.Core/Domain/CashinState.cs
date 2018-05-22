using System;

namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public enum CashinState
    {
        Starting,
        Started,
        EnrolledToMatchingEngine,
        EnrolledBalanceSet,
        OperationIsFinished,
        EnrolledBalanceReset
    }
}
