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
        public decimal EnrolledBalanceAmount { get; set; }

        [Key(2)]
        public decimal OperationAmount { get; set; }

        [Key(3)]
        public Guid OperationId { get; set; }

        [Key(4)]
        public double MeAmount { get; set; }
    }
}
