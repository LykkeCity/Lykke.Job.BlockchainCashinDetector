using System.Collections.Generic;
using System.Linq;
using Autofac;
using Common.Log;
using Lykke.Job.BlockchainCashinDetector.Core.Services.BLockchains;
using Lykke.Job.BlockchainCashinDetector.Services.Blockchains;
using Lykke.Job.BlockchainCashinDetector.Settings.Blockchain;
using Lykke.Job.BlockchainCashinDetector.Settings.JobSettings;
using Lykke.Job.BlockchainCashinDetector.Workflow.PeriodicalHandlers;
using Lykke.Service.BlockchainApi.Client;

namespace Lykke.Job.BlockchainCashinDetector.Modules
{
    public class BlockchainsModule : Module
    {
        private readonly BlockchainCashinDetectorSettings _settings;
        private readonly BlockchainsIntegrationSettings _blockchainsIntegrationSettings;
        private readonly ILog _log;

        public BlockchainsModule(
            BlockchainCashinDetectorSettings settings,
            BlockchainsIntegrationSettings blockchainsIntegrationSettings,
            ILog log)
        {
            _settings = settings;
            _blockchainsIntegrationSettings = blockchainsIntegrationSettings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<HotWalletsProvider>()
                .As<IHotWalletsProvider>()
                .WithParameter(TypedParameter.From<IReadOnlyDictionary<string, string>>(_blockchainsIntegrationSettings.Blockchains.ToDictionary(b => b.Type, b => b.HotWalletAddress)))
                .SingleInstance();

            foreach (var blockchain in _blockchainsIntegrationSettings.Blockchains)
            {
                _log.WriteInfo("Blockchains registration", "", 
                    $"Registering blockchain: {blockchain.Type} -> \r\nAPI: {blockchain.ApiUrl}\r\nSign: {blockchain.SignFacadeUrl}\r\nHW: {blockchain.HotWalletAddress}");

                builder.RegisterType<BlockchainApiClient>()
                    .Named<IBlockchainApiClient>(blockchain.Type)
                    .WithParameter(TypedParameter.From(blockchain.ApiUrl));

                builder.RegisterType<DepositWalletsBalanceProcessingPeriodicalHandler>()
                    .As<IStartable>()
                    .AutoActivate()
                    .SingleInstance()
                    .WithParameter(
                        (p, c) => p.ParameterType == typeof(IBlockchainApiClient),
                        (p, c) => c.ResolveNamed<IBlockchainApiClient>(blockchain.Type))
                    .WithParameter(TypedParameter.From(_settings.Monitoring.Period))
                    .WithParameter(TypedParameter.From(_settings.Requests.BatchSize))
                    .WithParameter(TypedParameter.From(blockchain.Type));
            }
        }
    }
}
