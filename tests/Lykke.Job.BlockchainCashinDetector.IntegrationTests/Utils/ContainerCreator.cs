using Autofac;
using Autofac.Core;
using Lykke.Common.Log;
using Lykke.Job.BlockchainCashinDetector.IntegrationTests.Modules;
using Lykke.Job.BlockchainCashinDetector.Modules;
using Lykke.Job.BlockchainCashinDetector.Settings;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeConsole;
using Lykke.SettingsReader;
using Microsoft.Extensions.Configuration;

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

            builder.RegisterModule(new JobModule(appSettings));
            builder.RegisterModule(new RepositoriesModule(appSettings));
            builder.RegisterModule(new BlockchainsModule(appSettings));
            builder.RegisterModule(new CqrsTestModule(
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
                return builder.Build();
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
