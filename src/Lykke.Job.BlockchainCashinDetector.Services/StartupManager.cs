using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Core.Services.BLockchains;
using Lykke.Sdk;

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
        private readonly ICqrsEngine _cqrsEngine;

        public StartupManager(
            ILogFactory logFactory, 
            IEnumerable<IDepositWalletsBalanceProcessingPeriodicalHandler> depositWalletsBalanceProcessingHandlers,
            ICqrsEngine cqrsEngine)
        {
            _log = logFactory.CreateLog(this);
            _depositWalletsBalanceProcessingHandlers = depositWalletsBalanceProcessingHandlers ?? throw new ArgumentNullException(nameof(depositWalletsBalanceProcessingHandlers));
            _cqrsEngine = cqrsEngine ?? throw new ArgumentNullException(nameof(cqrsEngine));
        }

        public async Task StartAsync()
        {
            _log.Info(nameof(StartAsync), "Starting deposit wallets balance monitoring...");

            _cqrsEngine.Start();

            _log.Info("Starting deposit wallets balance monitoring...");

            foreach (var depositWalletsBalanceProcessingHandler in _depositWalletsBalanceProcessingHandlers)
            {
                depositWalletsBalanceProcessingHandler.Start();
            }
            
            _cqrsEngine.Start();

            await Task.CompletedTask;
        }
    }
}
