using System;
using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashinDetector.Core.Domain.Cashin
{
    public interface IOperationsDeduplicationRepository
    {
        Task InsertOrReplaceAsync(Guid operationId);
        Task<bool> IsExists(Guid operationId);
    }
}
