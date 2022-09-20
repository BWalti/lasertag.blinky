using System.Net.NetworkInformation;

namespace BootUp
{
    public static class WirelessAp
    {
        public const string SoftApIp = "192.168.4.1";

        /// <summary>
        ///     Disable the Soft AP for next restart.
        /// </summary>
        public static void Disable()
        {
            var configuration = GetConfiguration();
            configuration.Options = WirelessAPConfiguration.ConfigurationOptions.None;
            configuration.SaveConfiguration();
        }

        /// <summary>
        ///     Set-up the Wireless AP settings, enable and save
        /// </summary>
        /// <returns>True if already set-up</returns>
        public static bool Setup()
        {
            var ni = GetInterface();
            var configuration = GetConfiguration();

            // Check if already Enabled and return true
            if (configuration.Options == (WirelessAPConfiguration.ConfigurationOptions.Enable |
                                    WirelessAPConfiguration.ConfigurationOptions.AutoStart) &&
                ni.IPv4Address == SoftApIp)
                return true;

            // Set up IP address for Soft AP
            ni.EnableStaticIPv4(SoftApIp, "255.255.255.0", SoftApIp);

            // Set Options for Network Interface
            //
            // Enable    - Enable the Soft AP ( Disable to reduce power )
            // AutoStart - Start Soft AP when system boots.
            // HiddenSSID- Hide the SSID
            //
            configuration.Options = WirelessAPConfiguration.ConfigurationOptions.AutoStart |
                              WirelessAPConfiguration.ConfigurationOptions.Enable;

            // Set the SSID for Access Point. If not set will use default  "nano_xxxxxx"
            configuration.Ssid = "Lasertag";

            // Maximum number of simultaneous connections, reserves memory for connections
            configuration.MaxConnections = 5;

            // To set-up Access point with no Authentication
            configuration.Authentication = AuthenticationType.Open;
            configuration.Password = "";

            // To set up Access point with Authentication. Password minimum 8 chars.
            //configuration.Authentication = AuthenticationType.WPA2;
            //configuration.Password = "password";

            // Save the configuration so on restart Access point will be running.
            configuration.SaveConfiguration();

            return false;
        }

        /// <summary>
        ///     Find the Wireless AP configuration
        /// </summary>
        /// <returns>Wireless AP configuration or NUll if not available</returns>
        public static WirelessAPConfiguration GetConfiguration()
        {
            var ni = GetInterface();
            return WirelessAPConfiguration.GetAllWirelessAPConfigurations()[ni.SpecificConfigId];
        }

        public static NetworkInterface GetInterface()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();

            // Find WirelessAp interface
            foreach (var ni in interfaces)
                if (ni.NetworkInterfaceType == NetworkInterfaceType.WirelessAP)
                    return ni;
            return null;
        }

        /// <summary>
        ///     Returns the IP address of the Soft AP
        /// </summary>
        /// <returns>IP address</returns>
        public static string GetIpAddress()
        {
            var ni = GetInterface();
            return ni.IPv4Address;
        }
    }
}