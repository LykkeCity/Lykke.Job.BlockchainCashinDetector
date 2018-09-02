using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Contract.Events;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.CommandHandlers
{
    public class NotifyCashinFailedCommandsHandler
    {
        [UsedImplicitly]
        public Task<CommandHandlingResult> Handle(NotifyCashinFailedCommand command, IEventPublisher publisher)
        {
            publisher.PublishEvent(new CashinFailedEvent
            {
                ClientId = command.ClientId,
                AssetId = command.AssetId,
                Amount = command.Amount,
                OperationId = command.OperationId,
                Error = command.Error,
                ErrorCode = command.ErrorCode
            });

            return Task.FromResult(CommandHandlingResult.Ok());
        }
    }
}
