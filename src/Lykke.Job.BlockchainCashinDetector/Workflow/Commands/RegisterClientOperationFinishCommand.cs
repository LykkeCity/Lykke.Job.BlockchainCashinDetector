using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Commands
{
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
