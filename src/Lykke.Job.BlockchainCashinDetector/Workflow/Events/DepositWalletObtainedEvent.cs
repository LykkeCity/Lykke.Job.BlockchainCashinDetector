using MessagePack;
using System;
using Lykke.Service.BlockchainWallets.Contract;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Events
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class DepositWalletObtainedEvent
    {
        public string BlockchainType { get; set; }
        public string DepositWalletAddress { get; set; }
        public Guid ClientId { get; set; }
        public CreatorType CreatedBy { get; set; }
    }
}
