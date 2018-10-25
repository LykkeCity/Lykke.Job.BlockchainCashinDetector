using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using Autofac.Core;
using Autofac.Extensions.DependencyInjection;
using Lykke.Common.Log;
using Lykke.Job.BlockchainCashinDetector.IntegrationTests.Modules;
using Lykke.Job.BlockchainCashinDetector.Modules;
using Lykke.Job.BlockchainCashinDetector.Settings;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeConsole;
using Lykke.SettingsReader;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Job.BlockchainCashinDetector.IntegrationTests.Utils
{
    public static class ContainerCreator
    {
        public static IContainer CreateIntegrationContainer()
        {
            var builder = new ContainerBuilder();
            var appSettings = LoadAppSettings();

            builder.RegisterInstance(LogFactory.Create().AddUnbufferedConsole())
                .As<ILogFactory>()
                .As<LogFactory>()
                .SingleInstance();

            builder.RegisterModule(new JobModule(
                appSettings.CurrentValue.MatchingEngineClient,
                appSettings.CurrentValue.Assets,
                appSettings.CurrentValue.BlockchainCashinDetectorJob.ChaosKitty));
            builder.RegisterModule(new RepositoriesModule(
                appSettings.Nested(x => x.BlockchainCashinDetectorJob.Db)));
            builder.RegisterModule(new BlockchainsModule(
                appSettings.CurrentValue.BlockchainCashinDetectorJob,
                appSettings.CurrentValue.BlockchainsIntegration,
                appSettings.CurrentValue.BlockchainWalletsServiceClient));
            builder.RegisterModule(new CqrsTestModule(
                //new CqrsSettings()
                //{
                //    RabbitConnectionString = "amqp://guest:guest@0.0.0.0:5672",
                //    RetryDelay = TimeSpan.FromMinutes(2)
                //}));
                appSettings.CurrentValue.BlockchainCashinDetectorJob.Cqrs, "test"));

            var testContainer = builder.Build();

            return testContainer;
        }

        public static IReloadingManager<AppSettings> LoadAppSettings()
        {
            var fixture = new LaunchSettingsFixture();
            var configBuilder = new ConfigurationBuilder().AddEnvironmentVariables();
            var configuration = configBuilder.Build();
            var appSettings = configuration.LoadSettings<AppSettings>(options =>
            {
                options.SetConnString(x => x.SlackNotifications.AzureQueue.ConnectionString);
                options.SetQueueName(x => x.SlackNotifications.AzureQueue.QueueName);
                options.SenderName = "Lykke.Job.BlockchainCashinDetector";
            });

            return appSettings;
        }

        public static IContainer CreateContainer(params IModule[] modules)
        {
            var builder = new ContainerBuilder();

            builder.RegisterInstance(LogFactory.Create().AddUnbufferedConsole())
                .As<ILogFactory>()
                .As<LogFactory>()
                .SingleInstance();

            if (modules != null && modules.Length == 0)
            {
                return null;
            }

            foreach (var module in modules)
            {
                builder.RegisterModule(module);
            }

            var testContainer = builder.Build();

            return testContainer;
        }
    }
}
