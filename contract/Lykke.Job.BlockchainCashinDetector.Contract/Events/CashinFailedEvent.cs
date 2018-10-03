using System;
using System.Collections.Generic;
using System.Text;
using MessagePack;

namespace Lykke.Job.BlockchainCashinDetector.Contract.Events
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class CashinFailedEvent
    {
        /// <summary>
        ///  Lykke unique asset ID
        /// </summary>
        public string AssetId { get; set; }

        /// <summary>
        /// Amount
        /// </summary>
        public decimal? Amount { get; set; }

        /// <summary>
        /// Lykke unique client ID
        /// </summary>
        public Guid? ClientId { get; set; }

        /// <summary>
        /// Lykke unique operation ID
        /// </summary>
        public Guid OperationId { get; set; }

        /// <summary>
        /// Error description
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Cashin error code
        /// </summary>
        public CashinErrorCode ErrorCode { get; set; }
    }
}
