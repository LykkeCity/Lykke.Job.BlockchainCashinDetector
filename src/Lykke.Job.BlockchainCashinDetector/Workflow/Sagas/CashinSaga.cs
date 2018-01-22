using System;
using System.Threading.Tasks;
using Common.Log;
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
    ///     -> RemoveMatchingEngineDeduplicationLockCommand
    /// -> MatchingEngineDeduplicationLockRemovedEvent
    /// </summary>
    [UsedImplicitly]
    public class CashinSaga
    {
        private static readonly string Self = BlockchainCashinDetectorBoundedContext.Name;

        private readonly ILog _log;
        private readonly ICashinRepository _cashinRepository;

        public CashinSaga(ILog log, ICashinRepository cashinRepository)
        {
            _log = log.CreateComponentScope(nameof(CashinSaga));
            _cashinRepository = cashinRepository;
        }

        [UsedImplicitly]
        private async Task Handle(DepositBalanceDetectedEvent evt, ICommandSender sender)
        {
            try
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
            catch (Exception ex)
            {
                _log.WriteError(nameof(DepositBalanceDetectedEvent), evt, ex);
                throw;
            }
        }

        [UsedImplicitly]
        private async Task Handle(CashinStartedEvent evt, ICommandSender sender)
        {
            try
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
            catch (Exception ex)
            {
                _log.WriteError(nameof(DepositBalanceDetectedEvent), evt, ex);
                throw;
            }
        }

        [UsedImplicitly]
        private async Task Handle(CashinEnrolledToMatchingEngineEvent evt, ICommandSender sender)
        {
            try
            {
                var aggregate = await _cashinRepository.GetAsync(evt.OperationId);

                if (aggregate.OnEnrolledToMatchingEngine(evt.ClientId, evt.AssetId))
                {
                    sender.SendCommand(new BlockchainOperationsExecutor.Contract.Commands.StartOperationExecutionCommand
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
            catch (Exception ex)
            {
                _log.WriteError(nameof(DepositBalanceDetectedEvent), evt, ex);
                throw;
            }
        }

        [UsedImplicitly]
        private async Task Handle(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent evt, ICommandSender sender)
        {
            try
            {
                var aggregate = await _cashinRepository.TryGetAsync(evt.OperationId);

                // TODO: Add tag (cashin/cashiout) to the operation, and pass it to the operations executor?

                if (aggregate == null)
                {
                    // This is not a cashin operation
                    return;
                }

                if (aggregate.OnOperationComplete(evt.TransactionHash, evt.TransactionAmount, evt.Fee))
                {
                    sender.SendCommand(new RemoveMatchingEngineDeduplicationLockCommand
                        {
                            OperationId = aggregate.OperationId
                        },
                        Self);

                    await _cashinRepository.SaveAsync(aggregate);

                    ChaosKitty.Meow();
                }
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(DepositBalanceDetectedEvent), evt, ex);
                throw;
            }
        }

        [UsedImplicitly]
        private async Task Handle(BlockchainOperationsExecutor.Contract.Events.OperationExecutionFailedEvent evt, ICommandSender sender)
        {
            try
            {
                var aggregate = await _cashinRepository.TryGetAsync(evt.OperationId);

                // TODO: Add tag (cashin/cashout) to the operation, and pass it to the operations executor?

                if (aggregate == null)
                {
                    // This is not a cashin operation
                    return;
                }

                if (aggregate.OnOperationFailed(evt.Error))
                {
                    sender.SendCommand(new RemoveMatchingEngineDeduplicationLockCommand
                        {
                            OperationId = aggregate.OperationId
                        },
                        Self);

                    await _cashinRepository.SaveAsync(aggregate);

                    ChaosKitty.Meow();
                }
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(DepositBalanceDetectedEvent), evt, ex);
                throw;
            }
        }

        [UsedImplicitly]
        private async Task Handle(MatchingEngineDeduplicationLockRemovedEvent evt, ICommandSender sender)
        {
            try
            {
                var aggregate = await _cashinRepository.GetAsync(evt.OperationId);

                if (aggregate.OnMatchingEngineDeduplicationLockRemoved())
                {
                    await _cashinRepository.SaveAsync(aggregate);

                    ChaosKitty.Meow();
                }
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(DepositBalanceDetectedEvent), evt, ex);
                throw;
            }
        }
    }
}
