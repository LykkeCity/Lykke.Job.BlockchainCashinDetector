using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.Common.Chaos;
using Lykke.Common.Log;
using Lykke.Job.BlockchainCashinDetector.Core.Services;
using Lykke.Job.BlockchainCashinDetector.Services;
using Lykke.Job.BlockchainCashinDetector.Settings.Assets;
using Lykke.Job.BlockchainCashinDetector.Settings.MeSettings;
using Lykke.MatchingEngine.Connector.Services;
using Lykke.Service.Assets.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Job.BlockchainCashinDetector.Modules
{
    public class JobModule : Module
    {
        private readonly MatchingEngineSettings _meSettings;
        private readonly AssetsSettings _assetsSettings;
        private readonly ChaosSettings _chaosSettings;

        public JobModule(
            MatchingEngineSettings meSettings,
            AssetsSettings assetsSettings,
            ChaosSettings chaosSettings)
        {
            _meSettings = meSettings;
            _assetsSettings = assetsSettings;
            _chaosSettings = chaosSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance();

            builder.RegisterType<StartupManager>()
                .As<IStartupManager>();

            builder.RegisterType<ShutdownManager>()
                .As<IShutdownManager>();

            builder.RegisterAssetsClient(new AssetServiceSettings
            {
                BaseUri = new Uri(_assetsSettings.ServiceUrl),
                AssetsCacheExpirationPeriod = _assetsSettings.CacheExpirationPeriod,
                AssetPairsCacheExpirationPeriod = _assetsSettings.CacheExpirationPeriod
            });

            RegisterMatchingEngine(builder);

            builder.RegisterChaosKitty(_chaosSettings);
        }

        private void RegisterMatchingEngine(ContainerBuilder builder)
        {
            builder.RegisgterMeClient(_meSettings.IpEndpoint.GetClientIpEndPoint());
        }
    }
}
