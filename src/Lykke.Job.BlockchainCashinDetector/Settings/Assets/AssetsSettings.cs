using System;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.BlockchainCashinDetector.Settings.Assets
{
    public class AssetsSettings
    {
        [HttpCheck("/api/isalive")]
        public string ServiceUrl { get; set; }
        public TimeSpan CacheExpirationPeriod { get; set; }
    }
}
