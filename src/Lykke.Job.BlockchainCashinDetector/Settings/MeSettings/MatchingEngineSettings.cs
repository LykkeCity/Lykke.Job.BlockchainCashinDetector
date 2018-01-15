using JetBrains.Annotations;

namespace Lykke.Job.BlockchainCashinDetector.Settings.MeSettings
{
    [UsedImplicitly]
    public class MatchingEngineSettings
    {
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public IpEndpointSettings IpEndpoint { get; set; }
    }
}
