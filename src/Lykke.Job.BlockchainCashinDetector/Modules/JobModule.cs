using System;
using Autofac;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Job.BlockchainCashinDetector.Contract.Events;
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

                options.Value
                    //Commands
                    .MapMessageId<RetrieveClientCommand>(x => x.OperationId.ToString())
                    .MapMessageId<EnrollToMatchingEngineCommand>(x => x.OperationId.ToString())
                    .MapMessageId<NotifyCashinCompletedCommand>(x => x.OperationId.ToString())
                    .MapMessageId<NotifyCashinFailedCommand>(x => x.OperationId.ToString())
                    .MapMessageId<ReleaseDepositWalletLockCommand>(x => x.OperationId.ToString())
                    .MapMessageId<ResetEnrolledBalanceCommand>(x => x.OperationId.ToString())
                    .MapMessageId<SetEnrolledBalanceCommand>(x => x.OperationId.ToString())
                    .MapMessageId<LockDepositWalletCommand>(x => x.BlockchainType.ToString())

                    //Events
                    .MapMessageId<ClientRetrievedEvent>(x => x.OperationId.ToString())
                    .MapMessageId<CashinCompletedEvent>(x => x.OperationId.ToString())
                    .MapMessageId<CashinFailedEvent>(x => x.OperationId.ToString())
                    .MapMessageId<CashinEnrolledToMatchingEngineEvent>(x => x.OperationId.ToString())
                    .MapMessageId<DepositWalletLockedEvent>(x => x.OperationId.ToString())
                    .MapMessageId<DepositWalletLockReleasedEvent>(x => x.OperationId.ToString())
                    .MapMessageId<EnrolledBalanceResetEvent>(x => x.OperationId.ToString())
                    .MapMessageId<EnrolledBalanceSetEvent>(x => x.OperationId.ToString())

                    //External Commands
                    .MapMessageId<BlockchainOperationsExecutor.Contract.Commands.StartOneToManyOutputsExecutionCommand>(
                        x => x.OperationId.ToString())
                    .MapMessageId<BlockchainOperationsExecutor.Contract.Commands.StartOperationExecutionCommand>(
                        x => x.OperationId.ToString())

                    //External Events
                    .MapMessageId<BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent>(
                        x => x.OperationId.ToString())
                    .MapMessageId<BlockchainOperationsExecutor.Contract.Events.OperationExecutionFailedEvent>(
                        x => x.OperationId.ToString())
                    .MapMessageId<BlockchainOperationsExecutor.Contract.Events.OneToManyOperationExecutionCompletedEvent>(
                        x => x.OperationId.ToString());

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
