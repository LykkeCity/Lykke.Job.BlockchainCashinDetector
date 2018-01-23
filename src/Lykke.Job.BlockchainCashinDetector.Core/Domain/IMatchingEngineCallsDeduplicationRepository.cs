using System;
using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public interface IMatchingEngineCallsDeduplicationRepository
    {
        Task InsertOrReplaceAsync(Guid operationId);
        Task<bool> IsExistsAsync(Guid operationId);
        Task TryRemoveAsync(Guid operationId);
    }
}
