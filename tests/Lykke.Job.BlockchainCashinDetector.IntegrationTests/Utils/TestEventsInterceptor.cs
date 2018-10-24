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
        private readonly CqrsActionAwaiter _cqrsAwaiter;

        public TestEventsInterceptor()
        {
            _cqrsAwaiter = new CqrsActionAwaiter();
        }

        public  async Task<CommandHandlingResult> InterceptAsync(IEventInterceptionContext context)
        {
            var commandType = context.Event.GetType();
            var result = await _cqrsAwaiter.InterceptAsync(commandType, async () =>
            {
                return await context.InvokeNextAsync();
            });

            return result;
        }

        public async Task WaitForEventToBeHandledWithTimeoutAsync(Type eventType, TimeSpan timeout)
        {
            await _cqrsAwaiter.WaitActionCompletionWithTimeoutAsync(eventType, timeout);
        }
    }
}
