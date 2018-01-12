using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashinDetector.Core.Services
{
    public interface IShutdownManager
    {
        Task StopAsync();
    }
}