using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Modules;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Job.BlockchainCashinDetector.Controllers
{
    [Route("api/maintenance")]
    public class MaintenanceController : Controller
    {
        private readonly ICqrsEngine _cqrsEngine;

        public MaintenanceController(ICqrsEngine cqrsEngine)
        {
            _cqrsEngine = cqrsEngine;
        }

        [HttpPost("commands/EnrollToMatchingEngineCommand")]
        public async void SendWaitForTransactionEndingCommand([FromBody] EnrollToMatchingEngineCommand command)
        {
            _cqrsEngine.SendCommand(command, $"{CqrsModule.Self}.saga", CqrsModule.Self);
        }
    }
}
