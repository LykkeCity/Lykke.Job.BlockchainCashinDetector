using System;
using MessagePack;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Commands
{
    /// <summary>
    /// Command to enroll cashin to the client ME account
    /// </summary>
    [MessagePackObject]
    public class EnrollToMatchingEngineCommand
    {
        [Key(0)]
        public string AssetId { get; set; }

        [Key(1)]
        public string BlockchainAssetId { get; set; }

        [Key(2)]
        public string BlockchainType { get; set; }

        [Key(3)]
        public string DepositWalletAddress { get; set; }

        [Key(4)]
        public Guid OperationId { get; set; }

        [Key(5)]
        public double MatchingEngineOperationAmount{ get; set; }

        [Key(6)]
        public Guid? ClientId { get; set; }
    }
}
