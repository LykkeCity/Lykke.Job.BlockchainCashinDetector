﻿using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashinDetector.Core.Domain.Cashin;
using Lykke.Job.BlockchainCashinDetector.Core.Domain.Cashin.Commands;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.CommandHandlers
{
    [UsedImplicitly]
    public class EndCashinCommandsHandler
    {
        private readonly ILog _log;
        private readonly IActiveCashinRepository _activeCashinRepository;

        public EndCashinCommandsHandler(
            ILog log,
            IActiveCashinRepository activeCashinRepository)
        {
            _log = log;
            _activeCashinRepository = activeCashinRepository;
        }

        [UsedImplicitly]
        public async Task<CommandHandlingResult> Handle(EndCashinCommand command)
        {
            _log.WriteInfo(nameof(EndCashinCommand), command, "");

            await _activeCashinRepository.TryRemoveAsync(
                command.BlockchainType,
                command.BlockchainDepositWalletAddress,
                command.AssetId,
                command.OperationId);

            return CommandHandlingResult.Ok();
        }
    }
}
