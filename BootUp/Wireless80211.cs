using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading;
using nanoFramework.Networking;

namespace BootUp
{
    internal class Wireless80211
    {
        public static bool IsEnabled()
        {
            var configuration = GetConfiguration();
            return !string.IsNullOrEmpty(configuration.Ssid);
        }

        /// <summary>
        ///     Disable the Wireless station interface.
        /// </summary>
        public static void Disable()
        {
            var configuration = GetConfiguration();
            configuration.Options = Wireless80211Configuration.ConfigurationOptions.None;
            configuration.SaveConfiguration();
        }

        /// <summary>
        ///     Configure and enable the Wireless station interface
        /// </summary>
        /// <param name="ssid"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static bool Configure(string ssid, string password)
        {
            // And we have to force connect once here even for a short time
            var success =
                WifiNetworkHelper.ConnectDhcp(ssid, password, token: new CancellationTokenSource(10000).Token);
            Debug.WriteLine($"Connection is {success}");
            var configuration = GetConfiguration();
            configuration.Options = Wireless80211Configuration.ConfigurationOptions.AutoConnect |
                            Wireless80211Configuration.ConfigurationOptions.Enable;
            configuration.SaveConfiguration();
            return true;
        }

        /// <summary>
        ///     Get the Wireless station configuration.
        /// </summary>
        /// <returns>Wireless80211Configuration object</returns>
        public static Wireless80211Configuration GetConfiguration()
        {
            var ni = GetInterface();
            return Wireless80211Configuration.GetAllWireless80211Configurations()[ni.SpecificConfigId];
        }

        public static NetworkInterface GetInterface()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();

            // Find WirelessAp interface
            foreach (var ni in interfaces)
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                    return ni;
            
            return null;
        }
    }
}