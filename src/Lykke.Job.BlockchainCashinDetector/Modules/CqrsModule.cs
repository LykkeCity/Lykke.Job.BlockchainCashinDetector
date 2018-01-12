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
using Lykke.Job.BlockchainTransfersExecutor.Contract;
using Lykke.Messaging;

namespace Lykke.Job.BlockchainCashinDetector.Modules
{
    public class CqrsModule : Module
    {
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
                    "lykke.bcn-integration")),

                Register.BoundedContext(BlockchainCashinDetectorBoundedContext.Name)
                    .FailedCommandRetryDelay(defaultRetryDelay)

                    .ListeningCommands(typeof(EnrollToMatchingEngineCommand))
                    .On("cashin-commands")
                    .WithLoopback()
                    .WithCommandsHandler<EnrollToMatchingEngineCommandsHandler>()

                    .ListeningCommands(typeof(EndCashinCommand))
                    .On("cashin-commands")
                    .WithCommandsHandler<EndCashinCommandsHandler>()

                    .ProcessingOptions("cashin-commands").MultiThreaded(10).QueueCapacity(1024),

                Register.Saga<CashinSaga>("cashin-saga")
                    .ListeningEvents(typeof(CashinEnrolledToMatchingEngineEvent))
                    .From(BlockchainCashinDetectorBoundedContext.Name)
                    .On("cashin-events")

                    .ListeningEvents(
                        typeof(BlockchainTransfersExecutor.Contract.Events.TransferCompletedEvent),
                        typeof(BlockchainTransfersExecutor.Contract.Events.TransferFailedEvent))
                    .From(BlockchainTransferExecutorBoundedContext.Name)
                    .On("transfer-events")

                    .PublishingCommands(typeof(EndCashinCommand))
                    .To(BlockchainCashinDetectorBoundedContext.Name)
                    .With("cashin-commands")

                    .PublishingCommands(typeof(BlockchainTransfersExecutor.Contract.Commands.StartTransferCommand))
                    .To(BlockchainTransferExecutorBoundedContext.Name)
                    .With("transfer-commands"));
        }
    }
}
