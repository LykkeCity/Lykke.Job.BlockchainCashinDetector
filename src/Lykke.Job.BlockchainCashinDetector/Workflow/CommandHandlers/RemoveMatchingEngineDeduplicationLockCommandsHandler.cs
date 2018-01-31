using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Lykke.Job.BlockchainCashinDetector.Workflow.Events;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.CommandHandlers
{
    [UsedImplicitly]
    public class RemoveMatchingEngineDeduplicationLockCommandsHandler
    {
        private readonly IChaosKitty _chaosKitty;
        private readonly ILog _log;
        private readonly IMatchingEngineCallsDeduplicationRepository _deduplicationRepository;

        public RemoveMatchingEngineDeduplicationLockCommandsHandler(
            IChaosKitty chaosKitty,
            ILog log,
            IMatchingEngineCallsDeduplicationRepository deduplicationRepository)
        {
            _chaosKitty = chaosKitty;
            _log = log;
            _deduplicationRepository = deduplicationRepository;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(RemoveMatchingEngineDeduplicationLockCommand command, IEventPublisher publisher)
        {

            _log.WriteInfo(nameof(RemoveMatchingEngineDeduplicationLockCommand), command, "");

            await _deduplicationRepository.TryRemoveAsync(command.OperationId);

            _chaosKitty.Meow(command.OperationId);
            
            publisher.PublishEvent(new MatchingEngineDeduplicationLockRemovedEvent
            {
                OperationId = command.OperationId
            });

            return CommandHandlingResult.Ok();
        }
    }
}
