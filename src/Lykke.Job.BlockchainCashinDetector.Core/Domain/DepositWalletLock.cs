using System;

namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public sealed class DepositWalletLock
    {
        public DepositWalletKey Key { get; }
        public Guid OperationId { get; }
        public decimal Balance { get; }
        public long Block { get; }

        private DepositWalletLock(DepositWalletKey key, Guid operationId, decimal balance, long block)
        {
            Key = key;
            OperationId = operationId;
            Balance = balance;
            Block = block;
        }

        public static DepositWalletLock Create(DepositWalletKey key, Guid operationId, decimal balance, long block)
        {
            return new DepositWalletLock(key, operationId, balance, block);
        }
    }
}