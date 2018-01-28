using System.Collections.Generic;
using JetBrains.Annotations;
using Lykke.Job.BlockchainCashinDetector.Core.Domain.Health;
using Lykke.Job.BlockchainCashinDetector.Core.Services;

namespace Lykke.Job.BlockchainCashinDetector.Services
{
    // NOTE: See https://lykkex.atlassian.net/wiki/spaces/LKEWALLET/pages/35755585/Add+your+app+to+Monitoring
    [UsedImplicitly]
    public class HealthService : IHealthService
    {
        public string GetHealthViolationMessage()
        {
            return null;
        }

        public IEnumerable<HealthIssue> GetHealthIssues()
        {
            var issues = new HealthIssuesCollection();

            return issues;
        }
    }
}
