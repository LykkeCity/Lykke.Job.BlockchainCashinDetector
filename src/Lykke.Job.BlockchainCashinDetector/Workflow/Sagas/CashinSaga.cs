using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Contract;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Lykke.Job.BlockchainCashinDetector.Workflow.Events;
using Lykke.Job.BlockchainOperationsExecutor.Contract;
using Lykke.Job.BlockchainOperationsExecutor.Contract.Commands;
using Lykke.Job.BlockchainOperationsExecutor.Contract.Events;

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
                    assetAccuracy: evt.AssetAccuracy,
                    blockchainAssetId: evt.BlockchainAssetId,
                    blockchainType: evt.BlockchainType,
                    cashinMinimalAmount: evt.CashinMinimalAmount,
                    depositWalletAddress: evt.DepositWalletAddress,
                    hotWalletAddress: evt.HotWalletAddress
                )
            );

            var startResult = aggregate.Start
            (
                balanceAmount: evt.LockedAtBalance,
                balanceBlock: evt.LockedAtBlock,
                enrolledBalanceAmount: evt.EnrolledBalance,
                enrolledBalanceBlock: evt.EnrolledBlock
            );

            switch (startResult)
            {
                case CashinStartResult.Started when !aggregate.MeAmount.HasValue:
                    throw new InvalidOperationException("ME operation amount should be not null here");

                case CashinStartResult.Started:
                    sender.SendCommand
                    (
                        new EnrollToMatchingEngineCommand
                        {
                            AssetId = aggregate.AssetId,
                            BlockchainAssetId = aggregate.BlockchainAssetId,
                            BlockchainType = aggregate.BlockchainType,
                            DepositWalletAddress = aggregate.DepositWalletAddress,
                            OperationId = aggregate.OperationId,
                            MatchingEngineOperationAmount = aggregate.MeAmount.Value
                        },
                        BlockchainCashinDetectorBoundedContext.Name
                    );

                    _chaosKitty.Meow(aggregate.OperationId);

                    await _cashinRepository.SaveAsync(aggregate);
                    break;

                case CashinStartResult.OutdatedBalance:
                    sender.SendCommand
                    (
                        new ReleaseDepositWalletLockCommand
                        {
                            BlockchainAssetId = aggregate.BlockchainAssetId,
                            BlockchainType = aggregate.BlockchainType,
                            DepositWalletAddress = aggregate.DepositWalletAddress,
                            OperationId = aggregate.OperationId
                        },
                        BlockchainCashinDetectorBoundedContext.Name
                    );

                    _chaosKitty.Meow(aggregate.OperationId);

                    await _cashinRepository.SaveAsync(aggregate);
                    break;

                case CashinStartResult.CashinInProgress:
                    return;

                default:
                    throw new InvalidOperationException($"Unexpected start result {startResult}");
            }
        }

        [UsedImplicitly]
        private async Task Handle(CashinEnrolledToMatchingEngineEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashinRepository.GetAsync(evt.OperationId);

            if (aggregate.OnEnrolledToMatchingEngine(clientId: evt.ClientId))
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

                _chaosKitty.Meow(evt.OperationId);

                await _cashinRepository.SaveAsync(aggregate);
            }
        }

        [UsedImplicitly]
        private async Task Handle(EnrolledBalanceSetEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashinRepository.GetAsync(evt.OperationId);

            if (aggregate.OnEnrolledBalanceSet())
            {
                if (!aggregate.IsDustCashin)
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

                _chaosKitty.Meow(evt.OperationId);

                await _cashinRepository.SaveAsync(aggregate);
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

            if (aggregate.OnTransactionCompleted(evt.TransactionHash, evt.Block, evt.TransactionAmount, evt.Fee))
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

                if (aggregate.OperationAmount.HasValue &&
                    aggregate.OperationAmount.Value != 0)
                {
                    if (!aggregate.ClientId.HasValue)
                    {
                        throw new ArgumentException($"Operation {evt.OperationId} has no client associated with it. Fix it!");
                    }

                    sender.SendCommand
                    (
                        new NotifyCashinCompletedCommand
                        {
                            OperationAmount = aggregate.OperationAmount.Value,
                            AssetId = aggregate.AssetId,
                            ClientId = aggregate.ClientId.Value,
                            OperationId = aggregate.OperationId,
                            TransactionHash = aggregate.IsDustCashin ? @"0x" : aggregate.TransactionHash
                        },
                        Self
                    );

                    _chaosKitty.Meow(evt.OperationId);
                }

                await _cashinRepository.SaveAsync(aggregate);
            }
        }

        [UsedImplicitly]
        public async Task Handle(EnrolledBalanceResetEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashinRepository.GetAsync(evt.OperationId);

            if (aggregate.OnEnrolledBalanceReset())
            {
                sender.SendCommand(new ReleaseDepositWalletLockCommand
                {
                    OperationId = aggregate.OperationId,
                    BlockchainType = aggregate.BlockchainType,
                    BlockchainAssetId = aggregate.BlockchainAssetId,
                    DepositWalletAddress = aggregate.DepositWalletAddress
                },
                Self);

                _chaosKitty.Meow(evt.OperationId);

                await _cashinRepository.SaveAsync(aggregate);
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

            if (aggregate.OnTransactionFailed(evt.Error))
            {
                sender.SendCommand(new ReleaseDepositWalletLockCommand
                {
                    OperationId = aggregate.OperationId,
                    BlockchainType = aggregate.BlockchainType,
                    BlockchainAssetId = aggregate.BlockchainAssetId,
                    DepositWalletAddress = aggregate.DepositWalletAddress
                },
                Self);

                _chaosKitty.Meow(evt.OperationId);

                await _cashinRepository.SaveAsync(aggregate);
            }
        }

        [UsedImplicitly]
        private async Task Handle(DepositWalletLockReleasedEvent evt, ICommandSender sender)
        {
            var aggregate = await _cashinRepository.GetAsync(evt.OperationId);

            if (aggregate.OnDepositWalletLockReleased())
            {
                await _cashinRepository.SaveAsync(aggregate);
            }
        }
    }
}
