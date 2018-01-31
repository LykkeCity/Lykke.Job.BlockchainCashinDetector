using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Core.Services.BLockchains;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Lykke.Job.BlockchainCashinDetector.Workflow.Events;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.CommandHandlers
{
    [UsedImplicitly]
    public class DetectDepositBalanceCommandHandler
    {
        private readonly ILog _log;
        private readonly IHotWalletsProvider _hotWalletsProvider;

        public DetectDepositBalanceCommandHandler(
            ILog log,
            IHotWalletsProvider hotWalletsProvider)
        {
            _log = log;
            _hotWalletsProvider = hotWalletsProvider;
        }

        [UsedImplicitly]
        public Task<CommandHandlingResult> Handle(DetectDepositBalanceCommand command, IEventPublisher publisher)
        {

            _log.WriteInfo(nameof(DetectDepositBalanceCommand), command, "");

            var hotWalletAddress = _hotWalletsProvider.GetHotWalletAddress(command.BlockchainType);

            publisher.PublishEvent(new DepositBalanceDetectedEvent
            {
                BlockchainType = command.BlockchainType,
                BlockchainAssetId = command.BlockchainAssetId,
                Amount = command.Amount,
                DepositWalletAddress = command.DepositWalletAddress,
                HotWalletAddress = hotWalletAddress,
                AssetId = command.AssetId
            });

            return Task.FromResult(CommandHandlingResult.Ok());
        }
    }
}
