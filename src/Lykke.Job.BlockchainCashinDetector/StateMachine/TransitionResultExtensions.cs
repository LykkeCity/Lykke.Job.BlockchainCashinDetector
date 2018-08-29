using Lykke.Job.BlockchainCashinDetector.Core.Domain;

namespace Lykke.Job.BlockchainCashinDetector.StateMachine
{
    public static class TransitionResultExtensions
    {
        public static bool ShouldSendCommands(this TransitionResult transitionResult)
        {
            return transitionResult == TransitionResult.Switched ||
                   transitionResult == TransitionResult.AlreadyInTargetState;
        }

        public static bool ShouldSaveAggregate(this TransitionResult transitionResult)
        {
            return transitionResult == TransitionResult.Switched;
        }
    }
}
