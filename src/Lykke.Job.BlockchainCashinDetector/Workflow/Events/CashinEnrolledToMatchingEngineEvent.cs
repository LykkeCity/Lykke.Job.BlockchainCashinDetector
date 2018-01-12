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
        public string BlockchainType { get; set; }
        [ProtoMember(3)]
        public string BlockchainDepositWalletAddress { get; set; }
        [ProtoMember(4)]
        public string BlockchainAssetId { get; set; }
        [ProtoMember(5)]
        public string AssetId { get; set; }
        [ProtoMember(6)]
        public string ClientId { get; set; }
        [ProtoMember(7)]
        public decimal Amount { get; set; }
    }
}
