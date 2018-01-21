using System;
using ProtoBuf;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Commands
{
    [ProtoContract]
    public class RemoveMatchingEngineDeduplicationLockCommand
    {
        [ProtoMember(1)]
        public Guid OperationId { get; set; }
    }
}