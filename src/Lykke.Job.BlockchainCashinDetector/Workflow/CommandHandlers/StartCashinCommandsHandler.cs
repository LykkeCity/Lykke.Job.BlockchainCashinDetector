using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Lykke.Job.BlockchainCashinDetector.Workflow.Events;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.CommandHandlers
{
    [UsedImplicitly]
    public class StartCashinCommandsHandler
    {
        [UsedImplicitly]
        public Task<CommandHandlingResult> Handle(StartCashinCommand command, IEventPublisher publisher)
        {
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
