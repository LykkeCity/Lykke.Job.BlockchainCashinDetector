using System;

namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public enum CashinState
    {
        Starting,
        Started,
        EnrolledToMatchingEngine,
        EnrolledBalanceIncreased,
        [Obsolete("Should be removed with next release")]
        ClientOperationStartIsRegistered,
        OperationIsFinished,
        [Obsolete("Should be removed with next release")]
        MatchingEngineDeduplicationLockIsRemoved,
        [Obsolete("Should be removed with next release")]
        ClientOperationFinishtIsRegistered
    }
}
