using System;
using ProtoBuf;

namespace Lykke.Job.BlockchainCashinDetector.Core.Domain.Cashin.Commands
{
    [ProtoContract]
    /// <summary>
    /// Command to enroll cashin to the client ME account
    /// </summary>
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
