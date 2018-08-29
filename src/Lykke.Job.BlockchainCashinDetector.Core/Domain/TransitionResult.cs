namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public enum TransitionResult
    {
        Switched,
        AlreadyInTargetState,
        AlreadyInFutureState
    }
}
