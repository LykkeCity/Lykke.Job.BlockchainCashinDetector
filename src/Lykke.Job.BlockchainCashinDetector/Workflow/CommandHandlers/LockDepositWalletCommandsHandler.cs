using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Lykke.Job.BlockchainCashinDetector.Workflow.Events;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.CommandHandlers
{
    [UsedImplicitly]
    public class LockDepositWalletCommandsHandler
    {
        private readonly IChaosKitty _chaosKitty;
        private readonly IDepositWalletLockRepository _depositWalletLockRepository;
        private readonly IEnrolledBalanceRepository _enrolledBalanceRepository;

        public LockDepositWalletCommandsHandler(
            IChaosKitty chaosKitty,
            IDepositWalletLockRepository depositWalletLockRepository,
            IEnrolledBalanceRepository enrolledBalanceRepository)
        {
            _chaosKitty = chaosKitty;
            _depositWalletLockRepository = depositWalletLockRepository;
            _enrolledBalanceRepository = enrolledBalanceRepository;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(LockDepositWalletCommand command, IEventPublisher publisher)
        {
            var depositWalletKey = new DepositWalletKey
            (
                command.BlockchainAssetId,
                command.BlockchainType,
                command.DepositWalletAddress
            );

            var depositWalletLock = await _depositWalletLockRepository.LockAsync
            (
                depositWalletKey,
                command.DepositWalletBalance,
                command.DepositWalletBlock,
                CashinAggregate.GetNextId
            );

            var enrolledBalance = await _enrolledBalanceRepository.TryGetAsync(depositWalletKey);

            _chaosKitty.Meow(depositWalletLock.OperationId);

            publisher.PublishEvent(new DepositWalletLockedEvent
            {
                OperationId = depositWalletLock.OperationId,
                BlockchainType = command.BlockchainType,
                BlockchainAssetId = command.BlockchainAssetId,
                DepositWalletAddress = command.DepositWalletAddress,
                LockedAtBalance = depositWalletLock.Balance,
                LockedAtBlock = depositWalletLock.Block,
                EnrolledBalance = enrolledBalance?.Balance ?? 0,
                EnrolledBlock = enrolledBalance?.Block ?? 0,
                AssetId = command.AssetId,
                AssetAccuracy = command.AssetAccuracy,
                BlockchainAssetAccuracy = command.BlockchainAssetAccuracy,
                CashinMinimalAmount = command.CashinMinimalAmount,
                HotWalletAddress = command.HotWalletAddress
            });

            return CommandHandlingResult.Ok();
        }
    }
}
