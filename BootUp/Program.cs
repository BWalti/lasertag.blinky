using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using Iot.Device.DhcpServer;
using nanoFramework.Networking;
using nanoFramework.Runtime.Native;

namespace BootUp
{
    public class Program
    {
        // GPIO pin used to put device into AP set-up mode
        private const int SetupPin = 5;

        // Start Simple WebServer
        private static readonly WebServer Server = new();

        // Connected Station count
        private static int _connectedCount;

        public static void Main()
        {
            Debug.WriteLine("Welcome to WiFI Soft AP world!");

            var gpioController = new GpioController();
            var setupButton = gpioController.OpenPin(SetupPin, PinMode.InputPullUp);

            // If Wireless station is not enabled then start Soft AP to allow Wireless configuration
            // or Button pressed
            if (!Wireless80211.IsEnabled() || setupButton.Read() == PinValue.High)
            {
                Wireless80211.Disable();
                if (WirelessAp.Setup() == false)
                {
                    // Reboot device to Activate Access Point on restart
                    Debug.WriteLine("Setup Soft AP, Rebooting device");
                    Power.RebootDevice();
                }

                var dhcpServer = new DhcpServer
                {
                    CaptivePortalUrl = $"http://{WirelessAp.SoftApIp}"
                };
                var initResult = dhcpServer.Start(IPAddress.Parse(WirelessAp.SoftApIp),
                    new IPAddress(new byte[] { 255, 255, 255, 0 }));
                if (!initResult) Debug.WriteLine("Error initializing DHCP server.");

                Debug.WriteLine("Running Soft AP, waiting for client to connect");
                Debug.WriteLine($"Soft AP IP address :{WirelessAp.GetIpAddress()}");

                // Link up Network event to show Stations connecting/disconnecting to Access point.
                NetworkChange.NetworkAPStationChanged += NetworkChange_NetworkAPStationChanged;
                // Now that the normal Wifi is deactivated, that we have setup a static IP
                // We can start the Web server
                Server.Start();
            }
            else
            {
                Debug.WriteLine("Running in normal mode, connecting to Access point");
                var conf = Wireless80211.GetConfiguration();

                bool success;

                // For devices like STM32, the password can't be read
                if (string.IsNullOrEmpty(conf.Password))
                    // In this case, we will let the automatic connection happen
                    success = WifiNetworkHelper.Reconnect(true, token: new CancellationTokenSource(60000).Token);
                else
                    // If we have access to the password, we will force the reconnection
                    // This is mainly for ESP32 which will connect normaly like that.
                    success = WifiNetworkHelper.ConnectDhcp(conf.Ssid, conf.Password, requiresDateTime: true,
                        token: new CancellationTokenSource(60000).Token);

                Debug.WriteLine($"Connection is {success}");
                Debug.WriteLine(success
                    ? $"We have a valid date: {DateTime.UtcNow}"
                    : "Something wrong happened, can't connect at all");
            }


            // Just wait for now
            // Here you would have the reset of your program using the client WiFI link
            Thread.Sleep(Timeout.Infinite);
        }

        /// <summary>
        ///     Event handler for Stations connecting or Disconnecting
        /// </summary>
        /// <param name="networkIndex">The index of Network Interface raising event</param>
        /// <param name="e">Event argument</param>
        private static void NetworkChange_NetworkAPStationChanged(int networkIndex, NetworkAPStationEventArgs e)
        {
            Debug.WriteLine(
                $"NetworkAPStationChanged event Index:{networkIndex} Connected:{e.IsConnected} Station:{e.StationIndex} ");

            // if connected then get information on the connecting station 
            if (e.IsConnected)
            {
                var wapconf = WirelessAPConfiguration.GetAllWirelessAPConfigurations()[0];
                var station = wapconf.GetConnectedStations(e.StationIndex);

                var macString = BitConverter.ToString(station.MacAddress);
                Debug.WriteLine($"Station mac {macString} Rssi:{station.Rssi} PhyMode:{station.PhyModes} ");

                _connectedCount++;

                // Start web server when it connects otherwise the bind to network will fail as 
                // no connected network. Start web server when first station connects 
                if (_connectedCount == 1)
                {
                    // Wait for Station to be fully connected before starting web server
                    // other you will get a Network error
                    Thread.Sleep(2000);
                    Server.Start();
                }
            }
            else
            {
                // Station disconnected. When no more station connected then stop web server
                if (_connectedCount > 0)
                {
                    _connectedCount--;
                    if (_connectedCount == 0)
                        Server.Stop();
                }
            }
        }
    }
}