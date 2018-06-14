using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using Lykke.Job.BlockchainCashinDetector.Tests.Integration.Common;
using Lykke.Job.BlockchainCashinDetector.Tests.Integration.Modules;
using Lykke.Service.BlockchainApi.Client.Models;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Assets;
using Lykke.Service.BlockchainApi.Contract.Balances;
using Moq;
using Xunit;

namespace Lykke.Job.BlockchainCashinDetector.Tests.Integration
{
    /// <summary>
    /// To run locally, add environment.json:
    /// 
    /// {
    /// "SettingsUrl": "http://settings.lykke-settings.svc.cluster.local/[secret-token]_BlockchainCashinDetector-IntegrationTests"
    /// }
    /// </summary>
    public class DepositBalanceDeduplicationTests : IDisposable
    {
        private enum TestState
        {
            FirstDepoist
        }

        private IContainer Container { get; }

        private IntegrationTestsMocks Mocks { get; }

        public DepositBalanceDeduplicationTests()
        {
            var log = new LogToConsole();

            var containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterModule(new IntegrationTestsModule(log));

            // TODO: Should be in-memory implementations
            //containerBuilder.RegisterModule(new RepositoriesModule(
            //    ConstantReloadingManager.From(new DbSettings
            //    {
            //        DataConnString = testsSettings.AzureStorageConnectionString
            //    }),
            //    log));
            //containerBuilder.RegisterModule(new CqrsModule(
            //    new CqrsSettings
            //    {
            //        RabbitConnectionString = testsSettings.RabbitMqConnectionString,
            //        RetryDelay = TimeSpan.FromSeconds(10)
            //    },
            //    log,
            //    rabbitMqVirtualHost: $"IntegrationTests-{Environment.MachineName}"));

            Container = containerBuilder.Build();

            Mocks = Container.Resolve<IntegrationTestsMocks>();

            // Setup mocks

            Mocks.LiteCoinApiClient
                .Setup(m => m.GetAllAssetsAsync(It.IsAny<int>()))
                .Returns<int>(batchSize => Task.FromResult<IReadOnlyDictionary<string, BlockchainAsset>>(
                    new Dictionary<string, BlockchainAsset>
                    {
                        {
                            Constants.Assets.Ltc.Id,
                            new BlockchainAsset(new AssetContract
                            {
                                AssetId = Constants.Assets.Ltc.Id,
                                Accuracy = Constants.Assets.Ltc.Accuracy,
                                Name = "LiteCoin"
                            })
                        }
                    }));
        }

        [Fact(Skip = "Not implemented yet")]
        public void Test()
        {
            const string depositWallet = "LTC_DW_1";
            var state = TestState.FirstDepoist;

            // Setup deposit 1: 10 LTC -> DW_1 mined in block 1000
            Mocks.LiteCoinApiClient
                .Setup(m => m.EnumerateWalletBalanceBatchesAsync(
                    It.IsAny<int>(),
                    It.IsNotNull<Func<string, int>>(),
                    It.IsNotNull<Func<IReadOnlyList<WalletBalance>, Task<bool>>>()))
                .Returns<int, Func<string, int>, Func<IReadOnlyList<WalletBalance>, Task<bool>>>(
                    async (batchSize, assetAccuracyProvider, enumerationCallback) =>
                    {
                        if (state == TestState.FirstDepoist)
                        {
                            await enumerationCallback.Invoke(new[]
                            {
                                new WalletBalance(new WalletBalanceContract
                                    {
                                        Address = depositWallet,
                                        AssetId = Constants.Assets.Ltc.Id,
                                        Balance = Conversions.CoinsToContract(1, Constants.Assets.Ltc.Accuracy),
                                        Block = 1000
                                    },
                                    Constants.Assets.Ltc.Accuracy)
                            });
                        }

                        return new EnumerationStatistics(1, 1, TimeSpan.Zero);
                    });


        }

        public void Dispose()
        {
            Container?.Dispose();
        }
    }
}
