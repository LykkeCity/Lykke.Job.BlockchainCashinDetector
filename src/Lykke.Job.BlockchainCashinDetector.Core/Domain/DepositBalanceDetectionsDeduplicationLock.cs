namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public sealed class DepositBalanceDetectionsDeduplicationLock
    {
        public DepositBalanceDetectionsDeduplicationLock(
            long block,
            string blockchainType,
            string blockchainAssetId,
            string depositWalletAddress)
        {
            Block = block;
            BlockchainType = blockchainType;
            BlockchainAssetId = blockchainAssetId;
            DepositWalletAddress = depositWalletAddress;
        }


        public long Block { get; }

        public string BlockchainType { get; }

        public string BlockchainAssetId { get; }

        public string DepositWalletAddress { get; }
    }
}
