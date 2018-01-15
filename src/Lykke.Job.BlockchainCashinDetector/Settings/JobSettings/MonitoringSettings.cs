using System;
using JetBrains.Annotations;

namespace Lykke.Job.BlockchainCashinDetector.Settings.JobSettings
{
    [UsedImplicitly]
    public class MonitoringSettings
    {
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public TimeSpan Period { get; set; }
    }
}
