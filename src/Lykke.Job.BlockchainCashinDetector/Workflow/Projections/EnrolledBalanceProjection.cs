using System;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
namespace Lykke.Job.BlockchainCashinDetector.Workflow.Projections
{
    public class EnrolledBalanceProjection
    {
        private readonly ILog _log;
        private readonly IChaosKitty _chaosKitty;
        private readonly ICashinRepository _cashinRepository;
        private readonly IEnrolledBalanceRepository _enrolledBalanceRepository;

        public EnrolledBalanceProjection(
            ILog log,
            ICashinRepository cashinRepository,
            IEnrolledBalanceRepository enrolledBalanceRepository,
            IChaosKitty chaosKitty)
        {
            _chaosKitty = chaosKitty;
            _cashinRepository = cashinRepository;
            _enrolledBalanceRepository = enrolledBalanceRepository;
            _log = log.CreateComponentScope(nameof(EnrolledBalanceProjection));
        }

        [UsedImplicitly]
        public async Task Handle(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent evt)
        {
            _log.WriteInfo(nameof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent), evt, "");

            try
            {
                var aggregate = await _cashinRepository.GetAsync(evt.OperationId);

                await _enrolledBalanceRepository.ResetBalanceAsync
                (
                    aggregate.BlockchainType,
                    aggregate.BlockchainAssetId,
                    aggregate.DepositWalletAddress,
                    evt.Block
                );
                
                _chaosKitty.Meow(evt.OperationId);
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent), evt, ex);
                throw;
            }
        }
    }
}
