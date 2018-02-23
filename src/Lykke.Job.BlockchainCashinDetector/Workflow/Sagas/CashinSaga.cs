using System;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Contract;
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
    /// -> BlockchainOperationsExecutor : OperationCompleted          | -> BlockchainOperationsExecutor : OperationFailed
    ///     -> UpdateDepositBalanceDetectionsDeduplicationLockCommand |     -> RemoveMatchingEngineDeduplicationLockCommand
    /// -> DepositBalanceDetectionsDeduplicationLockUpdatedEvent      |
    ///     -> RemoveMatchingEngineDeduplicationLockCommand           |
    /// -> MatchingEngineDeduplicationLockRemovedEvent
    ///     -> RegisterClientOperationFinishCommand
    /// -> ClientOperationFinishRegisteredEvent
    /// </summary>
    [UsedImplicitly]
    public class CashinSaga
    {
        private static readonly string Self = BlockchainCashinDetectorBoundedContext.Name;

        private readonly ILog _log;
        private readonly IChaosKitty _chaosKitty;
        private readonly ICashinRepository _cashinRepository;

        public CashinSaga(
            IChaosKitty chaosKitty,
            ILog log, 
            ICashinRepository cashinRepository)
        {
            _log = log.CreateComponentScope(nameof(CashinSaga));
            _chaosKitty = chaosKitty;
            _cashinRepository = cashinRepository;
        }

        [UsedImplicitly]
        private async Task Handle(DepositBalanceDetectedEvent evt, ICommandSender sender)
        {
            _log.WriteInfo(nameof(DepositBalanceDetectedEvent), evt, "");

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
                        evt.Amount,
                        evt.AssetId));

                _chaosKitty.Meow(aggregate.OperationId);

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
            _log.WriteInfo(nameof(CashinStartedEvent), evt, "");

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
                            Amount = aggregate.Amount,
                            AssetId = aggregate.AssetId
                        },
                        Self);

                    _chaosKitty.Meow(evt.OperationId);

                    await _cashinRepository.SaveAsync(aggregate);
                }
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(CashinStartedEvent), evt, ex);
                throw;
            }
        }

        [UsedImplicitly]
        private async Task Handle(CashinEnrolledToMatchingEngineEvent evt, ICommandSender sender)
        {
            _log.WriteInfo(nameof(CashinEnrolledToMatchingEngineEvent), evt, "");

            try
            {
                var aggregate = await _cashinRepository.GetAsync(evt.OperationId);

                if (aggregate.OnEnrolledToMatchingEngine(evt.ClientId))
                {
                    // TODO: Add tag (cashin/cashout) to the operation, and pass it to the operations executor?

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

                    _chaosKitty.Meow(evt.OperationId);

                    await _cashinRepository.SaveAsync(aggregate);
                }
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(CashinEnrolledToMatchingEngineEvent), evt, ex);
                throw;
            }
        }

        [Obsolete("Should be removed with next release")]
        [UsedImplicitly]
        private async Task Handle(ClientOperationStartRegisteredEvent evt, ICommandSender sender)
        {
            _log.WriteInfo(nameof(ClientOperationStartRegisteredEvent), evt, "");

            try
            {
                var aggregate = await _cashinRepository.GetAsync(evt.OperationId);

                if (aggregate.OnClientOperationStartRegistered())
                {
                    // TODO: Add tag (cashin/cashout) to the operation, and pass it to the operations executor?

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

                    _chaosKitty.Meow(evt.OperationId);

                    await _cashinRepository.SaveAsync(aggregate);
                }
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(ClientOperationStartRegisteredEvent), evt, ex);
                throw;
            }
        }

        [UsedImplicitly]
        private async Task Handle(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent evt, ICommandSender sender)
        {
            _log.WriteInfo(nameof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent), evt, "");

            try
            {
                var aggregate = await _cashinRepository.TryGetAsync(evt.OperationId);

                if (aggregate == null)
                {
                    // This is not a cashin operation
                    return;
                }

                if (aggregate.OnOperationCompleted(evt.TransactionHash, evt.Block, evt.TransactionAmount, evt.Fee))
                {
                    sender.SendCommand(new UpdateDepositBalanceDetectionsDeduplicationLockCommand
                        {
                            Block = evt.Block,
                            BlockchainAssetId = aggregate.BlockchainAssetId,
                            BlockchainType = aggregate.BlockchainType,
                            DepositWalletAddress = aggregate.DepositWalletAddress,
                            OperationId = aggregate.OperationId
                        },
                        Self);

                    _chaosKitty.Meow(evt.OperationId);

                    await _cashinRepository.SaveAsync(aggregate);
                }
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent), evt, ex);
                throw;
            }
        }
        
        [UsedImplicitly]
        private async Task Handle(BlockchainOperationsExecutor.Contract.Events.OperationExecutionFailedEvent evt, ICommandSender sender)
        {
            _log.WriteInfo(nameof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionFailedEvent), evt, "");

            try
            {
                var aggregate = await _cashinRepository.TryGetAsync(evt.OperationId);

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

                    _chaosKitty.Meow(evt.OperationId);

                    await _cashinRepository.SaveAsync(aggregate);
                }
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionFailedEvent), evt, ex);
                throw;
            }
        }

        [Obsolete("Should be removed with next release")]
        [UsedImplicitly]
        private async Task Handle(DepositBalanceDetectionsDeduplicationLockUpdatedEvent evt, ICommandSender sender)
        {
            _log.WriteInfo(nameof(DepositBalanceDetectionsDeduplicationLockUpdatedEvent), evt, "");

            try
            {
                var aggregate = await _cashinRepository.GetAsync(evt.OperationId);

                if (aggregate.OnDepositBalanceDetectionsDeduplicationLockUpdated())
                {
                    sender.SendCommand(new RemoveMatchingEngineDeduplicationLockCommand
                        {
                            OperationId = aggregate.OperationId
                        },
                        Self);

                    _chaosKitty.Meow(evt.OperationId);

                    await _cashinRepository.SaveAsync(aggregate);
                }
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(DepositBalanceDetectionsDeduplicationLockUpdatedEvent), evt, ex);
                throw;
            }
        }

        [UsedImplicitly]
        private async Task Handle(MatchingEngineDeduplicationLockRemovedEvent evt, ICommandSender sender)
        {
            _log.WriteInfo(nameof(MatchingEngineDeduplicationLockRemovedEvent), evt, "");

            try
            {
                var aggregate = await _cashinRepository.GetAsync(evt.OperationId);

                if (aggregate.OnMatchingEngineDeduplicationLockRemoved())
                {
                    if (!aggregate.ClientId.HasValue)
                    {
                        throw new InvalidOperationException("Client id should be not null");
                    }

                    sender.SendCommand(new RegisterClientOperationFinishCommand
                    {
                        OperationId = aggregate.OperationId,
                        ClientId = aggregate.ClientId.Value,
                        TransactionHash = aggregate.TransactionHash
                    }, Self);

                    _chaosKitty.Meow(evt.OperationId);

                    await _cashinRepository.SaveAsync(aggregate);
                }
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(MatchingEngineDeduplicationLockRemovedEvent), evt, ex);
                throw;
            }
        }

        [Obsolete("Should be removed with next release")]
        [UsedImplicitly]
        private Task Handle(ClientOperationFinishRegisteredEvent evt, ICommandSender sender)
        {
            return Task.CompletedTask;
        }
    }
}
