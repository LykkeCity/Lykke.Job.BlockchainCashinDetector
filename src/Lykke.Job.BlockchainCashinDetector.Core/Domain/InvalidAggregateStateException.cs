using System;
using System.Collections.Generic;
using System.Linq;

namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public class InvalidAggregateStateException : Exception
    {
        public InvalidAggregateStateException(CashinState currentState, IList<CashinState> expectedStates, CashinState targetState) :
            base(BuildMessage(currentState, expectedStates, targetState))
        {
            
        }

        private static string BuildMessage(CashinState currentState, IList<CashinState> expectedStates, CashinState targetState)
        {
            var expectedStateMessage = expectedStates.Count == 1
                ? $"{expectedStates} state"
                : $"one of [{string.Join(", ", expectedStates.Select(s => s.ToString()))}] states";

            return $"Cashin state can't be switched: {currentState} -> {targetState}. Waiting for the {expectedStateMessage}.";
        }
    }
}
