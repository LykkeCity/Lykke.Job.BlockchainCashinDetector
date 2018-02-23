using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public interface IDepositBalanceDetectionsDeduplicationRepository
    {
        Task<IEnumerable<DepositBalanceDetectionsDeduplicationLock>> GetAsync(IEnumerable<DepositWalletKey> keys);

        Task InсreaseBlockNumberAsync(string blockchainType, string blockchainAssetId, string depositWalletAddress, long block);
    }
}
