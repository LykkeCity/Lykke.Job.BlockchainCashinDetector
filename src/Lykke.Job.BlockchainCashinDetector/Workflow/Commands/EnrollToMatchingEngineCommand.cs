using System;
using ProtoBuf;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Commands
{
    /// <summary>
    /// Command to enroll cashin to the client ME account
    /// </summary>
    [ProtoContract]
    public class EnrollToMatchingEngineCommand
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
        public decimal Amount { get; set; }
    }
}
