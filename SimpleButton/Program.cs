using System.Device.Gpio;
using System.Diagnostics;
using System.Threading;

namespace SimpleButton
{
    public class Program
    {
        public static void Main()
        {
            Debug.WriteLine("Hello from nanoFramework!");

            var gpioController = new GpioController();
            var buttonPin = gpioController.OpenPin(4, PinMode.InputPullUp);

            var rgbButton = new RgbButtonHandler(buttonPin, 21, 19, 18, true);

            Thread.Sleep(Timeout.Infinite);

            rgbButton.Dispose();

            // Browse our samples repository: https://github.com/nanoframework/samples
            // Check our documentation online: https://docs.nanoframework.net/
            // Join our lively Discord community: https://discord.gg/gCyBu8T
        }
    }
}