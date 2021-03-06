﻿using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Contract.Events;
using Lykke.Job.BlockchainCashinDetector.Workflow.Commands;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.CommandHandlers
{
    [UsedImplicitly]
    public class NotifyCashinCompletedCommandsHandler
    {
        [UsedImplicitly]
        public Task<CommandHandlingResult> Handle(NotifyCashinCompletedCommand command, IEventPublisher publisher)
        {
            publisher.PublishEvent(new CashinCompletedEvent
            {
                ClientId = command.ClientId,
                AssetId = command.AssetId,
                Amount = command.OperationAmount,
                TransactionAmount = command.TransactionnAmount,
                TransactionFee = command.TransactionFee,
                OperationType = command.OperationType,
                OperationId = command.OperationId,
                TransactionHash = command.TransactionHash
            });

            return Task.FromResult(CommandHandlingResult.Ok());
        }
    }
}
