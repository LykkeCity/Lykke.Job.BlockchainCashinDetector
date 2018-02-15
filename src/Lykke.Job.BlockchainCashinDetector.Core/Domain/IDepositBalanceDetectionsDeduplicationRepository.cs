using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public interface IDepositBalanceDetectionsDeduplicationRepository
    {
        Task InsertOrReplaceAsync(string blockchainType, string blockchainAssetId, string depositWalletAddress, long block);
        Task<long?> TryGetAsync(string blockchainType, string blockchainAssetId, string depositWalletAddress);
    }
}
