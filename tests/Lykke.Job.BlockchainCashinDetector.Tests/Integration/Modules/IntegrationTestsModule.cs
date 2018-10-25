using System;
using System.Collections.Generic;
using Autofac;
using Common.Log;
using Lykke.Common.Chaos;
using Lykke.Job.BlockchainCashinDetector.Core.Services.BLockchains;
using Lykke.Job.BlockchainCashinDetector.Services.Blockchains;
using Lykke.Job.BlockchainCashinDetector.Tests.Integration.Common;
using Lykke.Job.BlockchainCashinDetector.Workflow.PeriodicalHandlers;
using Lykke.MatchingEngine.Connector.Abstractions.Services;
using Lykke.Service.Assets.Client;
using Lykke.Service.BlockchainApi.Client;
using Lykke.Service.BlockchainWallets.Client;

namespace Lykke.Job.BlockchainCashinDetector.Tests.Integration.Modules
{
    public class IntegrationTestsModule : Module
    {
        private readonly ILog _log;

        public IntegrationTestsModule(ILog log)
        {
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_log)
                .As<ILog>()
                .SingleInstance();

            builder.RegisterChaosKitty(null);

            builder.Register(c => new IntegrationTestsMocks())
                .AsSelf()
                .SingleInstance();

            builder.Register(c => new HotWalletsProvider(new Dictionary<string, string>
                {
                    {Constants.Blockchains.LiteCoin, Constants.HotWallets.LiteCoin}
                }))
                .As<IHotWalletsProvider>()
                .SingleInstance();

            builder.RegisterType<BlockchainApiClientProvider>()
                .As<IBlockchainApiClientProvider>();

            builder.Register(c => c.Resolve<IntegrationTestsMocks>().WalletsClient.Object)
                .As<IBlockchainWalletsClient>()
                .SingleInstance();

            RegisterBlockchain(builder, Constants.Blockchains.LiteCoin, c => c.Resolve<IntegrationTestsMocks>().LiteCoinApiClient.Object);

            builder.Register(c => c.Resolve<IntegrationTestsMocks>().AssetsClientWithCache.Object)
                .As<IAssetsServiceWithCache>()
                .SingleInstance();

            builder.Register(c => c.Resolve<IntegrationTestsMocks>().MatchingEngineClient.Object)
                .As<IMatchingEngineClient>()
                .SingleInstance();
        }

        private static void RegisterBlockchain(ContainerBuilder builder, string blockchainType, Func<IComponentContext, IBlockchainApiClient> mockResolver)
        {
            builder.Register(mockResolver)
                .Named<IBlockchainApiClient>(blockchainType)
                .SingleInstance();

            builder.RegisterType<DepositWalletsBalanceProcessingPeriodicalHandler>()
                .As<IStartable>()
                .AutoActivate()
                .SingleInstance()
                .WithParameter(TypedParameter.From(TimeSpan.FromSeconds(1)))
                .WithParameter(TypedParameter.From(100))
                .WithParameter(TypedParameter.From(blockchainType));
        }
    }
}
