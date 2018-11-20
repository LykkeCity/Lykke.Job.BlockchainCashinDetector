using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Cqrs;
using Lykke.Cqrs.Middleware;

namespace Lykke.Job.BlockchainCashinDetector.IntegrationTests.Utils
{
    internal class CqrsActionAwaiter
    {
        private ConcurrentDictionary<Type, EventWaitHandle> _typeEventHandles =
            new ConcurrentDictionary<Type, EventWaitHandle>();

        public CqrsActionAwaiter()
        {
        }

        public async Task<T> InterceptAsync<T>(Type type, Func<Task<T>> funcAsync)
        {
            try
            {
                var handle = ReceiveEventWaitHandle(type);
                var result = await funcAsync();
                handle.Set();

                return result;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public async Task WaitActionCompletionWithTimeoutAsync(Type type, TimeSpan timeout)
        {
            EventWaitHandle handle = ReceiveEventWaitHandle(type);
            handle.WaitOne(timeout);
        }

        //use after await _lock.WaitAsync();
        private EventWaitHandle ReceiveEventWaitHandle(Type commandType)
        {
            EventWaitHandle handle = _typeEventHandles.GetOrAdd(commandType, (x) =>
            {
                var newHandler = new AutoResetEvent(false);

                return newHandler;
            });

            return handle;
        }
    }
}
