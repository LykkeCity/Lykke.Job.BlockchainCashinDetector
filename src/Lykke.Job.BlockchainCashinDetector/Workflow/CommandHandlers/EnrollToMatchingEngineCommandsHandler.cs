using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Core.Domain.Cashin;
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
        private readonly RetryDelayProvider _delayProvider;
        private readonly IOperationsDeduplicationRepository _deduplicationRepository;
        private readonly IMatchingEngineClient _meClient;
        private readonly IAssetsServiceWithCache _assetsService;

        public EnrollToMatchingEngineCommandsHandler(
            ILog log,
            RetryDelayProvider delayProvider,
            IOperationsDeduplicationRepository deduplicationRepository, 
            IMatchingEngineClient meClient,
            IAssetsServiceWithCache assetsService)
        {
            _log = log;
            _delayProvider = delayProvider;
            _deduplicationRepository = deduplicationRepository;
            _meClient = meClient;
            _assetsService = assetsService;
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

            ChaosKitty.Meow();

            var clientId = GetClientId();
            var assets = await _assetsService.GetAllAssetsAsync();
            var asset = assets.FirstOrDefault(a => a.BlockChainAssetId == command.BlockchainAssetId);

            if (asset == null)
            {
                _log.WriteWarning(nameof(EnrollToMatchingEngineCommand), command, "Asset for the blockchain is not found");

                return CommandHandlingResult.Fail(_delayProvider.RetryDelay);
            }

            var cashInResult = await _meClient.CashInOutAsync(
                command.OperationId.ToString(),
                clientId,
                asset.Id,
                (double) command.Amount);

            ChaosKitty.Meow();

            if (cashInResult == null)
            {
                _log.WriteWarning(nameof(EnrollToMatchingEngineCommand), command, "ME response is null, don't know what to do");

                return CommandHandlingResult.Fail(_delayProvider.RetryDelay);
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
                    BlockchainType = command.BlockchainType,
                    BlockchainDepositWalletAddress = command.BlockchainDepositWalletAddress,
                    BlockchainAssetId = command.BlockchainAssetId,
                    ClientId = clientId,
                    AssetId = asset.Id,
                    Amount = command.Amount
                });

                ChaosKitty.Meow();

                await _deduplicationRepository.InsertOrReplaceAsync(command.OperationId);

                ChaosKitty.Meow();

                return CommandHandlingResult.Ok();
            }

            _log.WriteWarning(
                nameof(EnrollToMatchingEngineCommand), 
                command, 
                $"Cashin into the ME is failed. ME status: {cashInResult.Status}, ME message: {cashInResult.Message}");

            return CommandHandlingResult.Fail(_delayProvider.RetryDelay);
        }

        private string GetClientId()
        {
            // TODO:
            return "fake-client";
        }
    }
}
