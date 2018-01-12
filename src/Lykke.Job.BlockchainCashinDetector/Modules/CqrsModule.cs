using System.Collections.Generic;
using Autofac;
using Common.Log;
using Inceptum.Cqrs.Configuration;
using Inceptum.Messaging;
using Inceptum.Messaging.Contract;
using Inceptum.Messaging.RabbitMq;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Contract;
using Lykke.Job.BlockchainCashinDetector.Core.Domain.Cashin.Commands;
using Lykke.Job.BlockchainCashinDetector.Core.Domain.Cashin.Events;
using Lykke.Job.BlockchainCashinDetector.Settings.JobSettings;
using Lykke.Job.BlockchainCashinDetector.Workflow;
using Lykke.Job.BlockchainCashinDetector.Workflow.CommandHandlers;
using Lykke.Job.BlockchainCashinDetector.Workflow.Sagas;
using Lykke.Job.BlockchainOperationsExecutor.Contract;
using Lykke.Messaging;

namespace Lykke.Job.BlockchainCashinDetector.Modules
{
    public class CqrsModule : Module
    {
        private readonly CqrsSettings _settings;
        private readonly ChaosSettings _chaosSettings;
        private readonly ILog _log;

        public CqrsModule(CqrsSettings settings, ChaosSettings chaosSettings, ILog log)
        {
            _settings = settings;
            _chaosSettings = chaosSettings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            if (_chaosSettings != null)
            {
                ChaosKitty.StateOfChaos = _chaosSettings.StateOfChaos;
            }

            builder.Register(context => new AutofacDependencyResolver(context)).As<IDependencyResolver>().SingleInstance();

            builder.RegisterType<RetryDelayProvider>()
                .AsSelf()
                .WithParameter(TypedParameter.From(_settings.RetryDelay))
                .SingleInstance();

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
            builder.RegisterType<EnrollToMatchingEngineCommandsHandler>();
            builder.RegisterType<EndCashinCommandsHandler>();

            builder.Register(ctx => CreateEngine(ctx, messagingEngine))
                .As<ICqrsEngine>()
                .SingleInstance()
                .AutoActivate();
        }

        private CqrsEngine CreateEngine(IComponentContext ctx, IMessagingEngine messagingEngine)
        {
            var defaultRetryDelay = (long)_settings.RetryDelay.TotalMilliseconds;

            return new CqrsEngine(
                _log,
                ctx.Resolve<IDependencyResolver>(),
                messagingEngine,
                new DefaultEndpointProvider(),
                true,
                Register.DefaultEndpointResolver(new RabbitMqConventionEndpointResolver(
                    "RabbitMq",
                    "protobuf",
                    environment: "lykke.bcn-integration")),

                Register.BoundedContext(BlockchainCashinDetectorBoundedContext.Name)
                    .FailedCommandRetryDelay(defaultRetryDelay)

                    .ListeningCommands(typeof(EnrollToMatchingEngineCommand))
                    .On("cashin-enroll-commands")
                    .WithLoopback()
                    .WithCommandsHandler<EnrollToMatchingEngineCommandsHandler>()

                    .ListeningCommands(typeof(EndCashinCommand))
                    .On("cashin-end-commands")
                    .WithCommandsHandler<EndCashinCommandsHandler>()

                    .ProcessingOptions("cashin-enroll-commands").MultiThreaded(10).QueueCapacity(1024)
                    .ProcessingOptions("cashin-end-commands").MultiThreaded(10).QueueCapacity(1024),

                Register.Saga<CashinSaga>("cashin-saga")
                    .ListeningEvents(typeof(CashinEnrolledToMatchingEngineEvent))
                    .From(BlockchainCashinDetectorBoundedContext.Name)
                    .On("cashin-enroll-events")

                    .ListeningEvents(
                        typeof(BlockchainOperationsExecutor.Contract.Events.OperationCompletedEvent),
                        typeof(BlockchainOperationsExecutor.Contract.Events.OperationFailedEvent))
                    .From(BlockchainOperationsExecutorBoundedContext.Name)
                    .On("operation-end-events")

                    .PublishingCommands(typeof(EndCashinCommand))
                    .To(BlockchainCashinDetectorBoundedContext.Name)
                    .With("cashin-end-commands")

                    .PublishingCommands(typeof(BlockchainOperationsExecutor.Contract.Commands.StartOperationCommand))
                    .To(BlockchainOperationsExecutorBoundedContext.Name)
                    .With("operation-start-commands"));
        }
    }
}
