using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Lykke.Job.BlockchainCashinDetector.Workflow.Events;
using Lykke.Service.OperationsRepository.Client.Abstractions.CashOperations;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.CommandHandlers
{
    [UsedImplicitly]
    public class RegisterClientOperationFinishCommandsHandler
    {
        private readonly ILog _log;
        private readonly ICashOperationsRepositoryClient _clientOperationsRepositoryClient;
        private readonly IChaosKitty _chaosKitty;

        public RegisterClientOperationFinishCommandsHandler(
            ILog log,
            ICashOperationsRepositoryClient clientOperationsRepositoryClient,
            IChaosKitty chaosKitty)
        {
            _log = log;
            _clientOperationsRepositoryClient = clientOperationsRepositoryClient;
            _chaosKitty = chaosKitty;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(RegisterClientOperationFinishCommand command,
            IEventPublisher publisher)
        {

            _log.WriteInfo(nameof(RegisterClientOperationFinishCommand), command, "");

            await _clientOperationsRepositoryClient.UpdateBlockchainHashAsync(
                command.ClientId.ToString(),
                command.OperationId.ToString(),
                command.TransactionHash);

            _chaosKitty.Meow(command.OperationId);

            publisher.PublishEvent(new ClientOperationFinishRegisteredEvent
            {
                OperationId = command.OperationId
            });

            return CommandHandlingResult.Ok();
        }
    }
}

