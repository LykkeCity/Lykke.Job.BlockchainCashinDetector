using System;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.Job.BlockchainCashinDetector.Workflow.Events;
using ProtoBuf;

namespace Lykke.Job.BlockchainCashinDetector.Workflow.Commands
{
    /// <summary>
    /// Command to start the cashin.
    /// The main thing, why this command is distinguished - to
    /// move the saga from the <see cref="CashinState.Starting"/> state,
    /// to ignore new <see cref="DepositBalanceDetectedEvent"/> while cashin
    /// for the given deposit wallet and asset id is in-progress and to not
    /// clog process pipeline with the same balance detection messages
    /// </summary>
    [ProtoContract]
    public class StartCashinCommand
    {
        [ProtoMember(1)]
        public Guid OperationId { get; set; }
    }
}
