using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public interface IEnrolledBalanceRepository
    {
        Task<IEnumerable<EnrolledBalance>> GetAsync(IEnumerable<DepositWalletKey> keys);

        Task SetBalanceAsync(DepositWalletKey key, decimal balance, long balanceBlock);

        Task ResetBalanceAsync(DepositWalletKey key, long transactionBlock);

        Task<EnrolledBalance> TryGetAsync(DepositWalletKey key);
    }
}
