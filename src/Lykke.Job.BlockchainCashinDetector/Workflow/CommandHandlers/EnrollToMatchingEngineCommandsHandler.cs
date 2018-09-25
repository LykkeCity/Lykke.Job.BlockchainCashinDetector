using System;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
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
    public class EnrollToMatchingEngineCommandsHandler
    {
        private readonly IChaosKitty _chaosKitty;
        private readonly ILog _log;
        private readonly IBlockchainWalletsClient _walletsClient;
        private readonly IMatchingEngineCallsDeduplicationRepository _deduplicationRepository;
        private readonly IMatchingEngineClient _meClient;

        public EnrollToMatchingEngineCommandsHandler(
            IChaosKitty chaosKitty,
            ILog log,
            IBlockchainWalletsClient walletsClient,
            IMatchingEngineCallsDeduplicationRepository deduplicationRepository, 
            IMatchingEngineClient meClient)
        {
            _chaosKitty = chaosKitty;
            _log = log;
            _walletsClient = walletsClient;
            _deduplicationRepository = deduplicationRepository;
            _meClient = meClient;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(EnrollToMatchingEngineCommand command, IEventPublisher publisher)
        {
            // TODO: Add client cache for the walletsClient

            var clientId = await _walletsClient.TryGetClientIdAsync(
                command.BlockchainType,                
                command.DepositWalletAddress);

            if (clientId == null)
            {
                throw new InvalidOperationException("Client ID for the blockchain deposit wallet address is not found");
            }

            // First level deduplication just to reduce traffic to the ME
            if (await _deduplicationRepository.IsExistsAsync(command.OperationId))
            {
                _log.WriteInfo(nameof(EnrollToMatchingEngineCommand), command.OperationId, "Deduplicated at first level");

                // Workflow should be continued

                publisher.PublishEvent(new CashinEnrolledToMatchingEngineEvent
                {
                    ClientId = clientId.Value,
                    OperationId = command.OperationId
                });

                return CommandHandlingResult.Ok();
            }

            var cashInResult = await _meClient.CashInOutAsync
            (
                id: command.OperationId.ToString(),
                clientId: clientId.Value.ToString(),
                assetId: command.AssetId,
                amount: command.MatchingEngineOperationAmount
            );

            _chaosKitty.Meow(command.OperationId);

            if (cashInResult == null)
            {
                throw new InvalidOperationException("ME response is null, don't know what to do");
            }

            switch (cashInResult.Status)
            {
                case MeStatusCodes.Ok:
                case MeStatusCodes.Duplicate:
                    if (cashInResult.Status == MeStatusCodes.Duplicate)
                    {
                        _log.WriteInfo(nameof(EnrollToMatchingEngineCommand), command.OperationId, "Deduplicated by the ME");
                    }

                    publisher.PublishEvent(new CashinEnrolledToMatchingEngineEvent
                    {
                        ClientId = clientId.Value,
                        OperationId = command.OperationId
                    });

                    _chaosKitty.Meow(command.OperationId);

                    await _deduplicationRepository.InsertOrReplaceAsync(command.OperationId);

                    _chaosKitty.Meow(command.OperationId);

                    return CommandHandlingResult.Ok();

                case MeStatusCodes.Runtime:
                    // Retry forever with the default delay + log the error outside.
                    throw new Exception($"Cashin into the ME is failed. ME status: {cashInResult.Status}, ME message: {cashInResult.Message}");

                default:
                    // Just abort cashin for futher manual processing. ME call could not be retried anyway if responce was received.
                    _log.WriteWarning(nameof(EnrollToMatchingEngineCommand), command.OperationId, $"Unexpected response from ME. Status: {cashInResult.Status}, ME message: {cashInResult.Message}");
                    return CommandHandlingResult.Ok();
            }
        }
    }
}
