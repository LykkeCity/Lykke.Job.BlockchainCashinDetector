using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Lykke.Job.BlockchainCashinDetector.Workflow.Events;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.CommandHandlers
{
    public class UpdateDepositBalanceDetectionsDeduplicationLockCommandBalanceHandler
    {
        private readonly IChaosKitty _chaosKitty;
        private readonly ILog _log;
        private readonly IDepositBalanceDetectionsDeduplicationRepository _deduplicationRepository;

        public UpdateDepositBalanceDetectionsDeduplicationLockCommandBalanceHandler(
            IChaosKitty chaosKitty,
            ILog log,
            IDepositBalanceDetectionsDeduplicationRepository deduplicationRepository)
        {
            _chaosKitty = chaosKitty;
            _log = log;
            _deduplicationRepository = deduplicationRepository;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(UpdateDepositBalanceDetectionsDeduplicationLockCommand command, IEventPublisher publisher)
        {

            _log.WriteInfo(nameof(UpdateDepositBalanceDetectionsDeduplicationLockCommand), command, "");

            await _deduplicationRepository.InsertOrReplaceAsync
            (
                command.BlockchainType,
                command.BlockchainAssetId,
                command.DepositWalletAddress,
                command.Block
            );

            _chaosKitty.Meow(command.OperationId);

            publisher.PublishEvent(new DepositBalanceDetectionsDeduplicationLockUpdatedEvent
            {
                Block = command.Block,
                BlockchainAssetId = command.BlockchainAssetId,
                BlockchainType = command.BlockchainType,
                DepositWalletAddress = command.DepositWalletAddress,
                OperationId = command.OperationId
            });

            return CommandHandlingResult.Ok();
        }
    }
}
