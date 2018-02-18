namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public interface IDepositBalanceDetectionsDeduplicationLock
    {
        long Block { get; }

        string BlockchainType { get; }

        string BlockchainAssetId { get; }
        
        string DepositWalletAddress { get; }
    }
}
