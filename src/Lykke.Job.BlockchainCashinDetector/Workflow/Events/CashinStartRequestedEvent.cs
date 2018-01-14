using ProtoBuf;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Events
{
    [ProtoContract]
    public class CashinStartRequestedEvent
    {
        [ProtoMember(1)]
        public string BlockchainType { get; set; }

        [ProtoMember(2)]
        public string DepositWalletAddress { get; set; }
        
        [ProtoMember(3)]
        public string BlockchainAssetId { get; set; }

        [ProtoMember(4)]
        public decimal Amount { get; set; }

        [ProtoMember(5)]
        public string HotWalletAddress { get; set; }
    }
}
