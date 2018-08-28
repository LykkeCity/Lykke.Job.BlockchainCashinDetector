using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.Job.BlockchainCashinDetector.Core.Services.BLockchains;
using Lykke.Service.Assets.Client;
using Lykke.Service.BlockchainApi.Client;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.PeriodicalHandlers
{
    [UsedImplicitly]
    public class DepositWalletsBalanceProcessingPeriodicalHandler : IDepositWalletsBalanceProcessingPeriodicalHandler
    {
        private readonly ILog _log;
        private readonly int _batchSize;
        private readonly string _blockchainType;
        private readonly IBlockchainApiClient _blockchainApiClient;
        private readonly ICqrsEngine _cqrsEngine;
        private readonly IAssetsServiceWithCache _assetsService;
        private readonly IEnrolledBalanceRepository _enrolledBalanceRepository;
        private readonly IHotWalletsProvider _hotWalletsProvider;
        private readonly ICashinRepository _cashinRepository;
        private readonly IDepositWalletLockRepository _depositWalletLockRepository;
        private readonly IChaosKitty _chaosKitty;

        private readonly ITimerTrigger _timer;

        public DepositWalletsBalanceProcessingPeriodicalHandler(
            ILog log,
            TimeSpan period,
            int batchSize,
            string blockchainType,
            IBlockchainApiClientProvider blockchainApiClientProvider,
            ICqrsEngine cqrsEngine,
            IAssetsServiceWithCache assetsService,
            IEnrolledBalanceRepository enrolledBalanceRepository,
            IHotWalletsProvider hotWalletsProvider,
            ICashinRepository cashinRepository,
            IDepositWalletLockRepository depositWalletLockRepository,
            IChaosKitty chaosKitty)
        {
            _log = log;
            _batchSize = batchSize;
            _blockchainType = blockchainType;
            _blockchainApiClient = blockchainApiClientProvider.Get(blockchainType);
            _cqrsEngine = cqrsEngine;
            _assetsService = assetsService;
            _enrolledBalanceRepository = enrolledBalanceRepository;
            _hotWalletsProvider = hotWalletsProvider;
            _cashinRepository = cashinRepository;
            _depositWalletLockRepository = depositWalletLockRepository;
            _chaosKitty = chaosKitty;

            _timer = new TimerTrigger(
                $"{nameof(DepositWalletsBalanceProcessingPeriodicalHandler)} : {blockchainType}",
                period, 
                log);

            _timer.Triggered += ProcessBalancesAsync;   
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Dispose()
        {
            _timer.Dispose();
        }

        private async Task ProcessBalancesAsync(ITimerTrigger timer, TimerTriggeredHandlerArgs args, CancellationToken cancellationToken)
        {
            var assets = (await _assetsService.GetAllAssetsAsync(false, cancellationToken))
                .Where(a => a.BlockchainIntegrationLayerId == _blockchainType)
                .ToDictionary(
                    a => a.BlockchainIntegrationLayerAssetId,
                    a => a);
            var blockchainAssets = await _blockchainApiClient.GetAllAssetsAsync(_batchSize);

            var balanceProcessor = new BalanceProcessor(
                _blockchainType,
                _log,
                _hotWalletsProvider,
                _blockchainApiClient,
                _cqrsEngine,
                _enrolledBalanceRepository,
                assets,
                blockchainAssets);

            await balanceProcessor.ProcessAsync(_batchSize);            
        }
    }
}
