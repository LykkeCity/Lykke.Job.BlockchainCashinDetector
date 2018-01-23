using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Core;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Lykke.Job.BlockchainCashinDetector.Workflow.Events;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.CommandHandlers
{
    [UsedImplicitly]
    public class RemoveMatchingEngineDeduplicationLockCommandsHandler
    {
        private readonly ILog _log;
        private readonly IMatchingEngineCallsDeduplicationRepository _deduplicationRepository;

        public RemoveMatchingEngineDeduplicationLockCommandsHandler(ILog log)
        {
            _log = log;
        }

        public RemoveMatchingEngineDeduplicationLockCommandsHandler(IMatchingEngineCallsDeduplicationRepository deduplicationRepository)
        {
            _deduplicationRepository = deduplicationRepository;
        }

        [UsedImplicitly]
        public Task<CommandHandlingResult> Handle(RemoveMatchingEngineDeduplicationLockCommand command, IEventPublisher publisher)
        {
#if DEBUG
            _log.WriteInfo(nameof(RemoveMatchingEngineDeduplicationLockCommand), command, "");
#endif
            _deduplicationRepository.TryRemove(command.OperationId);

            ChaosKitty.Meow(command.OperationId);
            
            publisher.PublishEvent(new MatchingEngineDeduplicationLockRemovedEvent
            {
                OperationId = command.OperationId
            });

            return Task.FromResult(CommandHandlingResult.Ok());
        }
    }
}
