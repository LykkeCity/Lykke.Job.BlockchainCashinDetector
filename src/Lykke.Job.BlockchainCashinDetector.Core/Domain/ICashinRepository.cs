using System;
using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public interface ICashinRepository
    {
        Task<CashinAggregate> GetOrAddAsync(string blockchainType, string depositWalletAddress, string blockchainAssetId, Func<CashinAggregate> newAggregateFactory);
        Task<CashinAggregate> GetAsync(Guid operationId);
        Task<CashinAggregate> TryGetAsync(Guid operationId);
        Task SaveAsync(CashinAggregate aggregate);
    }
}
