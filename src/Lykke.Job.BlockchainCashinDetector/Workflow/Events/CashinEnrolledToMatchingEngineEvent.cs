using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Events
{
    /// <summary>
    /// Cashin is enrolled to the ME
    /// </summary>
    [MessagePackObject]
    public class CashinEnrolledToMatchingEngineEvent
    {
        [Key(0)]
        public Guid ClientId { get; set; }

        [Key(1)]
        public Guid OperationId { get; set; }
    }
}
