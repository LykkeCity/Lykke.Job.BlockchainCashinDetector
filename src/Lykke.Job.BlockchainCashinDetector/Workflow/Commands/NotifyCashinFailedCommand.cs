using System;
using System.Collections.Generic;
using System.Text;
using Lykke.Job.BlockchainCashinDetector.Contract;
using MessagePack;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Commands
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class NotifyCashinFailedCommand
    {
        public Guid? ClientId { get; set; }
        public decimal? Amount { get; set; }
        public string AssetId { get; set; }
        public Guid OperationId { get; set; }
        public string Error { get; set; }
        public CashinErrorCode ErrorCode { get; set; }
    }
}
