using Autofac;
using Common.Log;
using Lykke.Common.Chaos;
using Lykke.Common.Log;
using Lykke.Job.BlockchainCashinDetector.AzureRepositories;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.Job.BlockchainCashinDetector.Settings.JobSettings;
using Lykke.SettingsReader;

namespace Lykke.Job.BlockchainCashinDetector.Modules
{
    public class RepositoriesModule : Module
    {
        private readonly IReloadingManager<DbSettings> _dbSettings;

        public RepositoriesModule(
            IReloadingManager<DbSettings> dbSettings)
        {
            _dbSettings = dbSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => EnrolledBalanceRepository.Create(_dbSettings.Nested(x => x.DataConnString), c.Resolve<ILogFactory>()))
                .As<IEnrolledBalanceRepository>()
                .SingleInstance();

            builder.Register(c => MatchingEngineCallsDeduplicationRepository.Create(_dbSettings.Nested(x => x.DataConnString), c.Resolve<ILogFactory>()))
                .As<IMatchingEngineCallsDeduplicationRepository>()
                .SingleInstance();

            builder.Register(c => CashinRepository.Create(
                    _dbSettings.Nested(x => x.DataConnString),
                    c.Resolve<ILogFactory>(),
                    c.Resolve<IChaosKitty>()))
                .As<ICashinRepository>()
                .SingleInstance();

            builder.Register(c => DepositWalletLockRepository.Create(
                    _dbSettings.Nested(x => x.DataConnString),
                    c.Resolve<ILogFactory>()))
                .As<IDepositWalletLockRepository>()
                .SingleInstance();
        }
    }
}
