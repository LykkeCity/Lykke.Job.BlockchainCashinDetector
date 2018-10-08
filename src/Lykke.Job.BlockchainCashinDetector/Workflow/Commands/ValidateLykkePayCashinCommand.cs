using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Commands
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class ValidateLykkePayCashinCommand
    {
        public Guid OperationId { get; set; }
        public string IntegrationLayerId { get; set; }
        public string DepositWalletAddress { get; set; }
        public decimal TransferAmount { get; set; }
    }
}
