using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public interface IDepositBalanceDetectionsDeduplicationRepository
    {
        Task<IEnumerable<IDepositBalanceDetectionsDeduplicationLock>> GetAsync(IEnumerable<IDepositWalletKey> keys);

        Task InsertOrReplaceAsync(string blockchainType, string blockchainAssetId, string depositWalletAddress, long block);
    }
}
