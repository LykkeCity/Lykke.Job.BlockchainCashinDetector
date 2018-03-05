using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Lykke.Job.BlockchainCashinDetector.Workflow.Events;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.CommandHandlers
{
    public class ResetEnrolledBalanceCommandHandler
    {
        private readonly ILog _log;
        private readonly IEnrolledBalanceRepository _enrolledBalanceRepository;

        public ResetEnrolledBalanceCommandHandler(
            ILog log,
            IEnrolledBalanceRepository enrolledBalanceRepository)
        {
            _enrolledBalanceRepository = enrolledBalanceRepository;
            _log = log;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(ResetEnrolledBalanceCommand command, IEventPublisher publisher)
        {
            _log.WriteInfo(nameof(ResetEnrolledBalanceCommand), command, "");

            await _enrolledBalanceRepository.ResetBalanceAsync
            (
                command.BlockchainType,
                command.BlockchainAssetId,
                command.DepositWalletAddress,
                command.Block
            );

            publisher.PublishEvent(new EnrolledBalanceResetEvent
            {
                OperationId = command.OperationId
            });

            return CommandHandlingResult.Ok();
        }
    }
}
