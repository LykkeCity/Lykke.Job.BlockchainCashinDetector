using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Lykke.Job.BlockchainCashinDetector.Workflow.Events;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.CommandHandlers
{
    [UsedImplicitly]
    public class IncreaseEnrolledBalanceCommandHandler
    {
        private readonly ILog _log;
        private readonly IEnrolledBalanceRepository _enrolledBalanceRepository;

        public IncreaseEnrolledBalanceCommandHandler(
            ILog log,
            IEnrolledBalanceRepository enrolledBalanceRepository)
        {
            _log = log;
            _enrolledBalanceRepository = enrolledBalanceRepository;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(IncreaseEnrolledBalanceCommand command, IEventPublisher publisher)
        {
            _log.WriteInfo(nameof(IncreaseEnrolledBalanceCommand), command, "");

            await _enrolledBalanceRepository.InсreaseBalanceAsync
            (
                blockchainType: command.BlockchainType,
                blockchainAssetId: command.BlockchainAssetId,
                depositWalletAddress: command.DepositWalletAddress,
                amount: command.Amount
            );

            publisher.PublishEvent(new EnrolledBalanceIncreasedEvent
            {
                OperationId = command.OperationId,
                Amount = command.Amount,
                BlockchainType = command.BlockchainType,
                BlockchainAssetId = command.BlockchainAssetId,
                DepositWalletAddress = command.DepositWalletAddress,
            });

            return CommandHandlingResult.Ok();
        }
    }
}
