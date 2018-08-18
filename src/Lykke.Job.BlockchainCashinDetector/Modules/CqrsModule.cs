using System.Collections.Generic;
using Autofac;
using Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Job.BlockchainCashinDetector.Contract;
using Lykke.Job.BlockchainCashinDetector.Contract.Events;
using Lykke.Job.BlockchainCashinDetector.Settings.JobSettings;
using Lykke.Job.BlockchainCashinDetector.Workflow.CommandHandlers;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Lykke.Job.BlockchainCashinDetector.Workflow.Events;
using Lykke.Job.BlockchainCashinDetector.Workflow.Projections;
using Lykke.Job.BlockchainCashinDetector.Workflow.Sagas;
using Lykke.Job.BlockchainOperationsExecutor.Contract;
using Lykke.Messaging;
using Lykke.Messaging.Contract;
using Lykke.Messaging.RabbitMq;
using Lykke.Messaging.Serialization;


namespace Lykke.Job.BlockchainCashinDetector.Modules
{
    public class CqrsModule : Module
    {
        private static readonly string Self = BlockchainCashinDetectorBoundedContext.Name;

        private readonly CqrsSettings _settings;
        private readonly ILog _log;
        private readonly string _rabbitMqVirtualHost;

        public CqrsModule(CqrsSettings settings, ILog log, string rabbitMqVirtualHost = null)
        {
            _settings = settings;
            _log = log;
            _rabbitMqVirtualHost = rabbitMqVirtualHost;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(context => new AutofacDependencyResolver(context)).As<IDependencyResolver>().SingleInstance();

            var rabbitMqSettings = new RabbitMQ.Client.ConnectionFactory
            {
                Uri = _settings.RabbitConnectionString
            };
            var rabbitMqEndpoint = _rabbitMqVirtualHost == null
                ? rabbitMqSettings.Endpoint.ToString()
                : $"{rabbitMqSettings.Endpoint}/{_rabbitMqVirtualHost}";
            var messagingEngine = new MessagingEngine(_log,
                new TransportResolver(new Dictionary<string, TransportInfo>
                {
                    {
                        "RabbitMq",
                        new TransportInfo(
                            rabbitMqEndpoint, 
                            rabbitMqSettings.UserName,
                            rabbitMqSettings.Password, "None", "RabbitMq")
                    }
                }),
                new RabbitMqTransportFactory());

            // Sagas
            builder.RegisterType<CashinSaga>();

            // Command handlers
            builder.RegisterType<EnrollToMatchingEngineCommandsHandler>();
            builder.RegisterType<SetEnrolledBalanceCommandHandler>();
            builder.RegisterType<ResetEnrolledBalanceCommandHandler>();
            builder.RegisterType<NotifyCashinCompletedCommandsHandler>();
            builder.RegisterType<ReleaseDepositWalletLockCommandHandler>();

            // Projections
            builder.RegisterType<ClientOperationsProjection>();
            builder.RegisterType<MatchingEngineCallDeduplicationsProjection>();

            builder.Register(ctx => CreateEngine(ctx, messagingEngine))
                .As<ICqrsEngine>()
                .SingleInstance()
                .AutoActivate();
        }

        private CqrsEngine CreateEngine(IComponentContext ctx, IMessagingEngine messagingEngine)
        {
            var defaultRetryDelay = (long)_settings.RetryDelay.TotalMilliseconds;

            const string defaultPipeline = "commands";
            const string defaultRoute = "self";
            const string eventsRoute = "evets";

            return new CqrsEngine(
                _log,
                ctx.Resolve<IDependencyResolver>(),
                messagingEngine,
                new DefaultEndpointProvider(),
                true,
                Register.DefaultEndpointResolver(new RabbitMqConventionEndpointResolver(
                    "RabbitMq",
                    SerializationFormat.MessagePack,
                    environment: "lykke")),

                Register.BoundedContext(Self)
                    .FailedCommandRetryDelay(defaultRetryDelay)

                    .ListeningCommands(typeof(EnrollToMatchingEngineCommand))
                    .On(defaultRoute)
                    .WithLoopback()
                    .WithCommandsHandler<EnrollToMatchingEngineCommandsHandler>()
                    .PublishingEvents(typeof(CashinEnrolledToMatchingEngineEvent))
                    .With(defaultPipeline)

                    .ListeningCommands(typeof(SetEnrolledBalanceCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<SetEnrolledBalanceCommandHandler>()
                    .PublishingEvents(typeof(EnrolledBalanceSetEvent))
                    .With(defaultPipeline)

                    .ListeningCommands(typeof(ResetEnrolledBalanceCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<ResetEnrolledBalanceCommandHandler>()
                    .PublishingEvents(typeof(EnrolledBalanceResetEvent))
                    .With(defaultPipeline)

                    .ListeningCommands(typeof(ReleaseDepositWalletLockCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<ReleaseDepositWalletLockCommandHandler>()
                    .PublishingEvents(typeof(DepositWalletLockReleasedEvent))
                    .With(defaultPipeline)

                    .ListeningCommands(typeof(NotifyCashinCompletedCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<NotifyCashinCompletedCommandsHandler>()
                    .PublishingEvents(typeof(CashinCompletedEvent))
                    .With(eventsRoute)

                    .ListeningEvents(typeof(EnrolledBalanceSetEvent))
                    .From(Self)
                    .On(eventsRoute)
                    .WithProjection(typeof(MatchingEngineCallDeduplicationsProjection), Self)

                    .ListeningEvents(typeof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent))
                    .From(BlockchainOperationsExecutorBoundedContext.Name)
                    .On(eventsRoute)
                    .WithProjection(typeof(MatchingEngineCallDeduplicationsProjection),
                        BlockchainOperationsExecutorBoundedContext.Name)

                    .ListeningEvents(typeof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionFailedEvent))
                    .From(BlockchainOperationsExecutorBoundedContext.Name)
                    .On(eventsRoute)
                    .WithProjection(typeof(MatchingEngineCallDeduplicationsProjection), BlockchainOperationsExecutorBoundedContext.Name)

                    .ProcessingOptions(defaultRoute).MultiThreaded(8).QueueCapacity(1024)
                    .ProcessingOptions(eventsRoute).MultiThreaded(8).QueueCapacity(1024),

                // TODO: This should be moved to the separate service, which is responsible
                // for the client operations history

                Register.BoundedContext($"{Self}.client-operations")
                    .ListeningEvents(typeof(CashinEnrolledToMatchingEngineEvent))
                    .From(Self)
                    .On(defaultRoute)
                    .WithProjection(typeof(ClientOperationsProjection), Self)

                    .ListeningEvents(typeof(EnrolledBalanceSetEvent))
                    .From(Self)
                    .On(defaultRoute)
                    .WithProjection(typeof(ClientOperationsProjection), Self)

                    .ListeningEvents(typeof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent))
                    .From(BlockchainOperationsExecutorBoundedContext.Name)
                    .On(defaultRoute)
                    .WithProjection(typeof(ClientOperationsProjection), BlockchainOperationsExecutorBoundedContext.Name)

                    .ProcessingOptions(defaultRoute).MultiThreaded(8).QueueCapacity(1024),

                Register.Saga<CashinSaga>($"{Self}.saga")
                    .ListeningEvents(typeof(CashinEnrolledToMatchingEngineEvent))
                    .From(Self)
                    .On(defaultRoute)
                    .PublishingCommands(typeof(SetEnrolledBalanceCommand))
                    .To(Self)
                    .With(defaultPipeline)

                    .ListeningEvents(typeof(EnrolledBalanceSetEvent))
                    .From(Self)
                    .On(defaultRoute)
                    .PublishingCommands(typeof(BlockchainOperationsExecutor.Contract.Commands.StartOperationExecutionCommand))
                    .To(BlockchainOperationsExecutorBoundedContext.Name)
                    .With(defaultPipeline)

                    .ListeningEvents(typeof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent))
                    .From(BlockchainOperationsExecutorBoundedContext.Name)
                    .On(defaultRoute)
                    .PublishingCommands(typeof(ResetEnrolledBalanceCommand), 
                                        typeof(NotifyCashinCompletedCommand))
                    .To(Self)
                    .With(defaultPipeline)

                    .ListeningEvents(typeof(EnrolledBalanceResetEvent))
                    .From(Self)
                    .On(defaultRoute)
                    .PublishingCommands(typeof(ReleaseDepositWalletLockCommand))
                    .To(Self)
                    .With(defaultPipeline)

                    .ListeningEvents(typeof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionFailedEvent))
                    .From(BlockchainOperationsExecutorBoundedContext.Name)
                    .On(defaultRoute)

                    .ListeningEvents(typeof(DepositWalletLockReleasedEvent))
                    .From(Self)
                    .On(defaultRoute)

                    .ProcessingOptions(defaultRoute).MultiThreaded(8).QueueCapacity(1024));
        }
    }
}
