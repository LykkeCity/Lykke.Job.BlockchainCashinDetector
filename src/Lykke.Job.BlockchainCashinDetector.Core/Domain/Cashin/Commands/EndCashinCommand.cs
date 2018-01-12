using System;
using ProtoBuf;

namespace Lykke.Job.BlockchainCashinDetector.Core.Domain.Cashin.Commands
{
    /// <summary>
    /// Command to end cashin process
    /// </summary>
    [ProtoContract]
    public class EndCashinCommand
    {
        [ProtoMember(1)]
        public Guid OperationId { get; set; }
        [ProtoMember(2)]
        public string BlockchainType { get; set; }
        [ProtoMember(3)]
        public string BlockchainDepositWalletAddress { get; set; }
        /// <summary>
        /// Lykke asset ID
        /// </summary>
        [ProtoMember(4)]
        public string AssetId { get; set; }
    }
}
