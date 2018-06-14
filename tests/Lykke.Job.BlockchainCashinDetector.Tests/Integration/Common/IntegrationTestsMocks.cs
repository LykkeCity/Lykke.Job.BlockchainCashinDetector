using Lykke.MatchingEngine.Connector.Abstractions.Services;
using Lykke.Service.Assets.Client;
using Lykke.Service.BlockchainApi.Client;
using Lykke.Service.BlockchainWallets.Client;
using Lykke.Service.OperationsRepository.Client.Abstractions.CashOperations;
using Moq;

namespace Lykke.Job.BlockchainCashinDetector.Tests.Integration.Common
{
    public class IntegrationTestsMocks
    {
        public Mock<IBlockchainWalletsClient> WalletsClient { get; } = new Mock<IBlockchainWalletsClient>();
        public Mock<IAssetsServiceWithCache> AssetsClientWithCache { get; } = new Mock<IAssetsServiceWithCache>();
        public Mock<ICashOperationsRepositoryClient> CashOperationsRepositoryClient { get; } = new Mock<ICashOperationsRepositoryClient>();
        public Mock<IMatchingEngineClient> MatchingEngineClient { get; } = new Mock<IMatchingEngineClient>();
        public Mock<IBlockchainApiClient> LiteCoinApiClient { get; } = new Mock<IBlockchainApiClient>();
    }
}
