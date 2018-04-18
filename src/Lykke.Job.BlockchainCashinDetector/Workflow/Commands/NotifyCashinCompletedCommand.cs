using System;
using System.Collections.Generic;
using System.Text;
using MessagePack;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Commands
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class NotifyCashinCompletedCommand
    {
        public Guid ClientId { get; set; }
        public decimal Amount { get; set; }
        public string AssetId { get; set; }
    }
}
