using MessagePack;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Events
{
    [MessagePackObject]
    public class DepositBalanceDetectedEvent
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
        public string HotWalletAddress { get; set; }

        [Key(5)]
        public string AssetId { get; set; }

        [Key(6)]
        public decimal OperationAmount { get; set; }
    }
}
