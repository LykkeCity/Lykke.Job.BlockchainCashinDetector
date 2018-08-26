namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public sealed class EnrolledBalance
    {
        public DepositWalletKey Key { get; }
        public decimal Balance { get; }
        public long Block { get; }

        private EnrolledBalance(
            DepositWalletKey key,
            decimal balance,
            long block)
        {
            Balance = balance;
            Key = key;
            Block = block;
        }

        public static EnrolledBalance Create(DepositWalletKey key, decimal balance, long block)
        {
            return new EnrolledBalance(key, balance, block);
        }
    }
}
