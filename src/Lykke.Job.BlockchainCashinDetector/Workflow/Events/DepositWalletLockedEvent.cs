using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Events
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class DepositWalletLockedEvent
    {
        public Guid OperationId { get; set; }
        public string BlockchainType { get; set; }
        public string BlockchainAssetId { get; set; }
        public string DepositWalletAddress {get; set; }
        public decimal LockedAtBalance { get; set; }
        public long LockedAtBlock { get; set; }
        public decimal EnrolledBalance { get; set; }
        public long EnrolledBlock { get; set; }
        public string AssetId { get; set; }
        public int AssetAccuracy { get; set; }
        public int BlockchainAssetAccuracy { get; set; }
        public decimal CashinMinimalAmount { get; set; }
        public string HotWalletAddress { get; set; }
    }
}
