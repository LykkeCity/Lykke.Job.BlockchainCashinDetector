using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.Common.Chaos;
using Lykke.Job.BlockchainCashinDetector.Core.Services;
using Lykke.Job.BlockchainCashinDetector.Core.Services.LykkePay;
using Lykke.Job.BlockchainCashinDetector.Services;
using Lykke.Job.BlockchainCashinDetector.Services.LykkePay;
using Lykke.Job.BlockchainCashinDetector.Settings;
using Lykke.Job.BlockchainCashinDetector.Settings.Assets;
using Lykke.Job.BlockchainCashinDetector.Settings.MeSettings;
using Lykke.MatchingEngine.Connector.Services;
using Lykke.Service.Assets.Client;
using Lykke.Service.OperationsRepository.Client;
using Lykke.SettingsReader;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Job.BlockchainCashinDetector.Modules
{
    public class LykkePayModule : Module
    {
        private readonly PayInternalServiceClientSettings _settings;

        public LykkePayModule(PayInternalServiceClientSettings settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var payInternalSettings = new Lykke.Service.PayInternal.Client.PayInternalServiceClientSettings()
            {
                ServiceUrl = _settings.ServiceUrl
            };

            builder.RegisterType<Lykke.Service.PayInternal.Client.PayInternalClient>()
                .As<Lykke.Service.PayInternal.Client.IPayInternalClient>()
                .WithParameter("settings", payInternalSettings)
                .SingleInstance();
        }
    }
}
