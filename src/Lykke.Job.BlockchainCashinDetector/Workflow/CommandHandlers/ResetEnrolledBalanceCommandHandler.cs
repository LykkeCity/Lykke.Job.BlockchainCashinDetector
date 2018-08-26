using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Lykke.Job.BlockchainCashinDetector.Workflow.Events;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.CommandHandlers
{
    public class ResetEnrolledBalanceCommandHandler
    {
        private readonly IEnrolledBalanceRepository _enrolledBalanceRepository;

        public ResetEnrolledBalanceCommandHandler(IEnrolledBalanceRepository enrolledBalanceRepository)
        {
            _enrolledBalanceRepository = enrolledBalanceRepository;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(ResetEnrolledBalanceCommand command, IEventPublisher publisher)
        {
            await _enrolledBalanceRepository.ResetBalanceAsync
            (
                new DepositWalletKey
                (
                    command.BlockchainAssetId,
                    command.BlockchainType,
                    command.DepositWalletAddress
                ),
                command.TransactionBlock
            );

            publisher.PublishEvent(new EnrolledBalanceResetEvent
            {
                OperationId = command.OperationId
            });

            return CommandHandlingResult.Ok();
        }
    }
}
