using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashinDetector.Core.Services.LykkePay
{
    public interface IPayInternalServiceWrapper
    {
        Task<bool> ValidateDepoistTransferAsync(string integrationLayerId, string transferAddress, decimal transferAmount);
    }
}
