namespace Lykke.Job.BlockchainCashinDetector.Settings.JobSettings
{
    public class BlockchainCashinDetectorSettings
    {
        public DbSettings Db { get; set; }
        public MonitoringSettings Monitoring { get; set; }
        public RequestsSettings Requests { get; set; }
        public CqrsSettings Cqrs { get; set; }
    }
}
