using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Commands
{
    [MessagePackObject]
    public class RemoveMatchingEngineDeduplicationLockCommand
    {
        [Key(0)]
        public Guid OperationId { get; set; }
    }
}
