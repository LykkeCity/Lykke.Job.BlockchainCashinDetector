using Common;
using Lykke.AzureStorage.Tables;
using Lykke.Job.BlockchainCashinDetector.Core.Domain;

namespace Lykke.Job.BlockchainCashinDetector.AzureRepositories
{
    public class EnrolledBalanceEntity : AzureTableEntity
    {
        public decimal Balance { get; set; }

        public long Block { get; set; }

        public string BlockchainAssetId { get; set; }

        public string BlockchainType { get; set; }

        public string DepositWalletAddress { get; set; }


        public static string GetPartitionKey(DepositWalletKey key)
        {
            return $"{key.BlockchainType}-{key.BlockchainAssetId}-{key.DepositWalletAddress.CalculateHexHash32(3)}";
        }

        public static string GetRowKey(DepositWalletKey key)
        {
            return key.DepositWalletAddress;
        }
    }
}
