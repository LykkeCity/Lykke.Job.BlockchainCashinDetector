using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Contract;
using Lykke.Job.BlockchainCashinDetector.Core.Domain.Cashin.Commands;
using Lykke.Job.BlockchainCashinDetector.Core.Domain.Cashin.Events;
using Lykke.Job.BlockchainCashinDetector.Core.Services.BLockchains;
using Lykke.Job.BlockchainOperationsExecutor.Contract;
using Lykke.Service.Assets.Client;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Sagas
{
    /// <summary>
    /// DepositWalletsBalanceProcessingPeriodicalHandler 
    /// -> EnrollToMatchingEngineCommand
    /// -> CashinEnrolledToMatchingEngineEvent
    /// -> BlockchainOperationsExecutor : StartOperationCommand
    /// -> BlockchainOperationsExecutor : 
    ///     OperationCompleted 
    ///         -> EndCashinCommand
    ///     OperationFailed
    ///         -> EndCashinCommand
    /// </summary>
    [UsedImplicitly]
    public class CashinSaga
    {
        private readonly IHotWalletsProvider _hotWalletsProvider;
        private readonly ILog _log;

        public CashinSaga(
            ILog log,
            IHotWalletsProvider hotWalletsProvider,
            IAssetsServiceWithCache assetsService)
        {
            _hotWalletsProvider = hotWalletsProvider;
            _log = log;
        }

        [UsedImplicitly]
        private Task Handle(CashinEnrolledToMatchingEngineEvent evt, ICommandSender sender)
        {
            ChaosKitty.Meow();

            var hotWalletAddress = _hotWalletsProvider.GetHotWalletAddress(evt.BlockchainType);

            sender.SendCommand(new BlockchainOperationsExecutor.Contract.Commands.StartOperationCommand
            {
                OperationId = evt.OperationId,
                BlockchainType = evt.BlockchainType,
                FromAddress = evt.BlockchainDepositWalletAddress,
                ToAddress = hotWalletAddress,
                AssetId = evt.AssetId,
                Amount = evt.Amount,
                IncludeFee = true
            }, BlockchainOperationsExecutorBoundedContext.Name);

            ChaosKitty.Meow();

            return Task.CompletedTask;
        }

        [UsedImplicitly]
        private Task Handle(BlockchainOperationsExecutor.Contract.Events.OperationCompletedEvent evt, ICommandSender sender)
        {
            ChaosKitty.Meow();

            sender.SendCommand(new EndCashinCommand
            {
                OperationId = evt.OperationId,
                BlockchainType = evt.BlockchainType,
                BlockchainDepositWalletAddress = evt.FromAddress,
                AssetId = evt.AssetId
            }, BlockchainCashinDetectorBoundedContext.Name);

            ChaosKitty.Meow();

            return Task.CompletedTask;
        }

        [UsedImplicitly]
        private Task Handle(BlockchainOperationsExecutor.Contract.Events.OperationFailedEvent evt, ICommandSender sender)
        {
            ChaosKitty.Meow();

            sender.SendCommand(new EndCashinCommand
            {
                OperationId = evt.OperationId,
                BlockchainType = evt.BlockchainType,
                BlockchainDepositWalletAddress = evt.FromAddress,
                AssetId = evt.AssetId
            }, BlockchainCashinDetectorBoundedContext.Name);

            ChaosKitty.Meow();

            return Task.CompletedTask;
        }
    }
}
