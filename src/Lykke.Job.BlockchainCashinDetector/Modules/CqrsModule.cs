using System.Collections.Generic;
using Autofac;
using Common.Log;
using Inceptum.Cqrs.Configuration;
using Inceptum.Messaging;
using Inceptum.Messaging.Contract;
using Inceptum.Messaging.RabbitMq;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Contract;
using Lykke.Job.BlockchainCashinDetector.Core;
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
            builder.RegisterType<DetectDepositBalanceCommandHandler>();

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
                Register.DefaultEndpointResolver(new RabbitMqConventionEndpointResolver("RabbitMq", "protobuf")),

                Register.BoundedContext(Self)
                    .FailedCommandRetryDelay(defaultRetryDelay)

                    .ListeningCommands(typeof(DetectDepositBalanceCommand))
                    .On("detect-balance")
                    .WithLoopback()
                    .WithCommandsHandler<DetectDepositBalanceCommandHandler>()
                    .PublishingEvents(typeof(DepositBalanceDetectedEvent))
                    .With("balance-detected")
                    
                    .ListeningCommands(typeof(StartCashinCommand))
                    .On("start")
                    .WithCommandsHandler<StartCashinCommandsHandler>()
                    .PublishingEvents(typeof(CashinStartedEvent))
                    .With("started")
                    
                    .ListeningCommands(typeof(EnrollToMatchingEngineCommand))
                    .On("enroll")
                    .WithCommandsHandler<EnrollToMatchingEngineCommandsHandler>()
                    .PublishingEvents(typeof(CashinEnrolledToMatchingEngineEvent))
                    .With("enrolled")
                    
                    .ProcessingOptions("detect-balance").MultiThreaded(4).QueueCapacity(1024)
                    .ProcessingOptions("start").MultiThreaded(4).QueueCapacity(1024)
                    .ProcessingOptions("enroll").MultiThreaded(4).QueueCapacity(1024),

                Register.Saga<CashinSaga>("cashin-saga")
                    .ListeningEvents(typeof(DepositBalanceDetectedEvent))
                    .From(Self)
                    .On("balance-detected")
                    .PublishingCommands(typeof(StartCashinCommand))
                    .To(Self)
                    .With("start")

                    .ListeningEvents(typeof(CashinStartedEvent))
                    .From(Self)
                    .On("started")
                    .PublishingCommands(typeof(EnrollToMatchingEngineCommand))
                    .To(Self)
                    .With("enroll")

                    .ListeningEvents(typeof(CashinEnrolledToMatchingEngineEvent))
                    .From(Self)
                    .On("enrolled")
                    .PublishingCommands(typeof(BlockchainOperationsExecutor.Contract.Commands.StartOperationCommand))
                    .To(BlockchainOperationsExecutorBoundedContext.Name)
                    .With("start")

                    .ListeningEvents(
                        typeof(BlockchainOperationsExecutor.Contract.Events.OperationCompletedEvent),
                        typeof(BlockchainOperationsExecutor.Contract.Events.OperationFailedEvent))
                    .From(BlockchainOperationsExecutorBoundedContext.Name)
                    .On("finished"));
        }
    }
}
