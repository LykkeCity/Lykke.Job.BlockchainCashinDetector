using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
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
        private readonly ILog _log;
        private readonly ServiceCollection _services;

        public JobModule(
            MatchingEngineSettings meSettings,
            AssetsSettings assetsSettings,
            ILog log)
        {
            _meSettings = meSettings;
            _assetsSettings = assetsSettings;
            _log = log;
            _services = new ServiceCollection();
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_log)
                .As<ILog>()
                .SingleInstance();

            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance();

            builder.RegisterType<StartupManager>()
                .As<IStartupManager>();

            builder.RegisterType<ShutdownManager>()
                .As<IShutdownManager>();

            _services.RegisterAssetsClient(new AssetServiceSettings
            {
                BaseUri = new Uri(_assetsSettings.ServiceUrl),
                AssetsCacheExpirationPeriod = _assetsSettings.CacheExpirationPeriod,
                AssetPairsCacheExpirationPeriod = _assetsSettings.CacheExpirationPeriod
            });

            RegisterMatchingEngine(builder);

            builder.Populate(_services);
        }


        private void RegisterMatchingEngine(ContainerBuilder builder)
        {
            var socketLog = new SocketLogDynamic(
                i => { },
                str => _log.WriteInfoAsync("ME client", "", str));

            builder.BindMeClient(_meSettings.IpEndpoint.GetClientIpEndPoint(), socketLog);
        }

    }
}
