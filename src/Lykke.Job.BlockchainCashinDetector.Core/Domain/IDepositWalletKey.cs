namespace Lykke.Job.BlockchainCashinDetector.Core.Domain
{
    public interface IDepositWalletKey
    {
        string BlockchainType { get; }

        string BlockchainAssetId { get; }

        string DepositWalletAddress { get; }
    }
}
