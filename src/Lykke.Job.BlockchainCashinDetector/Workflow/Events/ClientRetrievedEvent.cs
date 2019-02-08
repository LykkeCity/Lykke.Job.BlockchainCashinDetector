using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Events
{
    [MessagePackObject]
    public class ClientRetrievedEvent
    {
        [Key(0)]
        public Guid OperationId { get; set; }

        [Key(1)]
        public Guid ClientId { get; set; }
    }
}