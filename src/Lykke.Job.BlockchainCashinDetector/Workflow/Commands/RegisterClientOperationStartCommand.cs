using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Commands
{
    [Obsolete("Should be removed with next release")]
    [MessagePackObject]
    public class RegisterClientOperationStartCommand
    {
        [Key(0)]
        public Guid OperationId { get; set; }

        [Key(1)]
        public DateTime Moment { get; set; }

        [Key(2)]
        public decimal Amount { get; set; }

        [Key(3)]
        public string AssetId { get; set; }

        [Key(4)]
        public Guid ClientId { get; set; }

        [Key(5)]
        public string DepositWalletAddress { get; set; }

        [Key(6)]
        public string HotWalletAddress { get; set; }
    }
}
