using System;

namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public enum CashinState
    {
        Starting,
        Started,
        EnrolledToMatchingEngine,
        [Obsolete("Should be removed with next release")]
        ClientOperationStartIsRegistered,
        OperationIsFinished,
        DepositBalanceDetectionsDeduplicationLockIsUpdated,
        [Obsolete("Should be removed with next release")]
        MatchingEngineDeduplicationLockIsRemoved,
        [Obsolete("Should be removed with next release")]
        ClientOperationFinishtIsRegistered
    }
}
