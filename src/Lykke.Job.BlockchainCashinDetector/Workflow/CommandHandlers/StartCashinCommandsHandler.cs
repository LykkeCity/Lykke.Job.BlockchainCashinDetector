using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Lykke.Job.BlockchainCashinDetector.Workflow.Events;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.CommandHandlers
{
    [UsedImplicitly]
    public class StartCashinCommandsHandler
    {
        private readonly ILog _log;

        public StartCashinCommandsHandler(ILog log)
        {
            _log = log;
        }

        [UsedImplicitly]
        public Task<CommandHandlingResult> Handle(StartCashinCommand command, IEventPublisher publisher)
        {

            _log.WriteInfo(nameof(StartCashinCommand), command, "");

            // This command handler shouldn't contain any dependencies to make saga switching to the Stated
            // state fast and reliable as posible

            publisher.PublishEvent(new CashinStartedEvent
            {
                OperationId = command.OperationId
            });

            return Task.FromResult(CommandHandlingResult.Ok());
        }
    }
}
