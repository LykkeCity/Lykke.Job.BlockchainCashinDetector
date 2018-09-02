using System;
using Lykke.Job.BlockchainCashinDetector.Contract;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;
using Lykke.Job.BlockchainOperationsExecutor.Contract;

namespace Lykke.Job.BlockchainCashinDetector.Mappers
{
    public static class MappingExtensions
    {
        public static CashinResult MapToChashinResult(this OperationExecutionErrorCode source)
        {
            switch (source)
            {
                case OperationExecutionErrorCode.Unknown:
                    return CashinResult.Unknown;
                case OperationExecutionErrorCode.AmountTooSmall:
                    return CashinResult.AmountTooSmall;
                default:
                    throw new ArgumentOutOfRangeException(nameof(source), source, null);
            }
        }


        public static CashinErrorCode MapToChashinErrorCode(this CashinResult source)
        {
            switch (source)
            {
                case CashinResult.Unknown:
                    return CashinErrorCode.Unknown;
                case CashinResult.AmountTooSmall:
                    return CashinErrorCode.AmountTooSmall;
                default:
                    throw new ArgumentOutOfRangeException(nameof(source), source, null);
            }
        }
    }
}
