using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Lykke.Job.BlockchainCashinDetector.Workflow.Events;
using Lykke.Service.OperationsRepository.AutorestClient.Models;
using Lykke.Service.OperationsRepository.Client.Abstractions.CashOperations;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.CommandHandlers
{
    [UsedImplicitly]
    public class RegisterClientOperationCommandsHandler
    {
        private readonly ILog _log;
        private readonly ICashOperationsRepositoryClient _clientOperationsRepositoryClient;
        private readonly IChaosKitty _chaosKitty;

        public RegisterClientOperationCommandsHandler(
            ILog log,
            ICashOperationsRepositoryClient clientOperationsRepositoryClient,
            IChaosKitty chaosKitty)
        {
            _log = log;
            _clientOperationsRepositoryClient = clientOperationsRepositoryClient;
            _chaosKitty = chaosKitty;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(RegisterClientOperationStartCommand command, IEventPublisher publisher)
        {
#if DEBUG
            _log.WriteInfo(nameof(RemoveMatchingEngineDeduplicationLockCommand), command, "");
#endif
            await _clientOperationsRepositoryClient.RegisterAsync(new CashInOutOperation(
                id: command.OperationId.ToString(),
                dateTime: command.Moment,
                amount: (double)command.Amount,
                assetId: command.AssetId,
                clientId: command.ClientId.ToString(),
                addressTo: command.DepositWalletAddress,
                type: CashOperationType.ForwardCashIn,
                state: TransactionStates.SettledNoChain,
                isSettled: true,

                // These fields are not used

                transactionId: "",
                feeType: FeeType.Unknown,
                feeSize: 0,
                isRefund: false,
                multisig: "",
                isHidden: false,

                // These fields will set later by transactions history detector job

                addressFrom: "",
                blockChainHash: ""
            ));

            _chaosKitty.Meow(command.OperationId);

            publisher.PublishEvent(new ClientOperationRegisteredEvent
            {
                OperationId = command.OperationId
            });

            return CommandHandlingResult.Ok();
        }
    }
}
