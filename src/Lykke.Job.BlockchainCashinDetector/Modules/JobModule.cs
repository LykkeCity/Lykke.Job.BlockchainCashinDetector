using System;
using Autofac;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Job.BlockchainCashinDetector.Services;
using Lykke.Job.BlockchainCashinDetector.Settings;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Lykke.Job.BlockchainCashinDetector.Workflow.Events;
using Lykke.Sdk;
using Lykke.Service.Assets.Client;
using Lykke.SettingsReader;

namespace Lykke.Job.BlockchainCashinDetector.Modules
{
    [UsedImplicitly]
    public class JobModule : Module
    {
        private readonly IReloadingManager<AppSettings> _settings;

        public JobModule(IReloadingManager<AppSettings> settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            Lykke.Cqrs.MessageCancellation.Configuration.ContainerBuilderExtensions.RegisterCqrsMessageCancellation(
            builder,
            (options) =>
            {
                #region Registry

                //Commands
                options.Value
                    .MapMessageId<EnrollToMatchingEngineCommand>(x => x.OperationId.ToString())
                    .MapMessageId<NotifyCashinCompletedCommand>(x => x.OperationId.ToString())
                    .MapMessageId<NotifyCashinFailedCommand>(x => x.OperationId.ToString())
                    .MapMessageId<ReleaseDepositWalletLockCommand>(x => x.OperationId.ToString())
                    .MapMessageId<ResetEnrolledBalanceCommand>(x => x.OperationId.ToString())
                    .MapMessageId<SetEnrolledBalanceCommand>(x => x.OperationId.ToString())

                //Events
                    .MapMessageId<CashinEnrolledToMatchingEngineEvent>(x => x.OperationId.ToString())
                    .MapMessageId<DepositWalletLockedEvent>(x => x.OperationId.ToString())
                    .MapMessageId<DepositWalletLockReleasedEvent>(x => x.OperationId.ToString())
                    .MapMessageId<EnrolledBalanceResetEvent>(x => x.OperationId.ToString())
                    .MapMessageId<EnrolledBalanceSetEvent>(x => x.OperationId.ToString());
                #endregion
            });

            builder.RegisterType<StartupManager>()
                .As<IStartupManager>();

            builder.RegisterType<ShutdownManager>()
                .As<IShutdownManager>();

            builder.RegisterAssetsClient(new AssetServiceSettings
            {
                BaseUri = new Uri(_settings.CurrentValue.Assets.ServiceUrl),
                AssetsCacheExpirationPeriod = _settings.CurrentValue.Assets.CacheExpirationPeriod,
                AssetPairsCacheExpirationPeriod = _settings.CurrentValue.Assets.CacheExpirationPeriod
            });

            builder.RegisgterMeClient(_settings.CurrentValue.MatchingEngineClient.IpEndpoint.GetClientIpEndPoint());
            builder.RegisterChaosKitty(_settings.CurrentValue.BlockchainCashinDetectorJob.ChaosKitty);
        }
    }
}
