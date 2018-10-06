using System;
using System.Collections.Generic;
using System.Text;
using MessagePack;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Commands
{
    /// <summary>
    /// Command to ...
    /// </summary>
    [MessagePackObject]
    public class ObtainDepositWalletCommand
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
        public double MatchingEngineOperationAmount { get; set; }
    }
}
