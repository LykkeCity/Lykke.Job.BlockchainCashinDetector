using System;
using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public interface IMatchingEngineCallsDeduplicationRepository
    {
        Task InsertOrReplaceAsync(Guid operationId);
        Task<bool> IsExists(Guid operationId);
    }
}
