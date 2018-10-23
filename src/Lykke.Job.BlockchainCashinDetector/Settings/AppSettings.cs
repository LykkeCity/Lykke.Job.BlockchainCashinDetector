using JetBrains.Annotations;
using Lykke.Job.BlockchainCashinDetector.Settings.Assets;
using Lykke.Job.BlockchainCashinDetector.Settings.Blockchain;
using Lykke.Job.BlockchainCashinDetector.Settings.JobSettings;
using Lykke.Job.BlockchainCashinDetector.Settings.MeSettings;
using Lykke.Sdk.Settings;

namespace Lykke.Job.BlockchainCashinDetector.Settings
{
    [UsedImplicitly]
    public class AppSettings : BaseAppSettings
    {
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public BlockchainCashinDetectorSettings BlockchainCashinDetectorJob { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public BlockchainsIntegrationSettings BlockchainsIntegration { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public MatchingEngineSettings MatchingEngineClient { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public AssetsSettings Assets { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public BlockchainWalletsServiceClientSettings BlockchainWalletsServiceClient { get; set; }
    }
}
