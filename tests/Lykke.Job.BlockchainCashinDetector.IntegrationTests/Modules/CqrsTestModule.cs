using Autofac;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Contract;
using Lykke.Job.BlockchainCashinDetector.Modules;
using Lykke.Job.BlockchainCashinDetector.Settings.JobSettings;
using Lykke.Messaging;
using System.Collections.Generic;
using Lykke.Cqrs.Configuration;
using Lykke.Job.BlockchainCashinDetector.IntegrationTests.Utils;
using Refit;

namespace Lykke.Job.BlockchainCashinDetector.IntegrationTests.Modules
{
    public class CqrsTestModule : CqrsModule
    {
        public CqrsTestModule(CqrsSettings settings, string rabbitMqVirtualHost = null) :
            base(settings, rabbitMqVirtualHost)
        {
        }

        static CqrsTestModule()
        {
            CommandsInterceptor = new TestCommandsInterceptor();
            EventsInterceptor = new TestEventsInterceptor();
        }

        public static TestCommandsInterceptor CommandsInterceptor {get; protected set;}
        public static TestEventsInterceptor EventsInterceptor { get; protected set; }

        protected override IRegistration[] GetInterceptors()
        {
            return new IRegistration[]
            {
                Register.CommandInterceptors(CommandsInterceptor),
                Register.EventInterceptors(EventsInterceptor)
            };
        }
    }
}
