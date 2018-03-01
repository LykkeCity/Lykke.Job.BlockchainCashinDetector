using System;
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
        private readonly ILog _log;
        private readonly IMatchingEngineCallsDeduplicationRepository _deduplicationRepository;
        private readonly ICashinRepository _cashinRepository;
        private readonly IChaosKitty _chaosKitty;

        public MatchingEngineCallDeduplicationsProjection(
            ILog log,
            IMatchingEngineCallsDeduplicationRepository deduplicationRepository,
            ICashinRepository cashinRepository,
            IChaosKitty chaosKitty)
        {
            _log = log.CreateComponentScope(nameof(MatchingEngineCallDeduplicationsProjection));
            _deduplicationRepository = deduplicationRepository;
            _chaosKitty = chaosKitty;
            _cashinRepository = cashinRepository;
        }

        [UsedImplicitly]
        public async Task Handle(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent evt)
        {
            _log.WriteInfo(nameof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent), evt, "");

            try
            {
                await _deduplicationRepository.TryRemoveAsync(evt.OperationId);

                _chaosKitty.Meow(evt.OperationId);
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent), evt, ex);
                throw;
            }
        }

        [UsedImplicitly]
        public async Task Handle(BlockchainOperationsExecutor.Contract.Events.OperationExecutionFailedEvent evt)
        {
            _log.WriteInfo(nameof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionFailedEvent), evt, "");

            try
            {
                await _deduplicationRepository.TryRemoveAsync(evt.OperationId);

                _chaosKitty.Meow(evt.OperationId);
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionFailedEvent), evt, ex);
                throw;
            }
        }

        [UsedImplicitly]
        public async Task Handle(EnrolledBalanceIncreasedEvent evt)
        {
            _log.WriteInfo(nameof(EnrolledBalanceIncreasedEvent), evt, "");

            try
            {
                var aggregate = await _cashinRepository.GetAsync(evt.OperationId);

                if (aggregate.State == CashinState.OperationIsFinished)
                {
                    await _deduplicationRepository.TryRemoveAsync(evt.OperationId);
                }
                
                _chaosKitty.Meow(evt.OperationId);
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(EnrolledBalanceIncreasedEvent), evt, ex);
                throw;
            }
        }
    }
}
