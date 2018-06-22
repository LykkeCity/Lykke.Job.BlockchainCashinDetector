using System.Collections.Generic;
using System.Linq;
using Autofac;
using Common.Log;
using Lykke.Job.BlockchainCashinDetector.Core.Services.BLockchains;
using Lykke.Job.BlockchainCashinDetector.Services.Blockchains;
using Lykke.Job.BlockchainCashinDetector.Settings;
using Lykke.Job.BlockchainCashinDetector.Settings.Blockchain;
using Lykke.Job.BlockchainCashinDetector.Settings.JobSettings;
using Lykke.Job.BlockchainCashinDetector.Workflow.PeriodicalHandlers;
using Lykke.Service.BlockchainApi.Client;
using Lykke.Service.BlockchainWallets.Client;

namespace Lykke.Job.BlockchainCashinDetector.Modules
{
    public class BlockchainsModule : Module
    {
        private readonly BlockchainCashinDetectorSettings _settings;
        private readonly BlockchainsIntegrationSettings _blockchainsIntegrationSettings;
        private readonly BlockchainWalletsServiceClientSettings _walletsServiceSettings;
        private readonly ILog _log;

        public BlockchainsModule(
            BlockchainCashinDetectorSettings settings,
            BlockchainsIntegrationSettings blockchainsIntegrationSettings,
            BlockchainWalletsServiceClientSettings walletsServiceSettings,
            ILog log)
        {
            _settings = settings;
            _blockchainsIntegrationSettings = blockchainsIntegrationSettings;
            _walletsServiceSettings = walletsServiceSettings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<HotWalletsProvider>()
                .As<IHotWalletsProvider>()
                .WithParameter(TypedParameter.From<IReadOnlyDictionary<string, string>>(_blockchainsIntegrationSettings.Blockchains.ToDictionary(b => b.Type, b => b.HotWalletAddress)))
                .SingleInstance();

            builder.RegisterType<BlockchainApiClientProvider>()
                .As<IBlockchainApiClientProvider>();

            builder.RegisterType<BlockchainWalletsClient>()
                .As<IBlockchainWalletsClient>()
                .WithParameter(TypedParameter.From(_walletsServiceSettings.ServiceUrl))
                .SingleInstance();

            foreach (var blockchain in _blockchainsIntegrationSettings.Blockchains.Where(b => !b.IsDisabled))
            {
                _log.WriteInfo("Blockchains registration", "", 
                    $"Registering blockchain: {blockchain.Type} -> \r\nAPI: {blockchain.ApiUrl}\r\nHW: {blockchain.HotWalletAddress}");

                builder.RegisterType<BlockchainApiClient>()
                    .Named<IBlockchainApiClient>(blockchain.Type)
                    .WithParameter(TypedParameter.From(blockchain.ApiUrl))
                    .SingleInstance();

                if (blockchain.AreCashinsDisabled)
                {
                    _log.WriteWarning("Blockchains registration", "", $"Cashins for blockchain {blockchain.Type} are disabled");
                }
                else
                {
                    builder.RegisterType<DepositWalletsBalanceProcessingPeriodicalHandler>()
                        .As<IStartable>()
                        .AutoActivate()
                        .SingleInstance()
                        .WithParameter(TypedParameter.From(_settings.Monitoring.Period))
                        .WithParameter(TypedParameter.From(_settings.Requests.BatchSize))
                        .WithParameter(TypedParameter.From(blockchain.Type));
                }
            }
        }
    }
}
