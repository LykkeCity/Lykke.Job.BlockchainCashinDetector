using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lykke.Cqrs;
using Lykke.Cqrs.Abstractions.Middleware;
using Lykke.Cqrs.Middleware;

namespace Lykke.Job.BlockchainCashinDetector.IntegrationTests.Utils
{
    public class TestEventsInterceptor : IEventInterceptor
    {
        public TestEventsInterceptor()
        {
        }

        public  async Task<CommandHandlingResult> InterceptAsync(IEventInterceptionContext context)
        {
            return await context.InvokeNextAsync();
        }
    }
}
