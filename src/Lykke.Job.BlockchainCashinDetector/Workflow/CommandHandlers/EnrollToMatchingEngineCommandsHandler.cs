using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Core;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Lykke.Job.BlockchainCashinDetector.Workflow.Events;
using Lykke.MatchingEngine.Connector.Abstractions.Models;
using Lykke.MatchingEngine.Connector.Abstractions.Services;
using Lykke.Service.Assets.Client;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.CommandHandlers
{
    [UsedImplicitly]
    public class EnrollToMatchingEngineCommandsHandler
    {
        private readonly ILog _log;
        private readonly IAssetsServiceWithCache _assetsService;
        private readonly IMatchingEngineCallsDeduplicationRepository _deduplicationRepository;
        private readonly IMatchingEngineClient _meClient;

        public EnrollToMatchingEngineCommandsHandler(
            ILog log,
            IAssetsServiceWithCache assetsService,
            IMatchingEngineCallsDeduplicationRepository deduplicationRepository, 
            IMatchingEngineClient meClient)
        {
            _log = log;
            _assetsService = assetsService;
            _deduplicationRepository = deduplicationRepository;
            _meClient = meClient;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(EnrollToMatchingEngineCommand command, IEventPublisher publisher)
        {
            ChaosKitty.Meow();

            // First level deduplication just to reduce traffic to the ME
            if (await _deduplicationRepository.IsExists(command.OperationId))
            {
                _log.WriteInfo(nameof(EnrollToMatchingEngineCommand), command.OperationId, "Deduplicated");

                // Skips silently
                return CommandHandlingResult.Ok();
            }

            var clientId = TryGetClientId(command.DepositWalletAddress);

            if (clientId == null)
            {
                throw new InvalidOperationException($"Client ID for the blockchain deposit wallet address is not found");
            }

            var assets = await _assetsService.GetAllAssetsAsync();
            var asset = assets.FirstOrDefault(a => a.BlockChainAssetId == command.BlockchainAssetId);

            if (asset == null)
            {
                throw new InvalidOperationException($"Asset for the blockchain asset is not found");
            }

            ChaosKitty.Meow();

            var cashInResult = await _meClient.CashInOutAsync(
                command.OperationId.ToString(),
                clientId,
                asset.Id,
                (double) command.Amount);

            ChaosKitty.Meow();

            if (cashInResult == null)
            {
                throw new InvalidOperationException("ME response is null, don't know what to do");
            }

            if (cashInResult.Status == MeStatusCodes.Ok ||
                cashInResult.Status == MeStatusCodes.Duplicate)
            {
                if (cashInResult.Status == MeStatusCodes.Duplicate)
                {
                    _log.WriteInfo(nameof(EnrollToMatchingEngineCommand), command.OperationId, "Deduplicated by the ME");
                }

                publisher.PublishEvent(new CashinEnrolledToMatchingEngineEvent
                {
                    OperationId = command.OperationId,
                    ClientId = clientId,
                    AssetId = asset.Id
                });

                ChaosKitty.Meow();

                await _deduplicationRepository.InsertOrReplaceAsync(command.OperationId);

                ChaosKitty.Meow();

                return CommandHandlingResult.Ok();
            }

            throw new InvalidOperationException($"Cashin into the ME is failed. ME status: {cashInResult.Status}, ME message: {cashInResult.Message}");
        }

        private string TryGetClientId(string commandDepositWalletAddress)
        {
            // TODO:
            return $"fake-client-for-{commandDepositWalletAddress}";
        }
    }
}
