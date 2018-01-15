using System.Net;
using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.BlockchainCashinDetector.Settings.MeSettings
{
    [UsedImplicitly]
    public class IpEndpointSettings
    {
        [TcpCheck("Port")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string InternalHost { get; set; }

        [TcpCheck("Port")]
        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public string Host { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign)]
        public int Port { get; set; }

        public IPEndPoint GetClientIpEndPoint(bool useInternal = false)
        {
            var host = useInternal ? InternalHost : Host;

            if (IPAddress.TryParse(host, out var ipAddress))
                return new IPEndPoint(ipAddress, Port);

            var addresses = Dns.GetHostAddressesAsync(host).Result;
            return new IPEndPoint(addresses[0], Port);
        }
    }
}
