using Lykke.Job.BlockchainCashinDetector.Settings.Assets;
using Lykke.Job.BlockchainCashinDetector.Settings.Blockchain;
using Lykke.Job.BlockchainCashinDetector.Settings.JobSettings;
using Lykke.Job.BlockchainCashinDetector.Settings.MeSettings;
using Lykke.Job.BlockchainCashinDetector.Settings.SlackNotifications;

namespace Lykke.Job.BlockchainCashinDetector.Settings
{
    public class AppSettings
    {
        public BlockchainCashinDetectorSettings BlockchainCashinDetectorJob { get; set; }
        public SlackNotificationsSettings SlackNotifications { get; set; }
        public BlockchainsIntegrationSettings BlockchainsIntegration { get; set; }
        public MatchingEngineSettings MatchingEngineClient { get; set; }
        public AssetsSettings Assets { get; set; }
        public BlockchainWalletsServiceClientSettings BlockchainWalletsServiceClient { get; set; }
    }
}
