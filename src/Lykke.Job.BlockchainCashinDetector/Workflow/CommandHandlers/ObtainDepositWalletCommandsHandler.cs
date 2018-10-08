using System;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Lykke.Job.BlockchainCashinDetector.Workflow.Events;
using Lykke.MatchingEngine.Connector.Abstractions.Services;
using Lykke.MatchingEngine.Connector.Models.Api;
using Lykke.Service.BlockchainWallets.Client;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.CommandHandlers
{
    [UsedImplicitly]
    public class ObtainDepositWalletCommandsHandler
    {
        private readonly IChaosKitty _chaosKitty;
        private readonly ILog _log;
        private readonly IBlockchainWalletsClient _walletsClient;
        private readonly IMatchingEngineCallsDeduplicationRepository _deduplicationRepository;
        private readonly IMatchingEngineClient _meClient;

        public ObtainDepositWalletCommandsHandler(
            IChaosKitty chaosKitty,
            ILogFactory logFactory,
            IBlockchainWalletsClient walletsClient)
        {
            _chaosKitty = chaosKitty;
            _log = logFactory.CreateLog(this);
            _walletsClient = walletsClient;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(ObtainDepositWalletCommand command, IEventPublisher publisher)
        {
            var wallet = await _walletsClient.GetWalletAsync(
                command.BlockchainType,
                command.DepositWalletAddress);

            if (wallet == null)
            {
                throw new InvalidOperationException("Client ID for the blockchain deposit wallet address is not found");
            }

            var @event = new DepositWalletObtainedEvent()
            {
                OperationId = command.OperationId,
                ClientId = wallet.ClientId,
                BlockchainType = wallet.BlockchainType,
                CreatedBy = wallet.CreatedBy,
                DepositWalletAddress = wallet.Address
            };
            publisher.PublishEvent(@event);

            return CommandHandlingResult.Ok();
        }
    }
}
