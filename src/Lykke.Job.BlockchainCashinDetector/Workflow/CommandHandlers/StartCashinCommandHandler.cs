using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Core;
using Lykke.Job.BlockchainCashinDetector.Core.Services.BLockchains;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Lykke.Job.BlockchainCashinDetector.Workflow.Events;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.CommandHandlers
{
    [UsedImplicitly]
    public class StartCashinCommandHandler
    {
        private readonly IHotWalletsProvider _hotWalletsProvider;

        public StartCashinCommandHandler(IHotWalletsProvider hotWalletsProvider)
        {
            _hotWalletsProvider = hotWalletsProvider;
        }

        [UsedImplicitly]
        public Task<CommandHandlingResult> Handle(StartCashinCommand command, IEventPublisher publisher)
        {
            var hotWalletAddress = _hotWalletsProvider.GetHotWalletAddress(command.BlockchainType);

            publisher.PublishEvent(new CashinStartRequestedEvent
            {
                BlockchainType = command.BlockchainType,
                BlockchainAssetId = command.BlockchainAssetId,
                Amount = command.Amount,
                DepositWalletAddress = command.DepositWalletAddress,
                HotWalletAddress = hotWalletAddress
            });

            ChaosKitty.Meow();

            return Task.FromResult(CommandHandlingResult.Ok());
        }
    }
}
