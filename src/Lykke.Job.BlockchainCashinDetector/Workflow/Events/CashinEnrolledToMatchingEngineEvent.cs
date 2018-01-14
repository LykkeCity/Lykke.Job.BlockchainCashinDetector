using System;
using ProtoBuf;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Events
{
    /// <summary>
    /// Cashin is enrolled to the ME
    /// </summary>
    [ProtoContract]
    public class CashinEnrolledToMatchingEngineEvent
    {
        [ProtoMember(1)]
        public Guid OperationId { get; set; }

        [ProtoMember(2)]
        public string ClientId { get; set; }

        [ProtoMember(3)]
        public string AssetId { get; set; }
    }
}
