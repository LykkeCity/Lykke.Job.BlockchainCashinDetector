using System;

namespace Lykke.Job.BlockchainCashinDetector.Core.Domain.Cashin.Commands
{
    /// <summary>
    /// Command to end cashin process
    /// </summary>
    public class EndCashinCommand
    {
        public Guid OperationId { get; set; }
        public string BlockchainType { get; set; }
        public string BlockchainDepositWalletAddress { get; set; }
        /// <summary>
        /// Lykke asset ID
        /// </summary>
        public string AssetId { get; set; }
    }
}
