using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Events
{
    [MessagePackObject]
    public class EnrolledBalanceResetEvent
    {
        [Key(0)]
        public Guid OperationId { get; set; }
    }
}
