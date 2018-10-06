using MessagePack;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Commands
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class ValidateLykkePayCashinCommand
    {
        public string IntegrationLayerId { get; set; }
        public string DepositWalletAddress { get; set; }
        public decimal TransferAmount { get; set; }
    }
}
