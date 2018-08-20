using System;
using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public interface IDepositWalletLockRepository
    {
        Task<DepositWalletLock> LockAsync(
            DepositWalletKey key,
            decimal balance,
            long block,
            Func<Guid> operationIdFactory);

        Task ReleaseAsync(DepositWalletKey key, Guid operationId);
    }
}
