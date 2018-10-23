using System;
using Autofac;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Job.BlockchainCashinDetector.Services;
using Lykke.Job.BlockchainCashinDetector.Settings;
using Lykke.Sdk;
using Lykke.Service.Assets.Client;
using Lykke.SettingsReader;

namespace Lykke.Job.BlockchainCashinDetector.Modules
{
    [UsedImplicitly]
    public class JobModule : Module
    {
        private readonly IReloadingManager<AppSettings> _settings;

        public JobModule(IReloadingManager<AppSettings> settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<StartupManager>()
                .As<IStartupManager>();

            builder.RegisterType<ShutdownManager>()
                .As<IShutdownManager>();

            builder.RegisterAssetsClient(new AssetServiceSettings
            {
                BaseUri = new Uri(_settings.CurrentValue.Assets.ServiceUrl),
                AssetsCacheExpirationPeriod = _settings.CurrentValue.Assets.CacheExpirationPeriod,
                AssetPairsCacheExpirationPeriod = _settings.CurrentValue.Assets.CacheExpirationPeriod
            });

            builder.RegisgterMeClient(_settings.CurrentValue.MatchingEngineClient.IpEndpoint.GetClientIpEndPoint());
            builder.RegisterChaosKitty(_settings.CurrentValue.BlockchainCashinDetectorJob.ChaosKitty);
        }
    }
}
