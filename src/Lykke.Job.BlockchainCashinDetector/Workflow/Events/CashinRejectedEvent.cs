using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Events
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class CashinRejectedEvent
    {
        public Guid OperationId { get; set; }
    }
}
