using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Contract;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.Job.BlockchainCashinDetector.Core.Services.BLockchains;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Lykke.Job.BlockchainCashinDetector.Workflow.PeriodicalHandlers;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.BlockchainApi.Client;
using Lykke.Service.BlockchainApi.Client.Models;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Assets;
using Lykke.Service.BlockchainApi.Contract.Balances;
using Moq;
using Xunit;

namespace Lykke.Job.BlockchainCashinDetector.Tests
{
    /// <summary>
    /// LWDEV-8995
    /// 1. Deposit 100 is detected on DW at block 5000
    /// 2. Balance processor has detected this balance, published EnrollToMatchingEngineCommand with balance 100,
    ///     but failed to save aggregate state due to Azure Storage unavailability here -
    ///     https://github.com/LykkeCity/Lykke.Job.BlockchainCashinDetector/blob/895c9d879e59af5c1312ef5f09f8e76a97607679/src/Lykke.Job.BlockchainCashinDetector/Workflow/PeriodicalHandlers/BalanceProcessor.cs#L173
    /// 3. One more deposit has detected and DW balance is changed to 300 at block 5001
    /// 4. Balance processor has detected balance 300, publish EnrollToMatchingEngineCommand with balance 300 and saved aggregate.
    /// 5. First enrollement command is processed and 100 is enrolled to ME
    /// 6. Second enrollement command with amount = 300 is processed but it's deduplicated by ME
    /// 7. Finally aggregate contains ME amount = 300, but actually only 100 is enrolled.
    /// </summary>
    public class InvalidDepositAmounEnrolledToMeTests
    {
        [Fact(Skip = "Should be update after refactoring")]
        public async Task InvalidDepositAmounEnrolledToMeTest()
        {
            var blockchainType = "Stellar";
            var hotWallet = "hot-wallet";
            var depositWallet = "deposit-wallet";
            var operationId = Guid.NewGuid();
            var hotWalletProviderMock = new Mock<IHotWalletsProvider>();
            var blockchainApiClientMock = new Mock<IBlockchainApiClient>();
            var cqrsEngineMock = new Mock<ICqrsEngine>();
            var enrolledBalanceRepositoryMock = new Mock<IEnrolledBalanceRepository>();
            var cashinRepositoryMock = new Mock<ICashinRepository>();
            var depositWalletLockRepository = new Mock<IDepositWalletLockRepository>();
            var chaosKittyMock = new Mock<IChaosKitty>();
            var xlmBlockchainAsset = new BlockchainAsset
            (
                new AssetContract
                {
                    AssetId = "XLM",
                    Accuracy = 7,
                    Name = "Stellar XLM"
                }
            );
            var blockchainAssets = new Dictionary<string, BlockchainAsset>
            {
                { xlmBlockchainAsset.AssetId, xlmBlockchainAsset }
            };
            var xlmAsset = new Asset
            {
                Id = "XLM-asset",
                BlockchainIntegrationLayerAssetId = xlmBlockchainAsset.AssetId,
                BlockchainIntegrationLayerId = blockchainType,
                CashinMinimalAmount = 1
            };
            var assets = new Dictionary<string, Asset>
            {
                {xlmAsset.BlockchainIntegrationLayerAssetId, xlmAsset}
            };

            hotWalletProviderMock
                .Setup(x => x.GetHotWalletAddress(It.Is<string>(b => b == blockchainType)))
                .Returns(hotWallet);

            cashinRepositoryMock
                .Setup(x => x.GetOrAddAsync
                (
                    It.Is<string>(b => b == blockchainType),
                    It.Is<string>(d => d == depositWallet),
                    It.Is<string>(a => a == xlmBlockchainAsset.AssetId),
                    It.Is<Guid>(o => o == operationId),
                    It.IsAny<Func<CashinAggregate>>()
                ))
                .ReturnsAsync(() => CashinAggregate.StartWaitingForActualBalance
                (
                    operationId,
                    xlmAsset.Id,
                    xlmBlockchainAsset.Accuracy,
                    xlmBlockchainAsset.AssetId,
                    blockchainType,
                    (decimal) xlmAsset.CashinMinimalAmount,
                    depositWallet,
                    hotWallet
                ));
            
            var balanceProcessor = new BalanceProcessor(
                blockchainType,
                EmptyLog.Instance,
                hotWalletProviderMock.Object,
                blockchainApiClientMock.Object,
                cqrsEngineMock.Object,
                enrolledBalanceRepositoryMock.Object,
                assets,
                blockchainAssets);

            // 1. Deposit 100 is detected on DW at block 5000
            // 2. Balance processor has detected this balance, published EnrollToMatchingEngineCommand with balance 100,
            //     but failed to save aggregate state due to Azure Storage unavailability here -
            //     https://github.com/LykkeCity/Lykke.Job.BlockchainCashinDetector/blob/895c9d879e59af5c1312ef5f09f8e76a97607679/src/Lykke.Job.BlockchainCashinDetector/Workflow/PeriodicalHandlers/BalanceProcessor.cs#L173
            // In current implementation this turns to the publicshing LockDepositWalletCommand
            
            // Arrange

            depositWalletLockRepository
                .Setup(x => x.LockAsync
                (
                    It.Is<DepositWalletKey>(k =>
                        k.DepositWalletAddress == depositWallet
                        && k.BlockchainType == blockchainType
                        && k.BlockchainAssetId == xlmBlockchainAsset.AssetId),
                    It.Is<decimal>(b => b == 100),
                    It.Is<long>(d => d == 5000),
                    It.IsAny<Func<Guid>>()
                ))
                .ReturnsAsync<DepositWalletKey, decimal, long, Func<Guid>, IDepositWalletLockRepository, DepositWalletLock>
                (
                    (key, balance, block, newOperationIdFactory) =>
                        DepositWalletLock.Create
                        (
                            key,
                            operationId,
                            100,
                            5000
                        )
                );

            blockchainApiClientMock
                .Setup(x => x.EnumerateWalletBalanceBatchesAsync
                (
                    It.IsAny<int>(),
                    It.IsAny<Func<string, int>>(),
                    It.IsAny<Func<IReadOnlyList<WalletBalance>, Task<bool>>>()
                ))
                .ReturnsAsync<int, Func<string, int>, Func<IReadOnlyList<WalletBalance>, Task<bool>>, IBlockchainApiClient, EnumerationStatistics>
                (
                    (batchSize, accuracyProvider, enumerationCallback) =>
                    {
                        enumerationCallback(new List<WalletBalance>
                        {
                            new WalletBalance
                            (
                                new WalletBalanceContract
                                {
                                    Address = depositWallet,
                                    AssetId = xlmBlockchainAsset.AssetId,
                                    Balance = Conversions.CoinsToContract(100, xlmBlockchainAsset.Accuracy),
                                    Block = 5000
                                },
                                assetAccuracy: xlmBlockchainAsset.Accuracy
                            )
                        }).GetAwaiter().GetResult();

                        return new EnumerationStatistics(1, 1, TimeSpan.FromMilliseconds(1));
                    }
                );

            cashinRepositoryMock
                .Setup(x => x.SaveAsync(It.IsAny<CashinAggregate>()))
                .Throws<CashinAggregatePersistingFailureTestException>();

            // Act / Assert

            await Assert.ThrowsAsync<CashinAggregatePersistingFailureTestException>(async () =>
            {
                await balanceProcessor.ProcessAsync(100);
            });

            depositWalletLockRepository.Verify(x => x.LockAsync
            (
                It.Is<DepositWalletKey>(k =>
                    k.DepositWalletAddress == depositWallet
                    && k.BlockchainType == blockchainType
                    && k.BlockchainAssetId == xlmBlockchainAsset.AssetId),
                It.Is<decimal>(b => b == 100),
                It.Is<long>(d => d == 5000),
                It.IsAny<Func<Guid>>()
            ));

            cqrsEngineMock.Verify(
                x => x.SendCommand
                (
                    It.Is<EnrollToMatchingEngineCommand>(c => 
                        c.DepositWalletAddress == depositWallet && 
                        c.AssetId == xlmAsset.Id && 
                        c.BlockchainType == blockchainType &&
                        c.BlockchainAssetId == xlmBlockchainAsset.AssetId &&
                        c.OperationId == operationId &&
                        // ReSharper disable once CompareOfFloatsByEqualityOperator
                        c.MatchingEngineOperationAmount == 100.0d),
                    It.Is<string>(c => c == BlockchainCashinDetectorBoundedContext.Name),
                    It.Is<string>(c => c == BlockchainCashinDetectorBoundedContext.Name),
                    It.IsAny<uint>()
                ),
                Times.Once);

            // The bug is:
            // 3. One more deposit has detected and DW balance is changed to 300 at block 5001
            // 4. Balance processor has detected balance 300, publish EnrollToMatchingEngineCommand with balance 300 and saved aggregate.
            // 5. First enrollement command is processed and 100 is enrolled to ME
            // 6. Second enrollement command with amount = 300 is processed but it's deduplicated by ME
            // 7. Finally aggregate contains ME amount = 300, but actually only 100 is enrolled
            // Should be:
            // There should be the same balance 100 at block 5000 as in first iteration

            // Arrange

            depositWalletLockRepository
                .Setup(x => x.LockAsync
                (
                    It.Is<DepositWalletKey>(k =>
                        k.DepositWalletAddress == depositWallet
                        && k.BlockchainType == blockchainType
                        && k.BlockchainAssetId == xlmBlockchainAsset.AssetId),
                    It.Is<decimal>(b => b == 300),
                    It.Is<long>(d => d == 5001),
                    It.IsAny<Func<Guid>>()
                ))
                .ReturnsAsync<DepositWalletKey, decimal, long, Func<Guid>, IDepositWalletLockRepository, DepositWalletLock>
                (
                    (key, balance, block, newOperationIdFactory) =>
                        DepositWalletLock.Create
                        (
                            key,
                            operationId,
                            100,
                            5000
                        )
                );

            blockchainApiClientMock
                .Setup(x => x.EnumerateWalletBalanceBatchesAsync
                (
                    It.IsAny<int>(),
                    It.IsAny<Func<string, int>>(),
                    It.IsAny<Func<IReadOnlyList<WalletBalance>, Task<bool>>>()
                ))
                .ReturnsAsync<int, Func<string, int>, Func<IReadOnlyList<WalletBalance>, Task<bool>>, IBlockchainApiClient, EnumerationStatistics>
                (
                    (batchSize, accuracyProvider, enumerationCallback) =>
                    {
                        enumerationCallback(new List<WalletBalance>
                        {
                            new WalletBalance
                            (
                                new WalletBalanceContract
                                {
                                    Address = depositWallet,
                                    AssetId = xlmBlockchainAsset.AssetId,
                                    Balance = Conversions.CoinsToContract(300, xlmBlockchainAsset.Accuracy),
                                    Block = 5001
                                },
                                assetAccuracy: xlmBlockchainAsset.Accuracy
                            )
                        }).GetAwaiter().GetResult();

                        return new EnumerationStatistics(1, 1, TimeSpan.FromMilliseconds(1));
                    }
                );

            cashinRepositoryMock
                .Setup(x => x.SaveAsync(It.IsAny<CashinAggregate>()))
                .Returns(Task.CompletedTask);

            depositWalletLockRepository.ResetCalls();
            cqrsEngineMock.ResetCalls();
            cashinRepositoryMock.ResetCalls();

            // Act

            await balanceProcessor.ProcessAsync(100);

            // Verify

            depositWalletLockRepository.Verify(
                x => x.LockAsync
                (
                    It.Is<DepositWalletKey>(k =>
                        k.DepositWalletAddress == depositWallet
                        && k.BlockchainType == blockchainType
                        && k.BlockchainAssetId == xlmBlockchainAsset.AssetId),
                    It.Is<decimal>(b => b == 300),
                    It.Is<long>(d => d == 5001),
                    It.IsAny<Func<Guid>>()
                ),
                Times.Once);

            cqrsEngineMock.Verify(
                x => x.SendCommand
                (
                    It.Is<EnrollToMatchingEngineCommand>(c => 
                        c.DepositWalletAddress == depositWallet && 
                        c.AssetId == xlmAsset.Id && 
                        c.BlockchainType == blockchainType &&
                        c.BlockchainAssetId == xlmBlockchainAsset.AssetId &&
                        c.OperationId == operationId &&
                        // ReSharper disable once CompareOfFloatsByEqualityOperator
                        c.MatchingEngineOperationAmount == 100.0d),
                    It.Is<string>(c => c == BlockchainCashinDetectorBoundedContext.Name),
                    It.Is<string>(c => c == BlockchainCashinDetectorBoundedContext.Name),
                    It.IsAny<uint>()
                ),
                Times.Once);


            cashinRepositoryMock.Verify(x => x.SaveAsync
            (
                It.Is<CashinAggregate>(a =>
                    a.OperationAmount == 100
                    && a.MeAmount == 100
                    && a.BalanceAmount == 100
                    && a.BalanceBlock == 5000
                    && a.DepositWalletAddress == depositWallet
                    && a.HotWalletAddress == hotWallet
                    && a.OperationId == operationId
                    && a.AssetAccuracy == xlmBlockchainAsset.Accuracy
                    && a.AssetId == xlmAsset.Id
                    && a.BlockchainAssetId == xlmBlockchainAsset.AssetId
                    && a.BlockchainType == blockchainType
                    && a.CashinMinimalAmount == (decimal) xlmAsset.CashinMinimalAmount
                    && a.State == CashinState.Started
                )
            ));
        }
    }
}
