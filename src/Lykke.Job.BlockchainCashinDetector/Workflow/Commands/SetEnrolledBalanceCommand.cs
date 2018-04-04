using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Commands
{
    [MessagePackObject]
    public class SetEnrolledBalanceCommand
    {
        [Key(0)]
        public string BlockchainAssetId { get; set; }

        [Key(1)]
        public string BlockchainType { get; set; }

        [Key(2)]
        public string DepositWalletAddress { get; set; }

        [Key(3)]
        public decimal EnrolledBalanceAmount { get; set; }

        [Key(4)]
        public decimal OperationAmount { get; set; }

        [Key(5)]
        public Guid OperationId { get; set; }
    }
}
