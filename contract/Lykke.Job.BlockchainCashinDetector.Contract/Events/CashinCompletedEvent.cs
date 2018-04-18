using System;
using System.Collections.Generic;
using System.Text;
using MessagePack;

namespace Lykke.Job.BlockchainCashinDetector.Contract.Events
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class CashinCompletedEvent
    {
        public string AssetId { get; set; }

        public decimal Amount { get; set; }

        public Guid ClientId { get; set; }
    }
}
