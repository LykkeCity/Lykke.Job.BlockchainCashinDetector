using System;

namespace Lykke.Job.BlockchainCashinDetector.Core.Domain.Cashin.Commands
{
    /// <summary>
    /// Command to enroll cashin to the client ME account
    /// </summary>
    public class EnrollToMatchingEngineCommand
    {
        public Guid OperationId { get; set; }
        public string BlockchainType { get; set; }
        public string BlockchainDepositWalletAddress { get; set; }
        public string BlockchainAssetId { get; set; }
        public decimal Amount { get; set; }
    }
}
