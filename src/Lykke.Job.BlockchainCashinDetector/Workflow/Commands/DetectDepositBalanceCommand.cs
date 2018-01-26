using MessagePack;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Commands
{
    /// <summary>
    /// Command to detect active positive balance on the deposit wallet
    /// </summary>
    [MessagePackObject]
    public class DetectDepositBalanceCommand
    {
        [Key(0)]
        public string BlockchainType { get; set; }
        [Key(1)]
        public string DepositWalletAddress { get; set; }
        [Key(2)]
        public string BlockchainAssetId { get; set; }
        [Key(3)]
        public decimal Amount { get; set; }
        [Key(4)]
        public string AssetId { get; set; }
    }
}
