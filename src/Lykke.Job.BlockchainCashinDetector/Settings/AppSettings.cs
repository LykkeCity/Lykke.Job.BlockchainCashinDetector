using JetBrains.Annotations;
using Lykke.Job.BlockchainCashinDetector.Settings.Assets;
using Lykke.Job.BlockchainCashinDetector.Settings.JobSettings;
using Lykke.Job.BlockchainCashinDetector.Settings.MeSettings;
using Lykke.Sdk.Settings;
using Lykke.Service.BlockchainSettings.Client;

namespace Lykke.Job.BlockchainCashinDetector.Settings
{
    [UsedImplicitly]
    public class AppSettings : BaseAppSettings
    {
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public BlockchainCashinDetectorSettings BlockchainCashinDetectorJob { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public MatchingEngineSettings MatchingEngineClient { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public AssetsSettings Assets { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public BlockchainWalletsServiceClientSettings BlockchainWalletsServiceClient { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public BlockchainSettingsServiceClientSettings BlockchainSettingsServiceClient { get; set; }
    }
}
