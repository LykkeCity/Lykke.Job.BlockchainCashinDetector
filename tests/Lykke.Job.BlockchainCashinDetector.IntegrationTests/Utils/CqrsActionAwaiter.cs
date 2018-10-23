using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Cqrs;
using Lykke.Cqrs.Middleware;

namespace Lykke.Job.BlockchainCashinDetector.IntegrationTests.Utils
{
    internal class CqrsActionAwaiter
    {
        private Dictionary<Type, EventWaitHandle> _processingDict =
            new Dictionary<Type, EventWaitHandle>();

        private SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public CqrsActionAwaiter()
        {
        }

        public async Task<T> InterceptAsync<T>(Type type, Func<Task<T>> funcAsync)
        {
            try
            {
                await _lock.WaitAsync();
                var handle = ReceiveEventWaitHandle(type);
                var result = await funcAsync();
                handle.Set();

                return result;
            }
            catch (Exception e)
            {
                throw;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task WaitActionCompletionWithTimeoutAsync(Type type, TimeSpan timeout)
        {
            EventWaitHandle handle = null;

            await _lock.WaitAsync();
            handle = ReceiveEventWaitHandle(type);
            var task = Task.Run(() =>
            {
                Thread.Sleep(1111);
                _lock.Release();
            });
            handle.WaitOne(timeout);
            await task;
        }

        //use after await _lock.WaitAsync();
        private EventWaitHandle ReceiveEventWaitHandle(Type commandType)
        {
            EventWaitHandle handle = null;

            if (!_processingDict.TryGetValue(commandType, out handle))
            {
                handle = new AutoResetEvent(false);
                _processingDict[commandType] = handle;
            }

            return handle;
        }
    }
}
