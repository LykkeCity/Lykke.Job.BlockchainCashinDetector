using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public interface IEnrolledBalanceRepository
    {
        Task<IEnumerable<EnrolledBalance>> GetAsync(IEnumerable<DepositWalletKey> keys);

        Task SetBalanceAsync(string blockchainType, string blockchainAssetId, string depositWalletAddress, decimal balance, long balanceBlock);

        Task ResetBalanceAsync(string blockchainType, string blockchainAssetId, string depositWalletAddress, long transactionBlock);

        Task<EnrolledBalance> TryGetAsync(DepositWalletKey key);
    }
}
