using Common;
using Lykke.AzureStorage.Tables;

namespace Lykke.Job.BlockchainCashinDetector.AzureRepositories
{
    public class EnrolledBalanceEntity : AzureTableEntity
    {
        public decimal Balance { get; set; }

        public long Block { get; set; }

        public string BlockchainAssetId { get; set; }

        public string BlockchainType { get; set; }

        public string DepositWalletAddress { get; set; }


        public static string GetPartitionKey(string blockchainType, string blockchainAssetId, string depositWalletAddress)
        {
            return $"{blockchainType}-{blockchainAssetId}-{depositWalletAddress.CalculateHexHash32(3)}";
        }

        public static string GetRowKey(string depositWalletAddress)
        {
            return depositWalletAddress;
        }
    }
}
