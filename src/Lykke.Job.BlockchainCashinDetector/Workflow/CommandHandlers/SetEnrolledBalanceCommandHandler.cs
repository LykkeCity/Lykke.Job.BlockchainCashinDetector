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
    public class SetEnrolledBalanceCommandHandler
    {
        private readonly ILog _log;
        private readonly IEnrolledBalanceRepository _enrolledBalanceRepository;

        public SetEnrolledBalanceCommandHandler(
            ILog log,
            IEnrolledBalanceRepository enrolledBalanceRepository)
        {
            _log = log;
            _enrolledBalanceRepository = enrolledBalanceRepository;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(SetEnrolledBalanceCommand command, IEventPublisher publisher)
        {
            _log.WriteInfo(nameof(SetEnrolledBalanceCommand), command, "");
            
            await _enrolledBalanceRepository.SetBalanceAsync
            (
                blockchainType: command.BlockchainType,
                blockchainAssetId: command.BlockchainAssetId,
                depositWalletAddress: command.DepositWalletAddress,
                amount: command.EnrolledBalanceAmount + command.OperationAmount
            );

            publisher.PublishEvent(new EnrolledBalanceSetEvent
            {
                OperationId = command.OperationId
            });

            return CommandHandlingResult.Ok();
        }
    }
}
