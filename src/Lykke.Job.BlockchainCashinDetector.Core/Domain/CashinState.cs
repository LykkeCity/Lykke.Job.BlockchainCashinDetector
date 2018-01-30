namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public enum CashinState
    {
        Starting,
        Started,
        EnrolledToMatchingEngine,
        ClientOperationIsRegistered,
        OperationIsFinished,
        MatchingEngineDeduplicationLockIsRemoved
    }
}
