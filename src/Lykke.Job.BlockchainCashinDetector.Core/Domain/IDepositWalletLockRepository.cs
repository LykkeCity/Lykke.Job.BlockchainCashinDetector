using System;
using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public interface IDepositWalletLockRepository
    {
        Task<Guid> LockAsync(string blockchainType, string depositWalletAddress, string blockchainAssetId, Func<Guid> operationIdFactory);
        Task ReleaseAsync(string blockchainType, string depositWalletAddress, string blockchainAssetId, Guid operationId);
    }
}
