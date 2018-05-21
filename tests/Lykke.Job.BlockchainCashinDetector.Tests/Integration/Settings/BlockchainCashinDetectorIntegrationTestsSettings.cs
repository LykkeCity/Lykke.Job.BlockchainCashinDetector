using JetBrains.Annotations;

namespace Lykke.Job.BlockchainCashinDetector.Tests.Integration.Settings
{
    [UsedImplicitly]
    public class BlockchainCashinDetectorIntegrationTestsSettings
    {
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string AzureStorageConnectionString { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string RabbitMqConnectionString { get; set; }
    }
}
