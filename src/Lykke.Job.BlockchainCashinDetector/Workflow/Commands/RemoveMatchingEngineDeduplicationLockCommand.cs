using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Commands
{
    [Obsolete("Should be removed with next release")]
    [MessagePackObject]
    public class RemoveMatchingEngineDeduplicationLockCommand
    {
        [Key(0)]
        public Guid OperationId { get; set; }
    }
}
