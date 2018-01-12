using System;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.BlockchainCashinDetector.Settings.JobSettings
{
    public class CqrsSettings
    {
        [AmqpCheck]
        public string RabbitConnectionString { get; set; }
        public TimeSpan RetryDelay { get; set; }
    }
}
