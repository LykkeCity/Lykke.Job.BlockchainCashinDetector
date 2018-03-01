namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public sealed class EnrolledBalance
    {
        public EnrolledBalance(
            decimal balance,
            string blockchainType,
            string blockchainAssetId,
            string depositWalletAddress,
            long block)
        {
            Balance = balance;
            BlockchainType = blockchainType;
            BlockchainAssetId = blockchainAssetId;
            DepositWalletAddress = depositWalletAddress;
            Block = block;
        }

        public decimal Balance { get; }

        public string BlockchainType { get; }

        public string BlockchainAssetId { get; }

        public string DepositWalletAddress { get; }

        public long Block { get; }
    }
}
