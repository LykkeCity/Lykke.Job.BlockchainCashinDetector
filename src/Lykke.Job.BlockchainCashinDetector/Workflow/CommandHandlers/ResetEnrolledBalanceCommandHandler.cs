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
    public class ResetEnrolledBalanceCommandHandler
    {
        private readonly ILog _log;
        private readonly IEnrolledBalanceRepository _enrolledBalanceRepository;

        public ResetEnrolledBalanceCommandHandler(
            ILog log,
            IEnrolledBalanceRepository enrolledBalanceRepository)
        {
            _log = log;
            _enrolledBalanceRepository = enrolledBalanceRepository;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(ResetEnrolledBalanceCommand command, IEventPublisher publisher)
        {
            _log.WriteInfo(nameof(ResetEnrolledBalanceCommand), command, "");

            await _enrolledBalanceRepository.ResetBalanceAsync
            (
                blockchainType: command.BlockchainType,
                blockchainAssetId: command.BlockchainAssetId,
                depositWalletAddress: command.DepositWalletAddress,
                block: command.TransactionBlock
            );

            publisher.PublishEvent(new EnrolledBalanceResettedEvent
            {
                OperationId = command.OperationId,
                BlockchainType = command.BlockchainType,
                BlockchainAssetId = command.BlockchainAssetId,
                DepositWalletAddress = command.DepositWalletAddress,
                TransactionBlock = command.TransactionBlock
            });

            return CommandHandlingResult.Ok();
        }
    }
}
