using System;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.Job.BlockchainCashinDetector.Core.Services.LykkePay;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Lykke.Job.BlockchainCashinDetector.Workflow.Events;
using Lykke.MatchingEngine.Connector.Abstractions.Services;
using Lykke.MatchingEngine.Connector.Models.Api;
using Lykke.Service.BlockchainWallets.Client;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.CommandHandlers
{
    [UsedImplicitly]
    public class ValidateLykkePayCashinCommandsHandler
    {
        private readonly IChaosKitty _chaosKitty;
        private readonly ILog _log;
        private readonly IPayInternalServiceWrapper _payInternalServiceWrapper;

        public ValidateLykkePayCashinCommandsHandler(
            IChaosKitty chaosKitty,
            ILogFactory logFactory,
            IPayInternalServiceWrapper payInternalServiceWrapper)
        {
            _chaosKitty = chaosKitty;
            _log = logFactory.CreateLog(this);
            _payInternalServiceWrapper = payInternalServiceWrapper;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(ValidateLykkePayCashinCommand command, IEventPublisher publisher)
        {
            var validationResult = await _payInternalServiceWrapper.ValidateDepoistTransferAsync(command.IntegrationLayerId,
                command.DepositWalletAddress,
                command.TransferAmount);

            object @event = (validationResult
                ? (object)new CashinValidatedEvent()
                {
                    OperationId = command.OperationId
                }
                :
                new CashinRejectedEvent()
                {
                    OperationId = command.OperationId
                });

            publisher.PublishEvent(@event);

            return CommandHandlingResult.Ok();
        }
    }
}
