using Lykke.Cqrs.Configuration;
using Lykke.Cqrs.MessageCancellation.Interceptors;
using Lykke.Job.BlockchainCashinDetector.IntegrationTests.Utils;
using Lykke.Job.BlockchainCashinDetector.Modules;
using Lykke.Job.BlockchainCashinDetector.Settings;
using Lykke.SettingsReader;

namespace Lykke.Job.BlockchainCashinDetector.IntegrationTests.Modules
{
    public class CqrsTestModule : CqrsModule
    {
        public CqrsTestModule(IReloadingManager<AppSettings> settings) : base(settings)
        {
        }

        static CqrsTestModule()
        {
            CommandsInterceptor = new TestCommandsInterceptor();
            EventsInterceptor = new TestEventsInterceptor();
        }

        public static TestCommandsInterceptor CommandsInterceptor { get; protected set; }
        public static TestEventsInterceptor EventsInterceptor { get; protected set; }

        protected override IRegistration[] GetInterceptors()
        {
            return new IRegistration[]
            {
                Register.CommandInterceptor<MessageCancellationCommandInterceptor>(),
                Register.EventInterceptor<MessageCancellationEventInterceptor>(),
                Register.CommandInterceptors(CommandsInterceptor),
                Register.EventInterceptors(EventsInterceptor)
            };
        }
    }
}
