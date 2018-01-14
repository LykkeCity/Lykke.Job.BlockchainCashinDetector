﻿using Autofac;
using Common.Log;
using Lykke.Job.BlockchainCashinDetector.AzureRepositories;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.Job.BlockchainCashinDetector.Settings.JobSettings;
using Lykke.SettingsReader;

namespace Lykke.Job.BlockchainCashinDetector.Modules
{
    public class RepositoriesModule : Module
    {
        private readonly IReloadingManager<DbSettings> _dbSettings;
        private readonly ILog _log;

        public RepositoriesModule(
            IReloadingManager<DbSettings> dbSettings,
            ILog log)
        {
            _log = log;
            _dbSettings = dbSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => MatchingEngineCallsDeduplicationRepository.Create(_dbSettings.Nested(x => x.DataConnString), _log))
                .As<IMatchingEngineCallsDeduplicationRepository>();

            builder.Register(c => CashinRepository.Create(_dbSettings.Nested(x => x.DataConnString), _log))
                .As<ICashinRepository>();
        }
    }
}
