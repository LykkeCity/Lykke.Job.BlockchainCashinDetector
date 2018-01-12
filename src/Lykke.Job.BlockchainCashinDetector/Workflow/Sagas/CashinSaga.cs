using System;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Contract;
using Lykke.Job.BlockchainCashinDetector.Core.Domain.Cashin.Commands;
using Lykke.Job.BlockchainCashinDetector.Core.Domain.Cashin.Events;
using Lykke.Job.BlockchainCashinDetector.Core.Services.BLockchains;
using Lykke.Job.BlockchainTransfersExecutor.Contract;
using Lykke.Service.Assets.Client;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Sagas
{
    /// <summary>
    /// DepositWalletsBalanceProcessingPeriodicalHandler 
    /// -> EnrollToMatchingEngineCommand
    /// -> CashinEnrolledToMatchingEngineEvent
    /// -> BlockchainTransfersExecutor : StartTransferCommand
    /// -> BlockchainTransfersExecutor : 
    ///     TransferCompleted 
    ///         -> EndCashinCommand
    ///     TransferFailed
    ///         -> EndCashinCommand
    /// </summary>
    [UsedImplicitly]
    public class CashinSaga
    {
        private readonly IHotWalletsProvider _hotWalletsProvider;
        private readonly IAssetsServiceWithCache _assetsService;
        private readonly ILog _log;
        private readonly RetryDelayProvider _delayProvider;

        public CashinSaga(
            ILog log,
            RetryDelayProvider delayProvider,
            IHotWalletsProvider hotWalletsProvider,
            IAssetsServiceWithCache assetsService)
        {
            _hotWalletsProvider = hotWalletsProvider;
            _assetsService = assetsService;
            _log = log;
            _delayProvider = delayProvider;
        }

        [UsedImplicitly]
        private Task Handle(CashinEnrolledToMatchingEngineEvent evt, ICommandSender sender)
        {
            _log.WriteInfo(nameof(CashinEnrolledToMatchingEngineEvent), evt, "");

            var hotWalletAddress = _hotWalletsProvider.GetHotWalletAddress(evt.BlockchainType);

            sender.SendCommand(new BlockchainTransfersExecutor.Contract.Commands.StartTransferCommand
            {
                OperationId = evt.OperationId,
                BlockchainType = evt.BlockchainType,
                FromAddress = evt.BlockchainDepositWalletAddress,
                ToAddress = hotWalletAddress,
                AssetId = evt.AssetId,
                Amount = evt.Amount,
                IncludeFee = true
            }, BlockchainTransferExecutorBoundedContext.Name);

            return Task.CompletedTask;
        }

        [UsedImplicitly]
        private async Task Handle(BlockchainTransfersExecutor.Contract.Events.TransferCompletedEvent evt, ICommandSender sender)
        {
            _log.WriteInfo(nameof(BlockchainTransfersExecutor.Contract.Events.TransferCompletedEvent), evt, "");

            var asset = await _assetsService.TryGetAssetAsync(evt.AssetId);

            if (asset == null)
            {
                throw new InvalidOperationException("Asset not found");
            }

            sender.SendCommand(new EndCashinCommand
            {
                OperationId = evt.OperationId,
                BlockchainType = evt.BlockchainType,
                BlockchainDepositWalletAddress = evt.FromAddress,
                AssetId = evt.AssetId
            }, BlockchainCashinDetectorBoundedContext.Name);
        }

        [UsedImplicitly]
        private async Task Handle(BlockchainTransfersExecutor.Contract.Events.TransferFailedEvent evt, ICommandSender sender)
        {
            _log.WriteInfo(nameof(BlockchainTransfersExecutor.Contract.Events.TransferFailedEvent), evt, "");

            var asset = await _assetsService.TryGetAssetAsync(evt.AssetId);

            if (asset == null)
            {
                throw new InvalidOperationException("Asset not found");
            }

            sender.SendCommand(new EndCashinCommand
            {
                OperationId = evt.OperationId,
                BlockchainType = evt.BlockchainType,
                BlockchainDepositWalletAddress = evt.FromAddress,
                AssetId = evt.AssetId
            }, BlockchainCashinDetectorBoundedContext.Name);
        }
    }
}
