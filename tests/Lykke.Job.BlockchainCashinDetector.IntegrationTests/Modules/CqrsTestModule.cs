using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Job.BlockchainCashinDetector.Contract;
using Lykke.Job.BlockchainCashinDetector.Contract.Events;
using Lykke.Job.BlockchainCashinDetector.Modules;
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

namespace Lykke.Job.BlockchainCashinDetector.IntegrationTests.Modules
{
    public class CqrsTestModule : CqrsModule
    {
        private static readonly string Self = BlockchainCashinDetectorBoundedContext.Name;


        public CqrsTestModule(CqrsSettings settings, string rabbitMqVirtualHost = null) :
            base(settings, rabbitMqVirtualHost)
        {
        }

        protected override MessagingEngine RegisterMessagingEngine(IComponentContext ctx)
        {
            var logFactory = ctx.Resolve<ILogFactory>();
            var messagingEngine = new MessagingEngine(logFactory,
                new TransportResolver(new Dictionary<string, TransportInfo>
                {
                    {"InMemory", new TransportInfo("none", "none", "none", null, "InMemory")},
                }));

            return messagingEngine;
        }

        protected override IEndpointResolver GetDefaultEndpointResolver()
        {
            return new InMemoryEndpointResolver();
        }
    }
}
