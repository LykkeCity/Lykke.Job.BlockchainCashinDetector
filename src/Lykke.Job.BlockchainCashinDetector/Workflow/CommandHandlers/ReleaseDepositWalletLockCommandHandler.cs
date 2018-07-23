using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Lykke.Job.BlockchainCashinDetector.Workflow.Events;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.CommandHandlers
{
    [UsedImplicitly]
    public class ReleaseDepositWalletLockCommandHandler
    {
        private readonly IDepositWalletLockRepository _depositWalletLockRepository;

        public ReleaseDepositWalletLockCommandHandler(IDepositWalletLockRepository depositWalletLockRepository)
        {
            _depositWalletLockRepository = depositWalletLockRepository;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(ReleaseDepositWalletLockCommand command, IEventPublisher publisher)
        {
            await _depositWalletLockRepository.ReleaseAsync(
                command.BlockchainType,
                command.DepositWalletAddress,
                command.BlockchainAssetId,
                command.OperationId);

            publisher.PublishEvent(new DepositWalletLockReleasedEvent
            {
                OperationId = command.OperationId
            });

            return CommandHandlingResult.Ok();
        }
    }
}
