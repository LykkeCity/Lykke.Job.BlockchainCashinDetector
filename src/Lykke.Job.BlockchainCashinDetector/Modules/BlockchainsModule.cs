using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Job.BlockchainCashinDetector.Core.Services.BLockchains;
using Lykke.Job.BlockchainCashinDetector.Services.Blockchains;
using Lykke.Job.BlockchainCashinDetector.Settings;
using Lykke.Job.BlockchainCashinDetector.Workflow.PeriodicalHandlers;
using Lykke.Service.BlockchainApi.Client;
using Lykke.Service.BlockchainWallets.Client;
using Lykke.SettingsReader;

namespace Lykke.Job.BlockchainCashinDetector.Modules
{
    [UsedImplicitly]
    public class BlockchainsModule : Module
    {
        private readonly IReloadingManager<AppSettings> _settings;

        public BlockchainsModule(IReloadingManager<AppSettings> settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<HotWalletsProvider>()
                .As<IHotWalletsProvider>()
                .WithParameter(TypedParameter.From<IReadOnlyDictionary<string, string>>(_settings.CurrentValue.BlockchainsIntegration.Blockchains.ToDictionary(b => b.Type, b => b.HotWalletAddress)))
                .SingleInstance();

            builder.RegisterType<BlockchainApiClientProvider>()
                .As<IBlockchainApiClientProvider>();

            builder.RegisterType<BlockchainWalletsClient>()
                .As<IBlockchainWalletsClient>()
                .WithParameter(TypedParameter.From(_settings.CurrentValue.BlockchainWalletsServiceClient.ServiceUrl))
                .SingleInstance();

            foreach (var blockchain in _settings.CurrentValue.BlockchainsIntegration.Blockchains.Where(b => !b.IsDisabled))
            {
                Console.WriteLine($"Registering blockchain: {blockchain.Type} -> \r\nAPI: {blockchain.ApiUrl}\r\nHW: {blockchain.HotWalletAddress}");

                builder.RegisterType<BlockchainApiClient>()
                    .Named<IBlockchainApiClient>(blockchain.Type)
                    .WithParameter(TypedParameter.From(blockchain.ApiUrl))
                    .SingleInstance();

                        if (blockchain.AreCashinsDisabled)
                        {
                    Console.WriteLine($"Cashins for blockchain {blockchain.Type} are disabled");
                }
                else
                {
                    builder.RegisterType<DepositWalletsBalanceProcessingPeriodicalHandler>()
                        .As<IDepositWalletsBalanceProcessingPeriodicalHandler>()
                        .SingleInstance()
                        .WithParameter(TypedParameter.From(_settings.CurrentValue.BlockchainCashinDetectorJob.Monitoring.Period))
                        .WithParameter(TypedParameter.From(_settings.CurrentValue.BlockchainCashinDetectorJob.Requests.BatchSize))
                        .WithParameter(TypedParameter.From(blockchain.Type));
                }
            }
        }
    }
}
