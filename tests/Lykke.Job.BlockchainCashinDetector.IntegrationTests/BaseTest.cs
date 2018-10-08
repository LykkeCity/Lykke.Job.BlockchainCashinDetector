using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Lykke.Common.Log;
using Lykke.Job.BlockchainCashinDetector.IntegrationTests.Modules;
using Lykke.Job.BlockchainCashinDetector.IntegrationTests.Utils;
using Lykke.Job.BlockchainCashinDetector.Modules;
using Lykke.Job.BlockchainCashinDetector.Settings;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeConsole;
using Lykke.SettingsReader;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Lykke.Job.BlockchainCashinDetector.IntegrationTests
{
    public class BaseTest
    {
        private readonly LaunchSettingsFixture _fixture;
        private readonly IConfigurationRoot _configuration;
        private readonly IContainer _testContainer;

        public BaseTest()
        {
            _fixture = new LaunchSettingsFixture();
            var configBuilder = new ConfigurationBuilder().AddEnvironmentVariables();

            _configuration = configBuilder.Build();

            var builder = new ContainerBuilder();
            var appSettings = _configuration.LoadSettings<AppSettings>(options =>
            {
                options.SetConnString(x => x.SlackNotifications.AzureQueue.ConnectionString);
                options.SetQueueName(x => x.SlackNotifications.AzureQueue.QueueName);
                options.SenderName = "Lykke.Job.BlockchainCashinDetector";
            });

            var slackSettings = appSettings.Nested(x => x.SlackNotifications);
            

            builder.RegisterInstance(LogFactory.Create().AddConsole())
                .As<ILogFactory>()
                .SingleInstance();

            builder.RegisterModule(new JobModule(
                appSettings.CurrentValue.MatchingEngineClient,
                appSettings.CurrentValue.Assets,
                appSettings.CurrentValue.BlockchainCashinDetectorJob.ChaosKitty,
                appSettings.CurrentValue.OperationsRepositoryServiceClient));
            builder.RegisterModule(new RepositoriesModule(
                appSettings.Nested(x => x.BlockchainCashinDetectorJob.Db)));
            builder.RegisterModule(new BlockchainsModule(
                appSettings.CurrentValue.BlockchainCashinDetectorJob,
                appSettings.CurrentValue.BlockchainsIntegration,
                appSettings.CurrentValue.BlockchainWalletsServiceClient));
            builder.RegisterModule(new CqrsTestModule(
                appSettings.CurrentValue.BlockchainCashinDetectorJob.Cqrs));

            _testContainer = builder.Build();
        }

        [Fact]
        public void Test1()
        {

        }
    }
}
