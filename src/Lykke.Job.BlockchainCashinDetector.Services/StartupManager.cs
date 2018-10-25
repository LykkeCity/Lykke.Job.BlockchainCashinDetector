using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Job.BlockchainCashinDetector.Core.Services;
using Lykke.Job.BlockchainCashinDetector.Core.Services.BLockchains;

namespace Lykke.Job.BlockchainCashinDetector.Services
{
    // NOTE: Sometimes, startup process which is expressed explicitly is not just better, 
    // but the only way. If this is your case, use this class to manage startup.
    // For example, sometimes some state should be restored before any periodical handler will be started, 
    // or any incoming message will be processed and so on.
    // Do not forget to remove As<IStartable>() and AutoActivate() from DI registartions of services, 
    // which you want to startup explicitly.
    [UsedImplicitly]
    public class StartupManager : IStartupManager
    {
        private readonly ILog _log;
        private readonly IEnumerable<IDepositWalletsBalanceProcessingPeriodicalHandler> _depositWalletsBalanceProcessingHandlers;

        public StartupManager(
            ILogFactory logFactory, 
            IEnumerable<IDepositWalletsBalanceProcessingPeriodicalHandler> depositWalletsBalanceProcessingHandlers)
        {
            _log = logFactory.CreateLog(this);
            _depositWalletsBalanceProcessingHandlers = depositWalletsBalanceProcessingHandlers ?? throw new ArgumentNullException(nameof(depositWalletsBalanceProcessingHandlers));
        }

        public async Task StartAsync()
        {
            _log.WriteInfo(nameof(StartAsync), null, "Starting deposit wallets balance monitoring...");

            foreach (var depositWalletsBalanceProcessingHandler in _depositWalletsBalanceProcessingHandlers)
            {
                depositWalletsBalanceProcessingHandler.Start();
            }

            await Task.CompletedTask;
        }
    }
}
