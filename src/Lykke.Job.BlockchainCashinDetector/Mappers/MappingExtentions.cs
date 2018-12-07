using System;
using Lykke.Job.BlockchainOperationsExecutor.Contract;

namespace Lykke.Job.BlockchainCashinDetector.Mappers
{
    public static class MappingExtensions
    {
        public static Core.Domain.CashinErrorCode MapToCashinErrorCode(this OperationExecutionErrorCode source)
        {
            switch (source)
            {
                case OperationExecutionErrorCode.Unknown:
                    return Core.Domain.CashinErrorCode.Unknown;
                case OperationExecutionErrorCode.AmountTooSmall:
                    return Core.Domain.CashinErrorCode.AmountTooSmall;
                case OperationExecutionErrorCode.RebuildingRejected:
                    return Core.Domain.CashinErrorCode.RebuildingRejected;
                default:
                    throw new ArgumentOutOfRangeException(nameof(source), source, null);
            }
        }


        public static Contract.CashinErrorCode MapToCashinErrorCode(this Core.Domain.CashinErrorCode source)
        {
            switch (source)
            {
                case Core.Domain.CashinErrorCode.Unknown:
                    return Contract.CashinErrorCode.Unknown;
                case Core.Domain.CashinErrorCode.RebuildingRejected:
                    return Contract.CashinErrorCode.Unknown; //TODO update contract and consumers
                case Core.Domain.CashinErrorCode.AmountTooSmall:
                    return Contract.CashinErrorCode.AmountTooSmall;
                default:
                    throw new ArgumentOutOfRangeException(nameof(source), source, null);
            }
        }
    }
}
