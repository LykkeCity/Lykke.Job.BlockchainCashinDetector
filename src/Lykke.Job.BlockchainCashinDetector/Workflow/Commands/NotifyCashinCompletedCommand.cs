using System;
using Lykke.Job.BlockchainCashinDetector.Contract.Events;
using MessagePack;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Commands
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class NotifyCashinCompletedCommand
    {
        public Guid ClientId { get; set; }
        public decimal OperationAmount { get; set; }
        public double MeOperationAmount { get; set; }
        public decimal Fee { get; set; }
        public string AssetId { get; set; }
        public CashinOperationType OperationType { get; set; }
        public Guid OperationId { get; set; }
        public string TransactionHash { get; set; }
    }
}
