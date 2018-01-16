using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Contract;
using Lykke.Job.BlockchainCashinDetector.Core;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Lykke.Job.BlockchainCashinDetector.Workflow.Events;
using Lykke.Job.BlockchainOperationsExecutor.Contract;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Sagas
{
    /// <summary>
    /// -> DepositWalletsBalanceProcessingPeriodicalHandler : DetectDepositBalanceCommand
    /// -> DepositBalanceDetectedEvent
    ///     -> StartCashinCommand
    /// -> CashinStartedEvent
    ///     -> EnrollToMatchingEngineCommand
    /// -> CashinEnrolledToMatchingEngineEvent 
    ///     -> BlockchainOperationsExecutor : StartOperationCommand
    /// -> BlockchainOperationsExecutor : OperationCompleted | OperationFailed
    /// </summary>
    [UsedImplicitly]
    public class CashinSaga
    {
        private static readonly string Self = BlockchainCashinDetectorBoundedContext.Name;

        private readonly ICashinRepository _cashinRepository;

        public CashinSaga(ICashinRepository cashinRepository)
        {
            _cashinRepository = cashinRepository;
        }

        [UsedImplicitly]
        private async Task Handle(DepositBalanceDetectedEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashinRepository.GetOrAddAsync(
                evt.BlockchainType,
                evt.DepositWalletAddress,
                evt.BlockchainAssetId,
                () => CashinAggregate.StartNew(
                    evt.BlockchainType,
                    evt.HotWalletAddress,
                    evt.DepositWalletAddress,
                    evt.BlockchainAssetId,
                    evt.Amount));

            ChaosKitty.Meow();

            if (aggregate.State == CashinState.Starting)
            {
                sender.SendCommand(new StartCashinCommand
                    {
                        OperationId = aggregate.OperationId
                    },
                    Self);
            }
        }

        [UsedImplicitly]
        private async Task Handle(CashinStartedEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashinRepository.GetAsync(evt.OperationId);

            if (aggregate.Start())
            {
                sender.SendCommand(new EnrollToMatchingEngineCommand
                    {
                        OperationId = aggregate.OperationId,
                        BlockchainType = aggregate.BlockchainType,
                        DepositWalletAddress = aggregate.DepositWalletAddress,
                        BlockchainAssetId = aggregate.BlockchainAssetId,
                        Amount = aggregate.Amount
                    },
                    Self);

                ChaosKitty.Meow();

                await _cashinRepository.SaveAsync(aggregate);

                ChaosKitty.Meow();
            }
        }

        [UsedImplicitly]
        private async Task Handle(CashinEnrolledToMatchingEngineEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashinRepository.GetAsync(evt.OperationId);

            if (aggregate.OnEnrolledToMatchingEngine(evt.ClientId, evt.AssetId))
            {
                sender.SendCommand(new BlockchainOperationsExecutor.Contract.Commands.StartOperationCommand
                    {
                        OperationId = aggregate.OperationId,
                        FromAddress = aggregate.DepositWalletAddress,
                        ToAddress = aggregate.HotWalletAddress,
                        AssetId = aggregate.AssetId,
                        Amount = aggregate.Amount,
                        IncludeFee = true
                    },
                    BlockchainOperationsExecutorBoundedContext.Name);

                ChaosKitty.Meow();

                await _cashinRepository.SaveAsync(aggregate);

                ChaosKitty.Meow();
            }
        }

        [UsedImplicitly]
        private async Task Handle(BlockchainOperationsExecutor.Contract.Events.OperationCompletedEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashinRepository.TryGetAsync(evt.OperationId);

            if (aggregate == null)
            {
                // This is not a cashin operation
                return;
            }

            if (aggregate.OnOperationComplete(evt.TransactionHash, evt.TransactionTimestamp, evt.TransactionAmount, evt.Fee))
            {
                await _cashinRepository.SaveAsync(aggregate);

                ChaosKitty.Meow();
            }
        }

        [UsedImplicitly]
        private async Task Handle(BlockchainOperationsExecutor.Contract.Events.OperationFailedEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashinRepository.TryGetAsync(evt.OperationId);

            if (aggregate == null)
            {
                // This is not a cashin operation
                return;
            }

            if (aggregate.OnOperationFailed(evt.TransactionTimestamp, evt.Error))
            {
                await _cashinRepository.SaveAsync(aggregate);

                ChaosKitty.Meow();
            }
        }
    }
}
