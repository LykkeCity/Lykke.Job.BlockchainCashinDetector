using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.BlockchainCashinDetector.Settings
{
    [UsedImplicitly]
    public class PayInternalServiceClientSettings
    {
        [HttpCheck("/api/isalive")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string ServiceUrl { get; set; }
    }
}
