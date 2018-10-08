using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Events
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class CashinValidatedEvent
    {
        public Guid OperationId { get; set; }
    }
}
