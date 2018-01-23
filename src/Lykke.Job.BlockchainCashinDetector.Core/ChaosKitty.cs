using System;

namespace Lykke.Job.BlockchainCashinDetector.Core
{
    public static class ChaosKitty
    {
        private static readonly Random Randmom = new Random();
        private static double _stateOfChaos;

        public static double StateOfChaos
        {
            set
            {
                if (value < 0.0 || value > 1.0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Should be in the range [0, 1]");
                }

                _stateOfChaos = value;
            }
        }

        public static void Meow(object tag)
        {
            if (_stateOfChaos < 1e-10)
            {
                return;
            }

            if (Randmom.NextDouble() < _stateOfChaos)
            {
                throw new Exception($"Meow: {tag}");
            }
        }
    }
}
