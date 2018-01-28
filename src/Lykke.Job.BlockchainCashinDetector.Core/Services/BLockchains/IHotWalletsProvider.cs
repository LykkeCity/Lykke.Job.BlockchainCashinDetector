namespace Lykke.Job.BlockchainCashinDetector.Core.Services.BLockchains
{
    public interface IHotWalletsProvider
    {
        string GetHotWalletAddress(string blockchainType);
    }
}
