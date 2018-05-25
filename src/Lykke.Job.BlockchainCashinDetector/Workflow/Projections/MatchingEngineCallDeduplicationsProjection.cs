using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.Job.BlockchainCashinDetector.Workflow.Events;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Projections
{
    public class MatchingEngineCallDeduplicationsProjection
    {
        private readonly IMatchingEngineCallsDeduplicationRepository _deduplicationRepository;
        private readonly ICashinRepository _cashinRepository;
        private readonly IChaosKitty _chaosKitty;

        public MatchingEngineCallDeduplicationsProjection(
            IMatchingEngineCallsDeduplicationRepository deduplicationRepository,
            ICashinRepository cashinRepository,
            IChaosKitty chaosKitty)
        {
            _deduplicationRepository = deduplicationRepository;
            _chaosKitty = chaosKitty;
            _cashinRepository = cashinRepository;
        }

        [UsedImplicitly]
        public async Task Handle(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent evt)
        {
            await _deduplicationRepository.TryRemoveAsync(evt.OperationId);

            _chaosKitty.Meow(evt.OperationId);
        }

        [UsedImplicitly]
        public async Task Handle(BlockchainOperationsExecutor.Contract.Events.OperationExecutionFailedEvent evt)
        {
            await _deduplicationRepository.TryRemoveAsync(evt.OperationId);

            _chaosKitty.Meow(evt.OperationId);
        }

        [UsedImplicitly]
        public async Task Handle(EnrolledBalanceSetEvent evt)
        {
            var aggregate = await _cashinRepository.GetAsync(evt.OperationId);

            if (aggregate.IsDustCashin)
            {
                await _deduplicationRepository.TryRemoveAsync(evt.OperationId);
            }
                
            _chaosKitty.Meow(evt.OperationId);
        }
    }
}
