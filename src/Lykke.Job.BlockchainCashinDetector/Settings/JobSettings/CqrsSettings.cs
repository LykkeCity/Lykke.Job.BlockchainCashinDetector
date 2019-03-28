using System;
using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.BlockchainCashinDetector.Settings.JobSettings
{
    [UsedImplicitly]
    public class CqrsSettings
    {
        [AmqpCheck]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string RabbitConnectionString { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public TimeSpan RetryDelay { get; set; }

        [Optional]
        public string Vhost { get; set; }
    }
}
