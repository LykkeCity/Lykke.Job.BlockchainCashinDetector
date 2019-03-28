using Autofac;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.Job.BlockchainCashinDetector.Core.Services.BLockchains;
using Lykke.Job.BlockchainCashinDetector.IntegrationTests.Modules;
using Lykke.Job.BlockchainCashinDetector.IntegrationTests.Utils;
using Lykke.Job.BlockchainCashinDetector.Modules;
using Lykke.Job.BlockchainCashinDetector.Workflow.PeriodicalHandlers;
using Lykke.Service.Assets.Client;
using Lykke.Service.BlockchainApi.Client;
using Lykke.Service.BlockchainApi.Client.Models;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Balances;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac.Core;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Lykke.Job.BlockchainCashinDetector.Workflow.Events;
using Xunit;

namespace Lykke.Job.BlockchainCashinDetector.IntegrationTests
{
    public class BaseTest
    {
        public BaseTest()
        {
        }

        [Fact(Skip = "Should be update after refactoring")]
        public async Task BalanceProcessorStarted__BalanceIsGtThanZero_DepositWalletLockedEventSent()
        {
            var testContainer = ContainerCreator.CreateIntegrationContainer();
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

            var cqrsEngine = testContainer.Resolve<ICqrsEngine>();
            var assets = (await testContainer.Resolve<IAssetsServiceWithCache>().GetAllAssetsAsync(false))
                .Where(a => a.BlockchainIntegrationLayerId == "EthereumClassic")
                .ToDictionary(
                    a => a.BlockchainIntegrationLayerAssetId,
                    a => a);
            var blockchainAssets = await testContainer.Resolve<IBlockchainApiClientProvider>().
                Get("EthereumClassic")
                .GetAllAssetsAsync(50);

            BalanceProcessor processor = new BalanceProcessor("EthereumClassic",
                testContainer.Resolve<ILogFactory>(),
                testContainer.Resolve<IHotWalletsProvider>(),
                apiClient.Object,
                cqrsEngine,
                testContainer.Resolve<IEnrolledBalanceRepository>(),
                assets,
                blockchainAssets
                );

            cqrsEngine.StartSubscribers();

            await processor.ProcessAsync(100);

            await CqrsTestModule.CommandsInterceptor.WaitForCommandToBeHandledWithTimeoutAsync(typeof(LockDepositWalletCommand),
                TimeSpan.MaxValue);

            await CqrsTestModule.EventsInterceptor.WaitForEventToBeHandledWithTimeoutAsync(typeof(DepositWalletLockedEvent),
                TimeSpan.FromMilliseconds(Int32.MaxValue));

            Task.Delay(600000).Wait();
        }

        internal class RepoMockModule : Module
        {
            private Action<ContainerBuilder> _regAction;

            public RepoMockModule(Action<ContainerBuilder> regAction)
            {
                _regAction = regAction;
            }

            protected override void Load(ContainerBuilder builder)
            {
                _regAction(builder);
            }
        }

        [Fact]
        public async Task DepositWalletLockReleasedEventSent__AggregateIsCashin__NotifyCashinCompletedCommandSent()
        {
            var operationId = Guid.NewGuid();
            var cashinAggregate = CashinAggregate.Restore(operationId,
                "ETC",
                6,
                6,
                10,
                250,
                "ETC",
                "EthereumClassic",
                0,
                DateTime.UtcNow,
                "0x...",
                150,
                250,
                null,
                null,
                null,
                0.05m,
                "0x...",
                DateTime.UtcNow,
                10,
                10,
                DateTime.UtcNow,
                null,
                operationId,
                CashinResult.Success,
                null,
                null,
                10,
                250,
                "0xHASH",
                CashinState.OutdatedBalance,
                false,
                "1.0.0",
                null,
                null);
            var cashinRepoMock = new Mock<ICashinRepository>();
            cashinRepoMock.Setup(x => x.GetAsync(It.IsAny<Guid>())).ReturnsAsync(cashinAggregate);
            var repoModule = new RepoMockModule((builder) =>
            {
                var depositWalletLockRepository = new Mock<IDepositWalletLockRepository>();
                var matchingEngineCallsDeduplicationRepository = new Mock<IMatchingEngineCallsDeduplicationRepository>();
                var enrolledBalanceRepository = new Mock<IEnrolledBalanceRepository>();
                builder.RegisterInstance(cashinRepoMock.Object)
                    .As<ICashinRepository>();

                builder.RegisterInstance(matchingEngineCallsDeduplicationRepository.Object)
                    .As<IMatchingEngineCallsDeduplicationRepository>();

                builder.RegisterInstance(enrolledBalanceRepository.Object)
                    .As<IEnrolledBalanceRepository>();

                builder.RegisterInstance(depositWalletLockRepository.Object)
                    .As<IDepositWalletLockRepository>();
            });
            var dependencies = GetIntegrationDependencies();
            dependencies.Add(repoModule);
            var testContainer = ContainerCreator.CreateContainer(dependencies.ToArray());
            var cashinRepo = testContainer.Resolve<ICashinRepository>();
            var cqrsEngine = testContainer.Resolve<ICqrsEngine>();
            var @event = new DepositWalletLockReleasedEvent()
            {
                OperationId = operationId
            };

            cqrsEngine.StartSubscribers();

            cqrsEngine.PublishEvent(@event, CqrsTestModule.Self);

            await CqrsTestModule.EventsInterceptor.WaitForEventToBeHandledWithTimeoutAsync(
                typeof(DepositWalletLockReleasedEvent),
                TimeSpan.FromMinutes(4));

            await CqrsTestModule.CommandsInterceptor.WaitForCommandToBeHandledWithTimeoutAsync(
                typeof(NotifyCashinCompletedCommand),
                TimeSpan.FromMinutes(4));
        }

        [Fact]
        public async Task DepositWalletLockReleasedEventSent__AggregateIsDustCashin__NotifyCashinCompletedCommandSent()
        {
            var operationId = Guid.NewGuid();
            var cashinAggregate = CashinAggregate.Restore(operationId,
                "ETC",
                6,
                6,
                10,
                250,
                "ETC",
                "EthereumClassic",
                0,
                DateTime.UtcNow,
                "0x...",
                150,
                250,
                null,
                null,
                null,
                0.05m,
                "0x...",
                DateTime.UtcNow,
                10,
                10,
                DateTime.UtcNow,
                null,
                operationId,
                CashinResult.Success,
                null,
                null,
                10,
                250,
                "0xHASH",
                CashinState.OutdatedBalance,
                true,
                "1.0.0",
                null,
                null);
            var cashinRepoMock = new Mock<ICashinRepository>();
            cashinRepoMock.Setup(x => x.GetAsync(It.IsAny<Guid>())).ReturnsAsync(cashinAggregate);
            var repoModule = new RepoMockModule((builder) =>
            {
                var depositWalletLockRepository = new Mock<IDepositWalletLockRepository>();
                var matchingEngineCallsDeduplicationRepository = new Mock<IMatchingEngineCallsDeduplicationRepository>();
                var enrolledBalanceRepository = new Mock<IEnrolledBalanceRepository>();
                builder.RegisterInstance(cashinRepoMock.Object)
                    .As<ICashinRepository>();

                builder.RegisterInstance(matchingEngineCallsDeduplicationRepository.Object)
                    .As<IMatchingEngineCallsDeduplicationRepository>();

                builder.RegisterInstance(enrolledBalanceRepository.Object)
                    .As<IEnrolledBalanceRepository>();

                builder.RegisterInstance(depositWalletLockRepository.Object)
                    .As<IDepositWalletLockRepository>();
            });
            var dependencies = GetIntegrationDependencies();
            dependencies.Add(repoModule);
            var testContainer = ContainerCreator.CreateContainer(dependencies.ToArray());
            var cashinRepo = testContainer.Resolve<ICashinRepository>();
            var cqrsEngine = testContainer.Resolve<ICqrsEngine>();
            var @event = new DepositWalletLockReleasedEvent()
            {
                OperationId = operationId
            };

            cqrsEngine.StartSubscribers();

            cqrsEngine.PublishEvent(@event, CqrsTestModule.Self);

            await CqrsTestModule.EventsInterceptor.WaitForEventToBeHandledWithTimeoutAsync(
                typeof(DepositWalletLockReleasedEvent),
                TimeSpan.FromMinutes(4));

            await CqrsTestModule.CommandsInterceptor.WaitForCommandToBeHandledWithTimeoutAsync(
                typeof(NotifyCashinCompletedCommand),
                TimeSpan.FromMinutes(4));
        }

        [Fact]
        public async Task DepositWalletLockReleasedEventSent__AggregateIsFailedCashin__NotifyCashinFailedCommandSent()
        {
            var operationId = Guid.NewGuid();
            var cashinAggregate = CashinAggregate.Restore(operationId,
                "ETC",
                6,
                6,
                10,
                250,
                "ETC",
                "EthereumClassic",
                0,
                DateTime.UtcNow,
                "0x...",
                150,
                250,
                null,
                null,
                null,
                0.05m,
                "0x...",
                DateTime.UtcNow,
                10,
                10,
                DateTime.UtcNow,
                null,
                operationId,
                CashinResult.Failure,
                null,
                null,
                10,
                250,
                "0xHASH",
                CashinState.OutdatedBalance,
                true,
                "1.0.0",
                CashinErrorCode.Unknown,
                null);
            var cashinRepoMock = new Mock<ICashinRepository>();
            cashinRepoMock.Setup(x => x.GetAsync(It.IsAny<Guid>())).ReturnsAsync(cashinAggregate);
            var repoModule = new RepoMockModule((builder) =>
            {
                var depositWalletLockRepository = new Mock<IDepositWalletLockRepository>();
                var matchingEngineCallsDeduplicationRepository = new Mock<IMatchingEngineCallsDeduplicationRepository>();
                var enrolledBalanceRepository = new Mock<IEnrolledBalanceRepository>();
                builder.RegisterInstance(cashinRepoMock.Object)
                    .As<ICashinRepository>();

                builder.RegisterInstance(matchingEngineCallsDeduplicationRepository.Object)
                    .As<IMatchingEngineCallsDeduplicationRepository>();

                builder.RegisterInstance(enrolledBalanceRepository.Object)
                    .As<IEnrolledBalanceRepository>();

                builder.RegisterInstance(depositWalletLockRepository.Object)
                    .As<IDepositWalletLockRepository>();
            });

            var dependencies = GetIntegrationDependencies();
            dependencies.Add(repoModule);
            var testContainer = ContainerCreator.CreateContainer(
                dependencies.ToArray()
                );
            var cashinRepo = testContainer.Resolve<ICashinRepository>();
            var cqrsEngine = testContainer.Resolve<ICqrsEngine>();
            var @event = new DepositWalletLockReleasedEvent()
            {
                OperationId = operationId
            };

            cqrsEngine.StartSubscribers();

            cqrsEngine.PublishEvent(@event, CqrsTestModule.Self);

            await CqrsTestModule.EventsInterceptor.WaitForEventToBeHandledWithTimeoutAsync(
                typeof(DepositWalletLockReleasedEvent),
                TimeSpan.FromMinutes(4));

            await CqrsTestModule.CommandsInterceptor.WaitForCommandToBeHandledWithTimeoutAsync(
                typeof(NotifyCashinFailedCommand),
                TimeSpan.FromMinutes(4));
        }

        [Fact]
        public async Task DepositWalletLockReleasedEventSent__AggregateIsRejectedCashin__NotifyCashinFailedCommandSent()
        {
            var operationId = Guid.NewGuid();
            var cashinAggregate = CashinAggregate.Restore(operationId,
                "ETC",
                6,
                6,
                10,
                250,
                "ETC",
                "EthereumClassic",
                0,
                DateTime.UtcNow,
                "0x...",
                150,
                250,
                null,
                null,
                null,
                0.05m,
                "0x...",
                DateTime.UtcNow,
                10,
                10,
                DateTime.UtcNow,
                null,
                operationId,
                CashinResult.Unknown,
                null,
                null,
                10,
                250,
                "0xHASH",
                CashinState.ClientRetrieved,
                true,
                "1.0.0",
                CashinErrorCode.Unknown,
                null);
            var cashinRepoMock = new Mock<ICashinRepository>();
            cashinRepoMock.Setup(x => x.GetAsync(It.IsAny<Guid>())).ReturnsAsync(cashinAggregate);
            var repoModule = new RepoMockModule((builder) =>
            {
                var depositWalletLockRepository = new Mock<IDepositWalletLockRepository>();
                var matchingEngineCallsDeduplicationRepository = new Mock<IMatchingEngineCallsDeduplicationRepository>();
                var enrolledBalanceRepository = new Mock<IEnrolledBalanceRepository>();
                builder.RegisterInstance(cashinRepoMock.Object)
                    .As<ICashinRepository>();

                builder.RegisterInstance(matchingEngineCallsDeduplicationRepository.Object)
                    .As<IMatchingEngineCallsDeduplicationRepository>();

                builder.RegisterInstance(enrolledBalanceRepository.Object)
                    .As<IEnrolledBalanceRepository>();

                builder.RegisterInstance(depositWalletLockRepository.Object)
                    .As<IDepositWalletLockRepository>();
            });

            var dependencies = GetIntegrationDependencies();
            dependencies.Add(repoModule);
            var testContainer = ContainerCreator.CreateContainer(
                dependencies.ToArray()
                );
            var cqrsEngine = testContainer.Resolve<ICqrsEngine>();
            var @event = new BlockchainRiskControl.Contract.Events.OperationRejectedEvent()
            {
                OperationId = operationId,
                Message = "Test"
            };

            cqrsEngine.StartSubscribers();

            cqrsEngine.PublishEvent(@event, BlockchainRiskControl.Contract.BlockchainRiskControlBoundedContext.Name);

            await CqrsTestModule.CommandsInterceptor.WaitForCommandToBeHandledWithTimeoutAsync(
                typeof(ReleaseDepositWalletLockCommand),
                TimeSpan.FromMinutes(4));

            await CqrsTestModule.EventsInterceptor.WaitForEventToBeHandledWithTimeoutAsync(
                typeof(DepositWalletLockReleasedEvent),
                TimeSpan.FromMinutes(4));

            await CqrsTestModule.CommandsInterceptor.WaitForCommandToBeHandledWithTimeoutAsync(
                typeof(NotifyCashinFailedCommand),
                TimeSpan.FromMinutes(4));
        }

        private List<IModule> GetIntegrationDependencies()
        {
            var appSettings = ContainerCreator.LoadAppSettings();
            appSettings.CurrentValue.BlockchainCashinDetectorJob.Cqrs.Vhost = "test";

            return new List<IModule>
            {
                new JobModule(appSettings),
                new BlockchainsModule(appSettings),
                new CqrsTestModule(appSettings)
            };
        }
    }
}
