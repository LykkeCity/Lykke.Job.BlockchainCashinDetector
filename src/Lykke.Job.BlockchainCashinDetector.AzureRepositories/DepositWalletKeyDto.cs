using Lykke.Job.BlockchainCashinDetector.Core.Domain;

namespace Lykke.Job.BlockchainCashinDetector.AzureRepositories
{
    public class DepositWalletKeyDto : IDepositWalletKey
    {
        public string BlockchainType { get; set; }

        public string BlockchainAssetId { get; set; }
        
        public string DepositWalletAddress { get; set; }
    }
}
