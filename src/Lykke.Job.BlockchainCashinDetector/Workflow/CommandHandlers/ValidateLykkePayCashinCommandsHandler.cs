using System;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
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
        private readonly IBlockchainWalletsClient _walletsClient;
        private readonly IMatchingEngineCallsDeduplicationRepository _deduplicationRepository;
        private readonly IMatchingEngineClient _meClient;
        private readonly IPayInternalServiceWrapper _payInternalServiceWrapper;

        public ValidateLykkePayCashinCommandsHandler(
            IChaosKitty chaosKitty,
            ILog log,
            IPayInternalServiceWrapper payInternalServiceWrapper)
        {
            _chaosKitty = chaosKitty;
            _log = log;
            _payInternalServiceWrapper = payInternalServiceWrapper;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(ValidateLykkePayCashinCommand command, IEventPublisher publisher)
        {
            var validationResult = await _payInternalServiceWrapper.ValidateDepoistTransferAsync(command.IntegrationLayerId,
                command.DepositWalletAddress, 
                command.TransferAmount);

            if (validationResult)
            {
                CashinValidatedEvent cashinValidatedEvent = new CashinValidatedEvent();
                publisher.PublishEvent(cashinValidatedEvent);
            }
            else
            {
                CashinRejectedEvent cashinRejectedEvent = new CashinRejectedEvent();
                publisher.PublishEvent(cashinRejectedEvent);
            }

            return CommandHandlingResult.Ok();
        }
    }
}
