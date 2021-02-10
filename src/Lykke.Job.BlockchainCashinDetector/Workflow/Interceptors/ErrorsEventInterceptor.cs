using System;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Abstractions.Middleware;
using Lykke.Cqrs.Middleware;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Interceptors
{
    public class ErrorsEventInterceptor : IEventInterceptor
    {
        private readonly ILog _log;

        public ErrorsEventInterceptor(ILogFactory logFactory)
        {
            _log = logFactory.CreateLog(this);
        }

        public async Task<CommandHandlingResult> InterceptAsync(IEventInterceptionContext context)
        {
            try
            {
                var result = await context.InvokeNextAsync();
                return result;
            }
            catch (InvalidAggregateStateException ex)
            {
                _log.Warning($"{nameof(InvalidAggregateStateException)} handled", ex);
                return CommandHandlingResult.Fail(TimeSpan.FromSeconds(10));
            }
        }
    }
}
