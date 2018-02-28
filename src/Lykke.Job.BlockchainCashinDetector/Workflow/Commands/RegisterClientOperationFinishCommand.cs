using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Commands
{
    [Obsolete("Should be removed with next release")]
    [MessagePackObject]
    public class RegisterClientOperationFinishCommand
    {
        [Key(0)]
        public Guid OperationId { get; set; }


        [Key(1)]
        public Guid ClientId { get; set; }

        [Key(2)]
        public string TransactionHash { get; set; }
    }
}
