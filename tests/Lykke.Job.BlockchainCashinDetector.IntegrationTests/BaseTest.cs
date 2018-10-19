using Autofac;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.Job.BlockchainCashinDetector.Core.Services.BLockchains;
using Lykke.Job.BlockchainCashinDetector.IntegrationTests.Modules;
using Lykke.Job.BlockchainCashinDetector.IntegrationTests.Utils;
using Lykke.Job.BlockchainCashinDetector.Modules;
using Lykke.Job.BlockchainCashinDetector.Settings;
using Lykke.Job.BlockchainCashinDetector.Workflow.PeriodicalHandlers;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeConsole;
using Lykke.Service.Assets.Client;
using Lykke.Service.BlockchainApi.Client;
using Lykke.Service.BlockchainApi.Client.Models;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Balances;
using Lykke.Service.BlockchainWallets.Client;
using Lykke.SettingsReader;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac.Extensions.DependencyInjection;
using Lykke.Job.BlockchainCashinDetector.Settings.JobSettings;
using Lykke.Logs.Loggers.LykkeSlack;
using Microsoft.Extensions.DependencyInjection;
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
            var services = new ServiceCollection();
            _configuration = configBuilder.Build();

            var builder = new ContainerBuilder();
            var appSettings = _configuration.LoadSettings<AppSettings>(options =>
            {
                options.SetConnString(x => x.SlackNotifications.AzureQueue.ConnectionString);
                options.SetQueueName(x => x.SlackNotifications.AzureQueue.QueueName);
                options.SenderName = "Lykke.Job.BlockchainCashinDetector";
            });

            var slackSettings = appSettings.Nested(x => x.SlackNotifications);
            services.AddLykkeLogging(
                appSettings.ConnectionString(x => x.BlockchainCashinDetectorJob.Db.LogsConnString),
                "BlockchainCashinDetectorLog",
                slackSettings.CurrentValue.AzureQueue.ConnectionString,
                slackSettings.CurrentValue.AzureQueue.QueueName,
                logging =>
                {
                    logging.AddAdditionalSlackChannel("CommonBlockChainIntegration");
                    logging.AddAdditionalSlackChannel("CommonBlockChainIntegrationImportantMessages", options =>
                    {
                        options.MinLogLevel = Microsoft.Extensions.Logging.LogLevel.Warning;
                    });
                }
            );

            builder.Populate(services);

            //builder.RegisterInstance(LogFactory.Create().AddUnbufferedConsole())
            //    .As<ILogFactory>()
            //    .As<LogFactory>()
            //    .SingleInstance();

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
            builder.RegisterModule(new CqrsModule(
                //new CqrsSettings()
                //{
                //    RabbitConnectionString = "amqp://guest:guest@0.0.0.0:5672",
                //    RetryDelay = TimeSpan.FromMinutes(2)
                //}));
                appSettings.CurrentValue.BlockchainCashinDetectorJob.Cqrs, "vhost3"));

            _testContainer = builder.Build();
        }

        [Fact]
        public async Task Test1()
        {
            Mock<IBlockchainApiClient> apiClient = new Mock<IBlockchainApiClient>();
            apiClient.Setup(x =>
                x.EnumerateWalletBalanceBatchesAsync(
                    It.IsAny<int>(),
                    It.IsAny<Func<string, int>>(),
                    It.IsAny<Func<IReadOnlyList<WalletBalance>, Task<bool>>>()))
                .ReturnsAsync<int, Func<string, int>, 
                    Func<IReadOnlyList<WalletBalance>, Task<bool>>, 
                    IBlockchainApiClient, EnumerationStatistics>
                (
                    (batchSize, accuracyProvider, enumerationCallback) =>
                    {
                        enumerationCallback(new List<WalletBalance>
                        {
                            new WalletBalance
                            (
                                new WalletBalanceContract
                                {
                                    Address = "0x974542bd37b57a2a5151d0ef1619e3e3d150ce47",
                                    AssetId = "ETC",
                                    Balance = Conversions.CoinsToContract(1, 6),
                                    Block = 3000000
                                },
                                assetAccuracy: 6
                            )
                        }).GetAwaiter().GetResult();

                        return new EnumerationStatistics(1, 1, TimeSpan.FromMilliseconds(1));
                    }
                );
            //)};

            var cqrsEngine = _testContainer.Resolve<ICqrsEngine>();
            var assets = (await _testContainer.Resolve<IAssetsServiceWithCache>().GetAllAssetsAsync(false))
                .Where(a => a.BlockchainIntegrationLayerId == "EthereumClassic")
                .ToDictionary(
                    a => a.BlockchainIntegrationLayerAssetId,
                    a => a);
            var blockchainAssets = await _testContainer.Resolve<IBlockchainApiClientProvider>().
                Get("EthereumClassic")
                .GetAllAssetsAsync(50);

            BalanceProcessor processor = new BalanceProcessor("EthereumClassic",
                _testContainer.Resolve<ILogFactory>(),
                _testContainer.Resolve<IHotWalletsProvider>(),
                apiClient.Object,
                cqrsEngine,
                _testContainer.Resolve<IEnrolledBalanceRepository>(),
                assets,
                blockchainAssets
                );

            var factory1 = _testContainer.Resolve<ILogFactory>();
            Task.Delay(10000).Wait();
            cqrsEngine.Start();

            await processor.ProcessAsync(100);

            Task.Delay(600000).Wait();
        }
    }

    #region Mock

    #endregion
}
