using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public interface IEnrolledBalanceRepository
    {
        Task<IEnumerable<EnrolledBalance>> GetAsync(IEnumerable<DepositWalletKey> keys);

        Task SetBalanceAsync(string blockchainType, string blockchainAssetId, string depositWalletAddress, decimal amount, long block);

        Task ResetBalanceAsync(string blockchainType, string blockchainAssetId, string depositWalletAddress, long block);

        Task<EnrolledBalance> TryGetAsync(DepositWalletKey key);
    }
}
