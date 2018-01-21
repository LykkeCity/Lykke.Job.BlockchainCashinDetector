using System;
using ProtoBuf;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Events
{
    [ProtoContract]
    public class MatchingEngineDeduplicationLockRemovedEvent
    {
        [ProtoMember(1)]
        public Guid OperationId { get; set; }
    }
}
