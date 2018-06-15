using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Lykke.Job.BlockchainCashinDetector.Workflow.Events;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.CommandHandlers
{
    [UsedImplicitly]
    public class SetEnrolledBalanceCommandHandler
    {
        private readonly IEnrolledBalanceRepository _enrolledBalanceRepository;

        public SetEnrolledBalanceCommandHandler(IEnrolledBalanceRepository enrolledBalanceRepository)
        {
            _enrolledBalanceRepository = enrolledBalanceRepository;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(SetEnrolledBalanceCommand command, IEventPublisher publisher)
        {
            await _enrolledBalanceRepository.SetBalanceAsync
            (
                blockchainType: command.BlockchainType,
                blockchainAssetId: command.BlockchainAssetId,
                depositWalletAddress: command.DepositWalletAddress,
                amount: command.EnrolledBalanceAmount + command.OperationAmount,
                balanceBlock: command.BalanceBlock
            );

            publisher.PublishEvent(new EnrolledBalanceSetEvent
            {
                OperationId = command.OperationId
            });

            return CommandHandlingResult.Ok();
        }
    }
}
