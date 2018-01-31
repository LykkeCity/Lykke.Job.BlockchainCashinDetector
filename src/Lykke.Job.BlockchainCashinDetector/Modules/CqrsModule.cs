using System.Collections.Generic;
using Autofac;
using Common.Log;
using Inceptum.Cqrs.Configuration;
using Inceptum.Messaging;
using Inceptum.Messaging.Contract;
using Inceptum.Messaging.RabbitMq;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Contract;
using Lykke.Job.BlockchainCashinDetector.Settings.JobSettings;
using Lykke.Job.BlockchainCashinDetector.Workflow.CommandHandlers;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;
using Lykke.Job.BlockchainCashinDetector.Workflow.Events;
using Lykke.Job.BlockchainCashinDetector.Workflow.Sagas;
using Lykke.Job.BlockchainOperationsExecutor.Contract;
using Lykke.Messaging;

namespace Lykke.Job.BlockchainCashinDetector.Modules
{
    public class CqrsModule : Module
    {
        private static readonly string Self = BlockchainCashinDetectorBoundedContext.Name;

        private readonly CqrsSettings _settings;
        private readonly ILog _log;

        public CqrsModule(CqrsSettings settings, ILog log)
        {
            _settings = settings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(context => new AutofacDependencyResolver(context)).As<IDependencyResolver>().SingleInstance();

            var rabbitMqSettings = new RabbitMQ.Client.ConnectionFactory
            {
                Uri = _settings.RabbitConnectionString
            };
            var messagingEngine = new MessagingEngine(_log,
                new TransportResolver(new Dictionary<string, TransportInfo>
                {
                    {
                        "RabbitMq",
                        new TransportInfo(rabbitMqSettings.Endpoint.ToString(), rabbitMqSettings.UserName,
                            rabbitMqSettings.Password, "None", "RabbitMq")
                    }
                }),
                new RabbitMqTransportFactory());

            // Sagas
            builder.RegisterType<CashinSaga>();

            // Command handlers
            builder.RegisterType<StartCashinCommandsHandler>();
            builder.RegisterType<EnrollToMatchingEngineCommandsHandler>();
            builder.RegisterType<RegisterClientOperationStartCommandsHandler>();
            builder.RegisterType<DetectDepositBalanceCommandHandler>();
            builder.RegisterType<RemoveMatchingEngineDeduplicationLockCommandsHandler>();
            builder.RegisterType<RegisterClientOperationFinishCommandsHandler>();

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

            return new CqrsEngine(
                _log,
                ctx.Resolve<IDependencyResolver>(),
                messagingEngine,
                new DefaultEndpointProvider(),
                true,
                Register.DefaultEndpointResolver(new RabbitMqConventionEndpointResolver(
                    "RabbitMq",
                    "messagepack", 
                    environment: "lykke")),

                Register.BoundedContext(Self)
                    .FailedCommandRetryDelay(defaultRetryDelay)

                    .ListeningCommands(typeof(DetectDepositBalanceCommand))
                    .On(defaultRoute)
                    .WithLoopback()
                    .WithCommandsHandler<DetectDepositBalanceCommandHandler>()
                    .PublishingEvents(typeof(DepositBalanceDetectedEvent))
                    .With(defaultPipeline)
                    
                    .ListeningCommands(typeof(StartCashinCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<StartCashinCommandsHandler>()
                    .PublishingEvents(typeof(CashinStartedEvent))
                    .With(defaultPipeline)
                    
                    .ListeningCommands(typeof(EnrollToMatchingEngineCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<EnrollToMatchingEngineCommandsHandler>()
                    .PublishingEvents(typeof(CashinEnrolledToMatchingEngineEvent))
                    .With(defaultPipeline)

                    .ListeningCommands(typeof(RegisterClientOperationStartCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<RegisterClientOperationStartCommandsHandler>()
                    .PublishingEvents(typeof(ClientOperationStartRegisteredEvent))
                    .With(defaultPipeline)

                    .ListeningCommands(typeof(RemoveMatchingEngineDeduplicationLockCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<RemoveMatchingEngineDeduplicationLockCommandsHandler>()
                    .PublishingEvents(typeof(MatchingEngineDeduplicationLockRemovedEvent))
                    .With(defaultPipeline)

                    .ListeningCommands(typeof(RegisterClientOperationFinishCommand))
                    .On(defaultRoute)
                    .WithCommandsHandler<RegisterClientOperationFinishCommandsHandler>()
                    .PublishingEvents(typeof(ClientOperationFinishRegisteredEvent))
                    .With(defaultPipeline)

                    .ProcessingOptions(defaultRoute).MultiThreaded(8).QueueCapacity(1024),

                Register.Saga<CashinSaga>($"{Self}.saga")
                    .ListeningEvents(typeof(DepositBalanceDetectedEvent))
                    .From(Self)
                    .On(defaultRoute)
                    .PublishingCommands(typeof(StartCashinCommand))
                    .To(Self)
                    .With(defaultPipeline)

                    .ListeningEvents(typeof(CashinStartedEvent))
                    .From(Self)
                    .On(defaultRoute)
                    .PublishingCommands(typeof(EnrollToMatchingEngineCommand))
                    .To(Self)
                    .With(defaultPipeline)

                    .ListeningEvents(typeof(CashinEnrolledToMatchingEngineEvent))
                    .From(Self)
                    .On(defaultRoute)
                    .PublishingCommands(typeof(RegisterClientOperationStartCommand))
                    .To(Self)
                    .With(defaultPipeline)

                    .ListeningEvents(typeof(ClientOperationStartRegisteredEvent))
                    .From(Self)
                    .On(defaultRoute)
                    .PublishingCommands(typeof(BlockchainOperationsExecutor.Contract.Commands.StartOperationExecutionCommand))
                    .To(BlockchainOperationsExecutorBoundedContext.Name)
                    .With(defaultPipeline)

                    .ListeningEvents(
                        typeof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionCompletedEvent),
                        typeof(BlockchainOperationsExecutor.Contract.Events.OperationExecutionFailedEvent))
                    .From(BlockchainOperationsExecutorBoundedContext.Name)
                    .On(defaultRoute)
                    .PublishingCommands(typeof(RemoveMatchingEngineDeduplicationLockCommand))
                    .To(Self)
                    .With(defaultPipeline)

                    .ListeningEvents(typeof(MatchingEngineDeduplicationLockRemovedEvent))
                    .From(Self)
                    .On(defaultRoute)
                    .PublishingCommands(typeof(RegisterClientOperationFinishCommand))
                    .To(Self)
                    .With(defaultPipeline)

                    .ListeningEvents(typeof(ClientOperationFinishRegisteredEvent))
                    .From(Self)
                    .On(defaultRoute));
        }
    }
}
