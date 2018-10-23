using Autofac;
using Lykke.Common.Log;
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
using Lykke.Messaging.RabbitMq;
using Lykke.Messaging.Serialization;
using System.Collections.Generic;
using Lykke.Job.BlockchainCashinDetector.IntegrationTests.Utils;

namespace Lykke.Job.BlockchainCashinDetector.IntegrationTests.Modules
{
    public class CqrsTestModule : Module
    {
        public static readonly string Self = BlockchainCashinDetectorBoundedContext.Name;

        private readonly CqrsSettings _settings;
        private readonly string _host;

        public CqrsTestModule(CqrsSettings settings, string host)
        {
            _settings = settings;
            _host = host;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(context => new AutofacDependencyResolver(context)).As<IDependencyResolver>().SingleInstance();

            RegisterInfrastructure(builder);
        }

        private MessagingEngine RegisterMessagingEngine(IComponentContext ctx)
        {
            var logFactory = ctx.Resolve<ILogFactory>();
            var rabbitMqSettings = new RabbitMQ.Client.ConnectionFactory
            {
                Uri = _settings.RabbitConnectionString
            };
            var rabbitMqEndpoint = rabbitMqSettings.Endpoint.ToString() + "/"+ _host;

            var messagingEngine = new MessagingEngine(logFactory,
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
                new RabbitMqTransportFactory(logFactory));

            return messagingEngine;
        }

        private void RegisterInfrastructure(ContainerBuilder builder)
        {
            RegisterWorkflowDependencies(builder);

            builder.Register(ctx => CreateEngine(ctx))
                .As<ICqrsEngine>()
                .SingleInstance()
                .AutoActivate();
        }

        private void RegisterWorkflowDependencies(ContainerBuilder builder)
        {
            // Sagas
            builder.RegisterType<CashinSaga>();

            // Command handlers
            builder.RegisterType<LockDepositWalletCommandsHandler>();
            builder.RegisterType<EnrollToMatchingEngineCommandsHandler>();
            builder.RegisterType<SetEnrolledBalanceCommandHandler>();
            builder.RegisterType<ResetEnrolledBalanceCommandHandler>();
            builder.RegisterType<NotifyCashinCompletedCommandsHandler>();
            builder.RegisterType<ReleaseDepositWalletLockCommandHandler>();
            builder.RegisterType<NotifyCashinFailedCommandsHandler>();

            // Projections
            builder.RegisterType<MatchingEngineCallDeduplicationsProjection>();
        }

        private IEndpointResolver GetDefaultEndpointResolver()
        {
            return new RabbitMqConventionEndpointResolver(
                "RabbitMq",
                SerializationFormat.MessagePack,
                environment: "lykke");
        }

        private CqrsEngine CreateEngine(IComponentContext ctx)
        {
            var logFactory = ctx.Resolve<ILogFactory>();
            var messagingEngine = RegisterMessagingEngine(ctx);
            var defaultRetryDelay = (long)_settings.RetryDelay.TotalMilliseconds;

            const string defaultPipeline = "commands";
            const string defaultRoute = "self";
            const string eventsRoute = "evets";

            var registration = new List<IRegistration>()
            {
                Register.DefaultEndpointResolver(GetDefaultEndpointResolver()),
                Register.BoundedContext(Self)
                    .FailedCommandRetryDelay(defaultRetryDelay)

                    .ListeningCommands(typeof(LockDepositWalletCommand))
                    .On(defaultRoute)
                    .WithLoopback()//When it is sent not from saga
                    .WithCommandsHandler<LockDepositWalletCommandsHandler>()
                    .PublishingEvents(typeof(DepositWalletLockedEvent))
                    .With(defaultPipeline)

                    .ListeningCommands(typeof(EnrollToMatchingEngineCommand))
                    .On(defaultRoute)
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

                    .ListeningCommands(typeof(NotifyCashinFailedCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<NotifyCashinFailedCommandsHandler>()
                    .PublishingEvents(typeof(CashinFailedEvent))
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

                Register.Saga<CashinSaga>($"{Self}.saga")
                    .ListeningEvents(typeof(DepositWalletLockedEvent))
                    .From(Self)
                    .On(defaultRoute)
                    .PublishingCommands(
                        typeof(EnrollToMatchingEngineCommand),
                        typeof(ReleaseDepositWalletLockCommand))
                    .To(Self)
                    .With(defaultPipeline)

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
                    .PublishingCommands(typeof(ResetEnrolledBalanceCommand))
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
                    .PublishingCommands(
                        typeof(NotifyCashinFailedCommand),
                        typeof(NotifyCashinCompletedCommand))
                    .To(Self)
                    .With(defaultPipeline)

                    .ProcessingOptions(defaultRoute).MultiThreaded(8).QueueCapacity(1024)
            };

            var interceptors = GetInterceptors();

            if (interceptors != null)
            {
                registration.AddRange(interceptors);
            }

            return new CqrsEngine(
                logFactory,
                ctx.Resolve<IDependencyResolver>(),
                messagingEngine,
                new DefaultEndpointProvider(),
                true,
                registration.ToArray());
        }

        static CqrsTestModule()
        {
            CommandsInterceptor = new TestCommandsInterceptor();
            EventsInterceptor = new TestEventsInterceptor();
        }

        public static TestCommandsInterceptor CommandsInterceptor {get; protected set;}
        public static TestEventsInterceptor EventsInterceptor { get; protected set; }

        private IRegistration[] GetInterceptors()
        {
            return new IRegistration[]
            {
                Register.CommandInterceptors(CommandsInterceptor),
                Register.EventInterceptors(EventsInterceptor)
            };
        }
    }
}
