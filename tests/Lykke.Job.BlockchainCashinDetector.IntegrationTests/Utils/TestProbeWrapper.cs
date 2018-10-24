using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Lykke.Cqrs;

namespace Lykke.Job.BlockchainCashinDetector.IntegrationTests.Utils
{
    public class TestProbeWrapper<T> where T : class
    {
        private Dictionary<(Type, Type), MethodInfo> _dict;
        public T Handler { get; protected set; }

        public TestProbeWrapper(T instance)
        {
            Handler = instance;
            ExtractTypeHandlers();
        }

        public async Task HandleWithResponse<TMessage>(TMessage message, ICommandSender sender)
        {
            if (!_dict.TryGetValue((typeof(TMessage), typeof(ICommandSender)), out var method))
            {
                throw new InvalidOperationException($"Type {typeof(T)} does not know how to handle {typeof(TMessage)}");
            }

            var awaitableTask = (Task)method.Invoke(Handler, new[] { (object)message, sender });
            await awaitableTask;
        }

        public async Task HandleWithResponse<TMessage>(TMessage message, IEventPublisher publisher)
        {
            if (!_dict.TryGetValue((typeof(TMessage), typeof(IEventPublisher)), out var method))
            {
                throw new InvalidOperationException($"Type {typeof(T)} does not know how to handle {typeof(TMessage)}");
            }

            var awaitableTask = (Task)method.Invoke(Handler, new[] { (object)message, publisher });
            await awaitableTask;
        }

        private void ExtractTypeHandlers()
        {
            var methods = typeof(T)
                .GetMethods(BindingFlags.Instance)
                .Where(x =>
                {
                    var returnType = x.ReturnParameter;
                    var parameters = x.GetParameters();
                    if (parameters.Length != 2)
                        return false;

                    var param1 = parameters[0];
                    var param2 = parameters[1];
                    var isCqrsAsyncMethod = typeof(Task).IsAssignableFrom(returnType.ParameterType) ||
                                            typeof(Task<CommandHandlingResult>).IsAssignableFrom(returnType.ParameterType);
                    var isSecondParameterCqrsType =
                        typeof(IEventPublisher).IsAssignableFrom(returnType.ParameterType) ||
                        typeof(ICommandSender).IsAssignableFrom(returnType.ParameterType);

                    return isCqrsAsyncMethod && isSecondParameterCqrsType;
                });

            _dict = methods.ToDictionary(x =>
            {
                var @params = x.GetParameters();
                return (@params[0].ParameterType, @params[1].ParameterType);
            });
        }
    }
}
