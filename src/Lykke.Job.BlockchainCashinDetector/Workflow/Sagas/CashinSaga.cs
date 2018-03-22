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

using StartTransactionCommand = Lykke.Job.BlockchainOperationsExecutor.Contract.Commands.StartOperationExecutionCommand;
using TransactionCompletedEvent = Lykke.Job.BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent;
using TransactionFailedEvent = Lykke.Job.BlockchainOperationsExecutor.Contract.Events.OperationExecutionFailedEvent;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Sagas
{
    /// <summary>
    /// -> DepositWalletsBalanceProcessingPeriodicalHandler : DetectDepositBalanceCommand
    /// -> DepositBalanceDetectedEvent
    ///     -> StartCashinCommand
    /// -> CashinStartedEvent
    ///     -> EnrollToMatchingEngineCommand
    /// -> CashinEnrolledToMatchingEngineEvent
    ///     -> SetEnrolledBalanceCommand
    /// -> EnrolledBalanceSetEvent
    ///     -> StartTransactionCommand
    /// -> TransactionCompletedEvent                        || -> TransactionFailedEvent
    ///     -> ResetEnrolledBalanceCommand                  ||     -> RemoveMatchingEngineDeduplicationLockCommand
    /// -> EnrolledBalanceResetEvent                        ||
    ///     -> RemoveMatchingEngineDeduplicationLockCommand ||
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
                var aggregate = await _cashinRepository.GetOrAddAsync
                (
                    evt.BlockchainType,
                    evt.DepositWalletAddress,
                    evt.BlockchainAssetId,
                    () => CashinAggregate.StartNew
                    (
                        assetId: evt.AssetId,
                        balanceAmount: evt.BalanceAmount,
                        balanceBlock: evt.BalanceBlock,
                        blockchainAssetId: evt.BlockchainAssetId,
                        blockchainType: evt.BlockchainType,
                        cashinMinimalAmount: evt.CashinMinimalAmount,
                        depositWalletAddress: evt.DepositWalletAddress,
                        hotWalletAddress: evt.HotWalletAddress
                    )
                );

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
                    sender.SendCommand
                    (
                        new EnrollToMatchingEngineCommand
                        {
                            AssetId = aggregate.AssetId,
                            BalanceAmount = aggregate.BalanceAmount,
                            BalanceBlock = aggregate.BalanceBlock,
                            BlockchainAssetId = aggregate.BlockchainAssetId,
                            BlockchainType = aggregate.BlockchainType,
                            CashinMinimalAmount = aggregate.CashinMinimalAmount,
                            DepositWalletAddress = aggregate.DepositWalletAddress,
                            HotWalletAddress = aggregate.HotWalletAddress,
                            OperationId = aggregate.OperationId
                        },
                        Self
                    );

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
                
                if (aggregate.OnEnrolledToMatchingEngine(
                        clientId: evt.ClientId,
                        enrolledBalanceAmount: evt.EnrolledBalanceAmount,
                        operationAmount: evt.OperationAmount
                   ))
                {
                    sender.SendCommand
                    (
                        new SetEnrolledBalanceCommand
                        {
                            BalanceBlock = aggregate.BalanceBlock,
                            BlockchainAssetId = aggregate.BlockchainAssetId,
                            BlockchainType = aggregate.BlockchainType,
                            DepositWalletAddress = aggregate.DepositWalletAddress,
                            EnrolledBalanceAmount = evt.EnrolledBalanceAmount,
                            OperationAmount = evt.OperationAmount,
                            OperationId = aggregate.OperationId                            
                        },
                        Self
                    );

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

        [UsedImplicitly]
        private async Task Handle(EnrolledBalanceSetEvent evt, ICommandSender sender)
        {
            _log.WriteInfo(nameof(EnrolledBalanceSetEvent), evt, "");

            try
            {
                var aggregate = await _cashinRepository.GetAsync(evt.OperationId);

                if (aggregate.OnEnrolledBalanceSet())
                {
                    if (!aggregate.IsDustCashin)
                    {
                        sender.SendCommand
                        (
                            new StartTransactionCommand
                            {
                                Amount = aggregate.BalanceAmount,
                                AssetId = aggregate.AssetId,
                                FromAddress = aggregate.DepositWalletAddress,
                                IncludeFee = true,
                                OperationId = aggregate.OperationId,
                                ToAddress = aggregate.HotWalletAddress
                            },
                            BlockchainOperationsExecutorBoundedContext.Name
                        );
                    }

                    _chaosKitty.Meow(evt.OperationId);

                    await _cashinRepository.SaveAsync(aggregate);
                }
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(EnrolledBalanceSetEvent), evt, ex);
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

                    sender.SendCommand(new StartTransactionCommand
                        {
                            OperationId = aggregate.OperationId,
                            FromAddress = aggregate.DepositWalletAddress,
                            ToAddress = aggregate.HotWalletAddress,
                            AssetId = aggregate.AssetId,
                            Amount = aggregate.BalanceAmount,
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
        private async Task Handle(TransactionCompletedEvent evt, ICommandSender sender)
        {
            _log.WriteInfo(nameof(TransactionCompletedEvent), evt, "");

            try
            {
                var aggregate = await _cashinRepository.TryGetAsync(evt.OperationId);

                if (aggregate == null)
                {
                    // This is not a cashin operation
                    return;
                }

                if (!aggregate.ClientId.HasValue)
                    throw new ArgumentException($"Operation {evt.OperationId} has no client associated with it. Fix it!");

                if (aggregate.OnTransactionCompleted(evt.TransactionHash, evt.Block, evt.TransactionAmount, evt.Fee))
                {
                    sender.SendCommand(new ResetEnrolledBalanceCommand
                    {
                        OperationId = aggregate.OperationId,
                        BlockchainType = aggregate.BlockchainType,
                        BlockchainAssetId = aggregate.BlockchainAssetId,
                        DepositWalletAddress = aggregate.DepositWalletAddress,
                        Block = evt.Block
                    },
                    Self);

                    _chaosKitty.Meow(evt.OperationId);

                    sender.SendCommand(new NotifyCashinCompletedCommand
                    {
                        Amount = aggregate.OperationAmount,
                        AssetId = aggregate.AssetId,
                        ClientId = aggregate.ClientId.Value
                    },
                        Self);

                    await _cashinRepository.SaveAsync(aggregate);
                }
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(TransactionCompletedEvent), evt, ex);
                throw;
            }
        }

        [UsedImplicitly]
        public async Task Handle(EnrolledBalanceResetEvent evt, ICommandSender sender)
        {
            _log.WriteInfo(nameof(EnrolledBalanceResetEvent), evt, "");

            try
            {
                var aggregate = await _cashinRepository.TryGetAsync(evt.OperationId);

                if (aggregate.OnEnrolledBalanceReset())
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
                _log.WriteError(nameof(EnrolledBalanceResetEvent), evt, ex);
                throw;
            }
        }

        [UsedImplicitly]
        private async Task Handle(TransactionFailedEvent evt, ICommandSender sender)
        {
            _log.WriteInfo(nameof(TransactionFailedEvent), evt, "");

            try
            {
                var aggregate = await _cashinRepository.TryGetAsync(evt.OperationId);

                if (aggregate == null)
                {
                    // This is not a cashin operation
                    return;
                }

                if (aggregate.OnTransactionFailed(evt.Error))
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
                _log.WriteError(nameof(TransactionFailedEvent), evt, ex);
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
    }
}
