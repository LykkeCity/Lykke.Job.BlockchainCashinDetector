using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Events
{
    [MessagePackObject]
    public class EnrolledBalanceIncreasedEvent
    {
        [Key(0)]
        public Guid OperationId { get; set; }

        [Key(1)]
        public decimal Amount { get; set; }

        [Key(2)]
        public string BlockchainType { get; set; }

        [Key(3)]
        public string BlockchainAssetId { get; set; }

        [Key(4)]
        public string DepositWalletAddress { get; set; }
    }
}
