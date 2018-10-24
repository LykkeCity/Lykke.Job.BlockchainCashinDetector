using Lykke.Cqrs;
using Lykke.Cqrs.Abstractions.Middleware;
using Lykke.Cqrs.Middleware;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lykke.Job.BlockchainCashinDetector.IntegrationTests.Utils
{
    public class TestCommandsInterceptor : ICommandInterceptor
    {
        private readonly CqrsActionAwaiter _cqrsAwaiter;

        public TestCommandsInterceptor()
        {
            _cqrsAwaiter = new CqrsActionAwaiter();
        }

        public async Task<CommandHandlingResult> InterceptAsync(ICommandInterceptionContext context)
        {
            var commandType = context.Command.GetType();
            var result = await _cqrsAwaiter.InterceptAsync(commandType, async () =>
            {
                return await context.InvokeNextAsync();
            });

            return result;
        }

        public async Task WaitForCommandToBeHandledWithTimeoutAsync(Type commandType, TimeSpan timeout)
        {
            await _cqrsAwaiter.WaitActionCompletionWithTimeoutAsync(commandType, timeout);
        }
    }
}
