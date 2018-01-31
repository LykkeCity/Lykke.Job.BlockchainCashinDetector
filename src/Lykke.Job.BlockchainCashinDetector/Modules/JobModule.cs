﻿using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.Common.Chaos;
using Lykke.Job.BlockchainCashinDetector.Core.Services;
using Lykke.Job.BlockchainCashinDetector.Services;
using Lykke.Job.BlockchainCashinDetector.Settings.Assets;
using Lykke.Job.BlockchainCashinDetector.Settings.MeSettings;
using Lykke.MatchingEngine.Connector.Services;
using Lykke.Service.Assets.Client;
using Lykke.Service.OperationsRepository.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Job.BlockchainCashinDetector.Modules
{
    public class JobModule : Module
    {
        private readonly MatchingEngineSettings _meSettings;
        private readonly AssetsSettings _assetsSettings;
        private readonly ChaosSettings _chaosSettings;
        private readonly Settings.OperationsRepositoryServiceClientSettings _operationsRepositoryServiceSettings;
        private readonly ILog _log;
        private readonly ServiceCollection _services;

        public JobModule(
            MatchingEngineSettings meSettings,
            AssetsSettings assetsSettings,
            ChaosSettings chaosSettings,
            Settings.OperationsRepositoryServiceClientSettings operationsRepositoryServiceSettings,
            ILog log)
        {
            _meSettings = meSettings;
            _assetsSettings = assetsSettings;
            _chaosSettings = chaosSettings;
            _operationsRepositoryServiceSettings = operationsRepositoryServiceSettings;
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

            builder.RegisterOperationsRepositoryClients(new OperationsRepositoryServiceClientSettings
            {
                ServiceUrl = _operationsRepositoryServiceSettings.ServiceUrl,
                RequestTimeout = _operationsRepositoryServiceSettings.RequestTimeout
            }, _log);

            RegisterMatchingEngine(builder);

            builder.RegisterChaosKitty(_chaosSettings);

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
