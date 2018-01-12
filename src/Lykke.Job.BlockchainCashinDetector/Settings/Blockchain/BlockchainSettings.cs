using JetBrains.Annotations;

namespace Lykke.Job.BlockchainCashinDetector.Settings.Blockchain
{
    [UsedImplicitly]
    public class BlockchainSettings
    {
        public string Type { get; set; }
        public string ApiUrl { get; set; }
        public string SignFacadeUrl { get; set; }
        public string HotWalletAddress { get; set; }
    }
}
