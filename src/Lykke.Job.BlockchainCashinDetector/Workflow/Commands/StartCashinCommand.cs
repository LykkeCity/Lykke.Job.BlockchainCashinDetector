using System;
using ProtoBuf;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Commands
{
    /// <summary>
    /// Command to start cashin (DW -> HW)
    /// </summary>
    [ProtoContract]
    public class StartCashinCommand
    {
        [ProtoMember(1)]
        public string BlockchainType { get; set; }
        [ProtoMember(2)]
        public string DepositWalletAddress { get; set; }
        [ProtoMember(3)]
        public string BlockchainAssetId { get; set; }
        [ProtoMember(4)]
        public decimal Amount { get; set; }

    }
}
