using MessagePack;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Events
{
    [MessagePackObject]
    public class DepositBalanceDetectionsDeduplicationLockUpdatedEvent
    {
        [Key(0)]
        public string BlockchainType { get; set; }

        [Key(1)]
        public string BlockchainAssetId { get; set; }

        [Key(2)]
        public long Block { get; set; }

        [Key(3)]
        public string DepositWalletAddress { get; set; }
    }
}
