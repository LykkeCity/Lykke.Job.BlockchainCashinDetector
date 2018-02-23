namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public sealed class DepositWalletKey
    {
        public DepositWalletKey(string blockchainAssetId, string blockchainType, string depositWalletAddress)
        {
            BlockchainAssetId = blockchainAssetId;
            BlockchainType = blockchainType;
            DepositWalletAddress = depositWalletAddress;
        }

        public string BlockchainAssetId { get; }

        public string BlockchainType { get; }
        
        public string DepositWalletAddress { get; }
    }
}
