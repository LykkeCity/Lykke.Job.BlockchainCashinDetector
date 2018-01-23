using System;

namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public class InvalidAggregateStateException : Exception
    {
        public InvalidAggregateStateException(CashinState currentState, CashinState expectedState, CashinState targetState) :
            base(BuildMessage(currentState, expectedState, targetState))
        {
            
        }

        private static string BuildMessage(CashinState currentState, CashinState expectedState, CashinState targetState)
        {
            return $"Cashin state can't be switched: {currentState} -> {targetState}. Waiting for the {expectedState} state.";
        }
    }
}
