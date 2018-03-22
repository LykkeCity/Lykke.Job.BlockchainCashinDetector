using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Contract.Events;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.CommandHandlers
{
    [UsedImplicitly]
    public class OperationCompletedCommandsHandler
    {
        private readonly ILog _log;

        public OperationCompletedCommandsHandler(
            ILog log)
        {
            _log = log;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(NotifyCashinCompletedCommand command, IEventPublisher publisher)
        {
            publisher.PublishEvent(new CashinCompletedEvent()
            {
                ClientId = command.ClientId,
                AssetId = command.AssetId,
                Amount = command.Amount
            });

            return CommandHandlingResult.Ok();
        }
    }
}
