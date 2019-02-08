using System;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Lykke.Job.BlockchainCashinDetector.Workflow.Events;
using Lykke.Service.BlockchainWallets.Client;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.CommandHandlers
{
    public class RetrieveClientCommandHandler
    {
        private readonly IChaosKitty _chaosKitty;
        private readonly IBlockchainWalletsClient _walletsClient;

        public RetrieveClientCommandHandler(
            IChaosKitty chaosKitty,
            IBlockchainWalletsClient walletsClient)
        {
            _chaosKitty = chaosKitty;
            _walletsClient = walletsClient;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(RetrieveClientCommand command, IEventPublisher publisher)
        {
            // TODO: Add client cache for the walletsClient

            var clientId = await _walletsClient.TryGetClientIdAsync(
                command.BlockchainType,
                command.DepositWalletAddress
            );

            if (clientId == null)
            {
                throw new InvalidOperationException("Client ID for the blockchain deposit wallet address is not found");
            }

            publisher.PublishEvent(new ClientRetrievedEvent
            {
                OperationId = command.OperationId,
                ClientId = clientId.Value
            });

            _chaosKitty.Meow(command.OperationId);

            return CommandHandlingResult.Ok();
        }
    }
}