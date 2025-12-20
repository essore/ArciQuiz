using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Web.Services
{
    public interface ILanAddressService
    {
        string? GetLanIPv4();
    }

    public sealed class LanAddressService : ILanAddressService
    {
        public string? GetLanIPv4()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;

                // Evita virtuali/tunnel se vuoi essere aggressivo:
                if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                    continue;

                var ipProps = ni.GetIPProperties();
                foreach (var ua in ipProps.UnicastAddresses)
                {
                    if (ua.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;

                    var ip = ua.Address;

                    if (IPAddress.IsLoopback(ip))
                        continue;

                    // Accetta solo privati (192.168.x.x / 10.x.x.x / 172.16-31.x.x)
                    if (IsPrivateIPv4(ip))
                        return ip.ToString();
                }
            }

            return null;
        }

        private static bool IsPrivateIPv4(IPAddress ip)
        {
            var b = ip.GetAddressBytes();
            return
                b[0] == 10 ||
                (b[0] == 172 && b[1] is >= 16 and <= 31) ||
                (b[0] == 192 && b[1] == 168);
        }
    }

}
