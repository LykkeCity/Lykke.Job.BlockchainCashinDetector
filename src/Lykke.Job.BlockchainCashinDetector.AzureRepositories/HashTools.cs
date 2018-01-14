using System.Linq;
using Common;

namespace Lykke.Job.BlockchainCashinDetector.AzureRepositories
{
    internal static class HashTools
    {
        public static string GetPartitionKeyHash(string value)
        {
            var sum = value.ToUtf8Bytes().Sum(b => b);

            return (sum & 0xFFF).ToString("X3");
        }
    }
}
