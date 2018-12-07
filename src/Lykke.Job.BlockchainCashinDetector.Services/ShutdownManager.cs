using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Sdk;

namespace Lykke.Job.BlockchainCashinDetector.Services
{
    // NOTE: Sometimes, shutdown process should be expressed explicitly. 
    // If this is your case, use this class to manage shutdown.
    // For example, sometimes some state should be saved only after all incoming message processing and 
    // all periodical handler was stopped, and so on.
    [UsedImplicitly]
    public class ShutdownManager : IShutdownManager
    {
        public async Task StopAsync()
        {
            await Task.CompletedTask;
        }
    }
}
