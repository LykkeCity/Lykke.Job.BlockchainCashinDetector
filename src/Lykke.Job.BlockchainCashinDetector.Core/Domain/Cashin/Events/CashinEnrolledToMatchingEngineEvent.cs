using System;

namespace Lykke.Job.BlockchainCashinDetector.Core.Domain.Cashin.Events
{
    /// <summary>
    /// Cashin is enrolled to the ME
    /// </summary>
    public class CashinEnrolledToMatchingEngineEvent
    {
        public Guid OperationId { get; set; }
        public string BlockchainType { get; set; }
        public string BlockchainDepositWalletAddress { get; set; }
        public string BlockchainAssetId { get; set; }
        public string AssetId { get; set; }
        public string ClientId { get; set; }
        public decimal Amount { get; set; }
    }
}
