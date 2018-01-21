using ProtoBuf;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Commands
{
    /// <summary>
    /// Command to detect active positive balance on the deposit wallet
    /// </summary>
    [ProtoContract]
    public class DetectDepositBalanceCommand
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
