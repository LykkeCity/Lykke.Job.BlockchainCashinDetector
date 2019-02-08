using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Contract;
using Lykke.Job.BlockchainCashinDetector.Contract.Events;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.Job.BlockchainCashinDetector.Mappers;
using Lykke.Job.BlockchainCashinDetector.StateMachine;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Lykke.Job.BlockchainCashinDetector.Workflow.Events;
using Lykke.Job.BlockchainOperationsExecutor.Contract;
using Lykke.Job.BlockchainOperationsExecutor.Contract.Commands;
using Lykke.Job.BlockchainOperationsExecutor.Contract.Events;
using Lykke.Job.BlockchainRiskControl.Contract;
using Lykke.Job.BlockchainRiskControl.Contract.Commands;
using Lykke.Job.BlockchainRiskControl.Contract.Events;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Sagas
{
    [UsedImplicitly]
    public class CashinSaga
    {
        private static readonly string Self = BlockchainCashinDetectorBoundedContext.Name;

        private readonly IChaosKitty _chaosKitty;
        private readonly ICashinRepository _cashinRepository;

        public CashinSaga(
            IChaosKitty chaosKitty,
            ICashinRepository cashinRepository)
        {
            _chaosKitty = chaosKitty;
            _cashinRepository = cashinRepository;
        }

        [UsedImplicitly]
        private async Task Handle(DepositWalletLockedEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashinRepository.GetOrAddAsync
             (
                 evt.BlockchainType,
                 evt.DepositWalletAddress,
                 evt.BlockchainAssetId,
                 evt.OperationId,
                 () => CashinAggregate.StartWaitingForActualBalance
                 (
                     operationId: evt.OperationId,
                     assetId: evt.AssetId,
                     blockchainAssetAccuracy: evt.BlockchainAssetAccuracy,
                     assetAccuracy: evt.AssetAccuracy,
                     blockchainAssetId: evt.BlockchainAssetId,
                     blockchainType: evt.BlockchainType,
                     cashinMinimalAmount: evt.CashinMinimalAmount,
                     depositWalletAddress: evt.DepositWalletAddress,
                     hotWalletAddress: evt.HotWalletAddress
                 )
             );

            var transitionResult = aggregate.Start
            (
                balanceAmount: evt.LockedAtBalance,
                balanceBlock: evt.LockedAtBlock,
                enrolledBalanceAmount: evt.EnrolledBalance,
                enrolledBalanceBlock: evt.EnrolledBlock
            );

            if (transitionResult.ShouldSaveAggregate())
            {
                await _cashinRepository.SaveAsync(aggregate);
            }

            if (transitionResult.ShouldSendCommands())
            {
                switch (aggregate.State)
                {
                    case CashinState.Started when !aggregate.MeAmount.HasValue:
                        throw new InvalidOperationException("ME operation amount should be not null here");

                    case CashinState.Started:
                        sender.SendCommand
                        (
                            new RetrieveClientCommand
                            {
                                BlockchainType = aggregate.BlockchainType,
                                DepositWalletAddress = aggregate.DepositWalletAddress,
                                OperationId = aggregate.OperationId
                            },
                            Self
                        );
                        break;

                    case CashinState.OutdatedBalance:
                        sender.SendCommand
                        (
                            new ReleaseDepositWalletLockCommand
                            {
                                BlockchainAssetId = aggregate.BlockchainAssetId,
                                BlockchainType = aggregate.BlockchainType,
                                DepositWalletAddress = aggregate.DepositWalletAddress,
                                OperationId = aggregate.OperationId
                            },
                            Self
                        );
                        break;

                    default:
                        throw new InvalidOperationException($"Unexpected aggregate state {aggregate.State}");
                }

                _chaosKitty.Meow(aggregate.OperationId);
            }
        }

        [UsedImplicitly]
        private async Task Handle(ClientRetrievedEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashinRepository.TryGetAsync(evt.OperationId);

            var transitionResult = aggregate.OnClientRetrieved(clientId: evt.ClientId);

            if (transitionResult.ShouldSaveAggregate())
            {
                await _cashinRepository.SaveAsync(aggregate);
            }

            if (transitionResult.ShouldSendCommands())
            {
                sender.SendCommand
                (
                    new ValidateOperationCommand
                    {
                        OperationId = aggregate.OperationId,
                        Type = OperationType.Deposit,
                        UserId = aggregate.ClientId.Value,
                        BlockchainType = aggregate.BlockchainType,
                        BlockchainAssetId = aggregate.BlockchainAssetId,
                        FromAddress = aggregate.DepositWalletAddress,
                        ToAddress = aggregate.HotWalletAddress,
                        Amount = aggregate.BalanceAmount.Value
                    },
                    BlockchainRiskControlBoundedContext.Name
                );

                _chaosKitty.Meow(evt.OperationId);
            }
        }


        [UsedImplicitly]
        private async Task Handle(OperationAcceptedEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashinRepository.TryGetAsync(evt.OperationId);

            if (aggregate == null)
            {
                // This is not a cashin operation
                return;
            }

            var transitionResult = aggregate.OnOperationAccepted();

            if (transitionResult.ShouldSaveAggregate())
            {
                await _cashinRepository.SaveAsync(aggregate);
            }

            if (transitionResult.ShouldSendCommands())
            {
                sender.SendCommand
                (
                    new EnrollToMatchingEngineCommand
                    {
                        AssetId = aggregate.AssetId,
                        BlockchainAssetId = aggregate.BlockchainAssetId,
                        BlockchainType = aggregate.BlockchainType,
                        DepositWalletAddress = aggregate.DepositWalletAddress,
                        OperationId = aggregate.OperationId,
                        MatchingEngineOperationAmount = aggregate.MeAmount.Value,
                        ClientId = aggregate.ClientId
                    },
                    Self
                );

                _chaosKitty.Meow(evt.OperationId);
            }
        }

        [UsedImplicitly]
        private async Task Handle(OperationRejectedEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashinRepository.GetAsync(evt.OperationId);

            if (aggregate == null)
            {
                // This is not a cashin operation
                return;
            }

            var transitionResult = aggregate.OnOperationRejected(evt.Message);

            if (transitionResult.ShouldSaveAggregate())
            {
                await _cashinRepository.SaveAsync(aggregate);
            }

            if (transitionResult.ShouldSendCommands())
            {
                sender.SendCommand
                (
                    new ReleaseDepositWalletLockCommand
                    {
                        OperationId = aggregate.OperationId,
                        BlockchainType = aggregate.BlockchainType,
                        BlockchainAssetId = aggregate.BlockchainAssetId,
                        DepositWalletAddress = aggregate.DepositWalletAddress
                    },
                    Self
                );

                _chaosKitty.Meow(aggregate.OperationId);
            }
        }

        [UsedImplicitly]
        private async Task Handle(CashinEnrolledToMatchingEngineEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashinRepository.GetAsync(evt.OperationId);

            var transitionResult = aggregate.OnEnrolledToMatchingEngine(clientId: evt.ClientId);

            if (transitionResult.ShouldSaveAggregate())
            {
                await _cashinRepository.SaveAsync(aggregate);
            }

            if (transitionResult.ShouldSendCommands())
            {
                if (!aggregate.BalanceBlock.HasValue)
                {
                    throw new InvalidOperationException("Balance block should be not null here");
                }

                if (!aggregate.EnrolledBalanceAmount.HasValue)
                {
                    throw new InvalidOperationException("Enrolled balance amount should be not null here");
                }

                if (!aggregate.OperationAmount.HasValue)
                {
                    throw new InvalidOperationException("Operation amount should be not null here");
                }

                if (!aggregate.IsDustCashin.HasValue)
                {
                    throw new InvalidOperationException("IsDustCashin should be not null here");
                }

                sender.SendCommand
                (
                    new SetEnrolledBalanceCommand
                    {
                        BalanceBlock = aggregate.BalanceBlock.Value,
                        BlockchainAssetId = aggregate.BlockchainAssetId,
                        BlockchainType = aggregate.BlockchainType,
                        DepositWalletAddress = aggregate.DepositWalletAddress,
                        EnrolledBalanceAmount = aggregate.EnrolledBalanceAmount.Value,
                        OperationAmount = aggregate.OperationAmount.Value,
                        OperationId = aggregate.OperationId
                    },
                    Self
                );

                _chaosKitty.Meow(aggregate.OperationId);
            }
        }

        [UsedImplicitly]
        private async Task Handle(EnrolledBalanceSetEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashinRepository.GetAsync(evt.OperationId);

            var transitionResult = aggregate.OnEnrolledBalanceSet();

            if (transitionResult.ShouldSaveAggregate())
            {
                await _cashinRepository.SaveAsync(aggregate);
            }

            if (transitionResult.ShouldSendCommands())
            {
                if (!aggregate.IsDustCashin.HasValue)
                {
                    throw new InvalidOperationException("IsDustCashin should be not null here");
                }

                if (!aggregate.IsDustCashin.Value)
                {
                    if (!aggregate.BalanceAmount.HasValue)
                    {
                        throw new InvalidOperationException("Balance amount should be not null here");
                    }

                    sender.SendCommand
                    (
                        new StartOperationExecutionCommand
                        {
                            Amount = aggregate.BalanceAmount.Value,
                            AssetId = aggregate.AssetId,
                            FromAddress = aggregate.DepositWalletAddress,
                            IncludeFee = true,
                            OperationId = aggregate.OperationId,
                            ToAddress = aggregate.HotWalletAddress
                        },
                        BlockchainOperationsExecutorBoundedContext.Name
                    );
                }
                else
                {
                    sender.SendCommand
                    (
                        new ReleaseDepositWalletLockCommand
                        {
                            OperationId = aggregate.OperationId,
                            BlockchainType = aggregate.BlockchainType,
                            BlockchainAssetId = aggregate.BlockchainAssetId,
                            DepositWalletAddress = aggregate.DepositWalletAddress
                        },
                        Self
                    );
                }

                _chaosKitty.Meow(aggregate.OperationId);
            }
        }

        [UsedImplicitly]
        private async Task Handle(OperationExecutionCompletedEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashinRepository.TryGetAsync(evt.OperationId);

            if (aggregate == null)
            {
                // This is not a cashin operation
                return;
            }

            var transitionResult = aggregate.OnTransactionCompleted(evt.TransactionHash, evt.Block, evt.TransactionAmount, evt.Fee);

            if (transitionResult.ShouldSaveAggregate())
            {
                await _cashinRepository.SaveAsync(aggregate);
            }

            if (transitionResult.ShouldSendCommands())
            {
                if (!aggregate.TransactionBlock.HasValue)
                {
                    throw new InvalidOperationException("Transaction block should be not null here");
                }

                sender.SendCommand
                (
                    new ResetEnrolledBalanceCommand
                    {
                        OperationId = aggregate.OperationId,
                        BlockchainType = aggregate.BlockchainType,
                        BlockchainAssetId = aggregate.BlockchainAssetId,
                        DepositWalletAddress = aggregate.DepositWalletAddress,
                        TransactionBlock = aggregate.TransactionBlock.Value
                    },
                    Self
                );

                _chaosKitty.Meow(evt.OperationId);
            }
        }

        [UsedImplicitly]
        public async Task Handle(EnrolledBalanceResetEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashinRepository.GetAsync(evt.OperationId);

            var transitionResult = aggregate.OnEnrolledBalanceReset();

            if (transitionResult.ShouldSaveAggregate())
            {
                await _cashinRepository.SaveAsync(aggregate);
            }

            if (transitionResult.ShouldSendCommands())
            {
                sender.SendCommand
                (
                    new ReleaseDepositWalletLockCommand
                    {
                        OperationId = aggregate.OperationId,
                        BlockchainType = aggregate.BlockchainType,
                        BlockchainAssetId = aggregate.BlockchainAssetId,
                        DepositWalletAddress = aggregate.DepositWalletAddress
                    },
                    Self
                );

                _chaosKitty.Meow(aggregate.OperationId);
            }
        }

        [UsedImplicitly]
        private async Task Handle(OperationExecutionFailedEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashinRepository.TryGetAsync(evt.OperationId);

            if (aggregate == null)
            {
                // This is not a cashin operation
                return;
            }

            var transitionResult = aggregate.OnTransactionFailed(evt.Error, evt.ErrorCode.MapToCashinErrorCode());

            if (!aggregate.ErrorCode.HasValue)
            {
                throw new InvalidOperationException("ErrorCode should be not null here");
            }

            if (transitionResult.ShouldSaveAggregate())
            {
                await _cashinRepository.SaveAsync(aggregate);
            }

            if (transitionResult.ShouldSendCommands())
            {
                sender.SendCommand
                (
                    new ReleaseDepositWalletLockCommand
                    {
                        OperationId = aggregate.OperationId,
                        BlockchainType = aggregate.BlockchainType,
                        BlockchainAssetId = aggregate.BlockchainAssetId,
                        DepositWalletAddress = aggregate.DepositWalletAddress
                    },
                    Self
                );

                _chaosKitty.Meow(aggregate.OperationId);
            }
        }

        [UsedImplicitly]
        private async Task Handle(DepositWalletLockReleasedEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashinRepository.GetAsync(evt.OperationId);

            var transitionResult = aggregate.OnDepositWalletLockReleased();


            if (transitionResult.ShouldSaveAggregate())
            {
                await _cashinRepository.SaveAsync(aggregate);
            }

            if (aggregate.Result == CashinResult.OutdatedBalance)
            {
                return;
            }

            // Sending the "off-blockchain operation" event, if needed.
            if (!aggregate.IsDustCashin.HasValue)
            {
                throw new InvalidOperationException("IsDustCashin should be not null here");
            }

            if (!aggregate.ClientId.HasValue)
            {
                throw new InvalidOperationException("Client ID should be not null here");
            }

            if (!aggregate.OperationAmount.HasValue)
            {
                throw new InvalidOperationException("Operation amount should be not null here");
            }

            if (aggregate.OperationAmount.Value == 0M)
            {
                throw new InvalidOperationException("Operation amount should be not 0 here");
            }

            if (transitionResult.ShouldSendCommands())
            {
                if (aggregate.ErrorCode.HasValue)
                {
                    sender.SendCommand
                    (
                        new NotifyCashinFailedCommand
                        {
                            OperationId = aggregate.OperationId,
                            Amount = aggregate.OperationAmount.Value,
                            ClientId = aggregate.ClientId.Value,
                            AssetId = aggregate.AssetId,
                            Error = aggregate.Error,
                            ErrorCode = aggregate.ErrorCode.Value.MapToCashinErrorCode()
                        },
                        Self
                    );
                }
                else if (aggregate.IsDustCashin.Value)
                {
                    sender.SendCommand
                    (
                        new NotifyCashinCompletedCommand
                        {
                            OperationAmount = aggregate.OperationAmount.Value,
                            TransactionnAmount = 0M,
                            TransactionFee = 0M,
                            AssetId = aggregate.AssetId,
                            ClientId = aggregate.ClientId.Value,
                            OperationType = CashinOperationType.OffBlockchain,
                            OperationId = aggregate.OperationId,
                            TransactionHash = "0x"
                        },
                        Self
                    );
                }
                else
                {
                    if (!aggregate.TransactionAmount.HasValue)
                    {
                        throw new InvalidOperationException("Transaction amount should be not null here");
                    }

                    if (aggregate.TransactionAmount.Value == 0M)
                    {
                        throw new InvalidOperationException("Transaction amount should be not 0 here");
                    }

                    if (!aggregate.Fee.HasValue)
                    {
                        throw new InvalidOperationException("TransactionFee should be not null here");
                    }

                    sender.SendCommand
                    (
                        new NotifyCashinCompletedCommand
                        {
                            OperationAmount = aggregate.OperationAmount.Value,
                            TransactionnAmount = aggregate.TransactionAmount.Value,
                            TransactionFee = aggregate.Fee.Value,
                            AssetId = aggregate.AssetId,
                            ClientId = aggregate.ClientId.Value,
                            OperationType = CashinOperationType.OnBlockchain,
                            OperationId = aggregate.OperationId,
                            TransactionHash = aggregate.TransactionHash
                        },
                        Self
                    );
                }

                _chaosKitty.Meow(aggregate.OperationId);
            }
        }
    }
}
