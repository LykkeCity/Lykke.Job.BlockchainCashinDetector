namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public enum CashinState
    {
        Starting,
        Started,
        EnrolledToMatchingEngine,
        ClientOperationStartIsRegistered,
        OperationIsFinished,
        DepositBalanceDetectionsDeduplicationLockIsUpdated,
        MatchingEngineDeduplicationLockIsRemoved,
        ClientOperationFinishtIsRegistered
    }
}
