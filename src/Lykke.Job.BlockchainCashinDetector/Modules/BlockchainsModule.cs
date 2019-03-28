using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using JetBrains.Annotations;
using Lykke.HttpClientGenerator.Caching;
using Lykke.Job.BlockchainCashinDetector.Core.Services.BLockchains;
using Lykke.Job.BlockchainCashinDetector.Services.Blockchains;
using Lykke.Job.BlockchainCashinDetector.Settings;
using Lykke.Job.BlockchainCashinDetector.Workflow.PeriodicalHandlers;
using Lykke.Service.BlockchainApi.Client;
using Lykke.Service.BlockchainSettings.Client;
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
            var cacheManager = new ClientCacheManager();
            var factory =
                new Lykke.Service.BlockchainSettings.Client.HttpClientGenerator.BlockchainSettingsClientFactory();
            var client = factory.CreateNew(
                _settings.CurrentValue.BlockchainSettingsServiceClient.ServiceUrl,
                _settings.CurrentValue.BlockchainSettingsServiceClient.ApiKey, 
                true, 
                cacheManager);
            builder.RegisterInstance(client)
                .As<IBlockchainSettingsClient>();

            builder.RegisterInstance(cacheManager)
                .As<IClientCacheManager>()
                .SingleInstance();

            var allSettings = client.GetAllSettingsAsync().Result;

            if (allSettings?.Collection == null || !allSettings.Collection.Any())
            {
                throw new Exception("There is no/or empty response from Lykke.Service.BlockchainSettings. " +
                                    "It is impossible to start CashinDetectorJob");
            }

            var blockchainIntegrations = allSettings.Collection.ToList();
            var typeHotwalletDictionary = blockchainIntegrations.ToDictionary(x => x.Type, y => y.HotWalletAddress);

            builder.RegisterType<HotWalletsProvider>()
                .As<IHotWalletsProvider>()
                .WithParameter(TypedParameter.From<IReadOnlyDictionary<string, string>>(typeHotwalletDictionary))
                .SingleInstance();

            builder.RegisterType<BlockchainApiClientProvider>()
                .As<IBlockchainApiClientProvider>();

            builder.RegisterType<BlockchainWalletsClient>()
                .As<IBlockchainWalletsClient>()
                .WithParameter(TypedParameter.From(_settings.CurrentValue.BlockchainWalletsServiceClient.ServiceUrl))
                .SingleInstance();

            foreach (var blockchain in blockchainIntegrations)
            {
                Console.WriteLine($"Registering blockchain: {blockchain.Type} -> \r\nAPI: {blockchain.ApiUrl}\r\nHW: {blockchain.HotWalletAddress}");

                builder.RegisterType<BlockchainApiClient>()
                    .Named<IBlockchainApiClient>(blockchain.Type)
                    .WithParameter(TypedParameter.From(blockchain.ApiUrl))
                    .SingleInstance()
                    .AutoActivate();

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
