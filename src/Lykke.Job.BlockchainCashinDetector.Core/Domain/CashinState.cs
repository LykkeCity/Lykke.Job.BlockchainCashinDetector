using System;

namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public enum CashinState
    {
        Starting,
        Started,
        EnrolledToMatchingEngine,
        EnrolledBalanceSet,
        [Obsolete("Should be removed with next release")]
        ClientOperationStartIsRegistered,
        OperationIsFinished,
        EnrolledBalanceReset,
        [Obsolete("Should be removed with next release")]
        MatchingEngineDeduplicationLockIsRemoved
    }
}
