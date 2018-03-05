using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Commands
{
    [MessagePackObject]
    public class ResetEnrolledBalanceCommand
    {
        [Key(0)]
        public Guid OperationId { get; set; }
        
        [Key(1)]
        public string BlockchainType { get; set; }

        [Key(2)]
        public string BlockchainAssetId { get; set; }

        [Key(3)]
        public string DepositWalletAddress { get; set; }

        [Key(4)]
        public long Block { get; set; }
    }
}
