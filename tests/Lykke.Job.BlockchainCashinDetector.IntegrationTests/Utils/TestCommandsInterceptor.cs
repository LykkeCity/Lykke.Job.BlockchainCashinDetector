using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lykke.Cqrs;
using Lykke.Cqrs.Abstractions.Middleware;
using Lykke.Cqrs.Middleware;

namespace Lykke.Job.BlockchainCashinDetector.IntegrationTests.Utils
{
    public class TestCommandsInterceptor : ICommandInterceptor
    {
        public TestCommandsInterceptor()
        {
        }

        public async Task<CommandHandlingResult> InterceptAsync(ICommandInterceptionContext context)
        {
            return await context.InvokeNextAsync();
        }
    }
}
