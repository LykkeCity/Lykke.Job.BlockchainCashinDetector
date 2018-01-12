using System;
using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashinDetector.Core.Domain.Cashin
{
    public interface IActiveCashinRepository
    {
        Task<Guid> GetOrAdd(string blockchainType, string fromAddress, string assetId, Func<Guid> generateOperationId);
        Task<bool> TryRemoveAsync(string blockchainType, string fromAddress, string assetId, Guid operationId);
    }
}
