namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public enum CashinState
    {
        Starting,
        Started,
        EnrolledToMatchingEnging,
        OperationIsFinished,
        MatchingEngineDeduplicationLockIsRemoved
    }
}
