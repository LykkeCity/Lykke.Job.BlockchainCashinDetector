using MessagePack;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Commands
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class LockDepositWalletCommand
    {
        public string BlockchainType { get; set; }
        public string BlockchainAssetId { get; set; }
        public string DepositWalletAddress { get; set; }
        public decimal DepositWalletBalance { get; set; }
        public long DepositWalletBlock { get; set; }
        public string AssetId { get; set; }
        public int AssetAccuracy { get; set; }
        public int BlockchainAssetAccuracy { get; set; }
        public decimal CashinMinimalAmount { get; set; }
        public string HotWalletAddress { get; set; }
    }
}
