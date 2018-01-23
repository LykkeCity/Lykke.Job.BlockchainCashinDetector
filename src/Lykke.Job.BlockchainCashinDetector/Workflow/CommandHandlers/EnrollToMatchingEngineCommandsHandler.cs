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
using Lykke.Service.BlockchainWallets.Client;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.CommandHandlers
{
    [UsedImplicitly]
    public class EnrollToMatchingEngineCommandsHandler
    {
        private readonly ILog _log;
        private readonly IAssetsServiceWithCache _assetsService;
        private readonly IBlockchainWalletsClient _walletsClient;
        private readonly IMatchingEngineCallsDeduplicationRepository _deduplicationRepository;
        private readonly IMatchingEngineClient _meClient;

        public EnrollToMatchingEngineCommandsHandler(
            ILog log,
            IAssetsServiceWithCache assetsService,
            IBlockchainWalletsClient walletsClient,
            IMatchingEngineCallsDeduplicationRepository deduplicationRepository, 
            IMatchingEngineClient meClient)
        {
            _log = log;
            _assetsService = assetsService;
            _walletsClient = walletsClient;
            _deduplicationRepository = deduplicationRepository;
            _meClient = meClient;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(EnrollToMatchingEngineCommand command, IEventPublisher publisher)
        {
#if DEBUG
            _log.WriteInfo(nameof(EnrollToMatchingEngineCommand), command, "");
#endif
            // First level deduplication just to reduce traffic to the ME
            if (await _deduplicationRepository.IsExistsAsync(command.OperationId))
            {
                _log.WriteInfo(nameof(EnrollToMatchingEngineCommand), command.OperationId, "Deduplicated");

                // Just skips
                return CommandHandlingResult.Ok();
            }

            var clientId = await _walletsClient.TryGetClientIdAsync(command.BlockchainType, command.BlockchainAssetId, command.DepositWalletAddress);

            if (clientId == null)
            {
                throw new InvalidOperationException("Client ID for the blockchain deposit wallet address is not found");
            }

            var assets = await _assetsService.GetAllAssetsAsync(false);
            var asset = assets.FirstOrDefault(a =>
                a.BlockchainIntegrationLayerId == command.BlockchainType &&
                a.BlockchainIntegrationLayerAssetId == command.BlockchainAssetId);

            if (asset == null)
            {
                throw new InvalidOperationException("Asset for the blockchain asset is not found");
            }

            ChaosKitty.Meow(command.OperationId);

            var cashInResult = await _meClient.CashInOutAsync(
                command.OperationId.ToString(),
                clientId.Value.ToString(),
                asset.Id,
                (double) command.Amount);

            ChaosKitty.Meow(command.OperationId);

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
                    ClientId = clientId.Value,
                    AssetId = asset.Id
                });

                ChaosKitty.Meow(command.OperationId);

                await _deduplicationRepository.InsertOrReplaceAsync(command.OperationId);

                ChaosKitty.Meow(command.OperationId);

                return CommandHandlingResult.Ok();
            }

            throw new InvalidOperationException($"Cashin into the ME is failed. ME status: {cashInResult.Status}, ME message: {cashInResult.Message}");
        }
    }
}
