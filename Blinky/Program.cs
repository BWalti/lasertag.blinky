using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using Blinky.CustomWs2812b;
using nanoFramework.Hardware.Esp32;

namespace Blinky
{
    public class Program
    {
        public static void Main()
        {
            Debug.WriteLine("Hello from nanoFramework!");

            //var pwmThread = new Thread(PwmRgb);
            //pwmThread.Start();
            //PwmRgb();

            //ButtonListener();

            var irExperiment = new IrExperiment();
            irExperiment.IrReceiver();

            //CustomWs2812b();
            //CustomDisplay2dSmiley();
            //CustomDisplay2dRainbow();

            //Ws2812Pixel();
            //TempSensor();
            //HallSensor();
        }

        //private static void PwmRgb()
        //{
        //    var gpioController = new GpioController();
        //    var stopButton = gpioController.OpenPin(4, PinMode.InputPullUp);
            
        //    var rgbLed = new RgbLed(21, 19, 18, true);

        //    double hue = 0;
        //    while (true)
        //    {
        //        var color = RgbColor.FromHsv(hue, 1, 1f);
        //        rgbLed.Set(color);

        //        if (stopButton.Read() == PinValue.High)
        //        {
        //            hue += 0.25;
        //            if (hue >= 360)
        //            {
        //                hue = 0;
        //            }
        //        }

        //        Thread.Sleep(TimeSpan.FromMilliseconds(10));
        //    }
        //}

        //private static void ButtonListener()
        //{
        //    var gpioController = new GpioController();
        //    var buttonPin = gpioController.OpenPin(4, PinMode.InputPullUp);

        //    var rgbButton = new RgbButtonHandler(buttonPin, 21, 19, 18, true);

        //    Thread.Sleep(Timeout.Infinite);

        //    rgbButton.Dispose();
        //}

        //private static void CustomDisplay2dSmiley()
        //{
        //    var display = new Display2d(19, 10, 10, LineMode.OddReverse);

        //    var color = Blinky.CustomWs2812b.RgbColor.FromHsv(100, 1, 0.1f);

        //    for (uint i = 2; i < 8; i++)
        //    {
        //        display.SetPixel(i, 0, color);
        //        display.SetPixel(i, 9, color);

        //        display.SetPixel(0, i, color);
        //        display.SetPixel(9, i, color);
        //    }

        //    display.SetPixel(1, 1, color);
        //    display.SetPixel(1, 8, color);
        //    display.SetPixel(8, 1, color);
        //    display.SetPixel(8, 8, color);

        //    display.SetPixel(3, 2, color);
        //    display.SetPixel(2, 3, color);
        //    display.SetPixel(3, 3, color);
        //    display.SetPixel(4, 3, color);
        //    display.SetPixel(3, 4, color);

        //    display.SetPixel(6, 2, color);
        //    display.SetPixel(5, 3, color);
        //    display.SetPixel(6, 3, color);
        //    display.SetPixel(7, 3, color);
        //    display.SetPixel(6, 4, color);

        //    display.SetPixel(2, 6, color);
        //    display.SetPixel(3, 7, color);
        //    display.SetPixel(4, 7, color);
        //    display.SetPixel(5, 7, color);
        //    display.SetPixel(6, 7, color);
        //    display.SetPixel(7, 6, color);

        //    display.Render();

        //    Thread.Sleep(Timeout.Infinite);
        //}

        private static void CustomDisplay2dRainbow()
        {
            uint height = 10;
            uint width = 10;

            var display = new Display2d(new TransmitterChannelWrapper(19), width, height, LineMode.OddReverse);

            var colors = new RgbColor[width];
            for (var i = 0; i < width; i++)
            {
                colors[i] = RgbColor.FromHsv(i * 360.0 / width, 1, 0.1f);
            }

            for (uint x = 0; x < width; x++)
            {
                var color = colors[x % width];
                for (uint y = 0; y < height; y++)
                {
                    display.SetPixel(x, y, color);
                }
            }

            display.Render();

            while (true)
            {
                display.ShiftRight();
                
                Thread.Sleep(25);
            }
        }

        

        //private static void CustomWs2812b()
        //{
        //    const uint pixelCount = 100;
        //    var pc = new PixelController(19, pixelCount);

        //    for (short i = 0; i < pixelCount; i++)
        //    {
        //        pc.SetHsvColor(i, i * 360.0 / pixelCount,1,0.1f);
        //    }

        //    pc.Update();

        //    while (true)
        //    {
        //        Thread.Sleep(100);
        //        pc.MovePixelsByStep(1);
        //        pc.SendCurrentCommandData();
        //    }
        //}

        //private static void Ws2812Pixel()
        //{
        //    var width = 2;
        //    var neo = new Ws2812b(19, width);

        //    Rainbow(neo, width);

        //    Thread.Sleep(Timeout.Infinite);
        //}

        //private static void Rainbow(Ws28xx neo, int count, int iterations = 1)
        //{
        //    var img = neo.Image;
        //    for (var i = 0; i < 255 * iterations; i++)
        //    {
        //        for (var j = 0; j < count; j++) img.SetPixel(j, 0, Wheel((i + j) & 255));

        //        neo.Update();

        //        Thread.Sleep(100);
        //    }
        //}

        //private static RgbColor Wheel(int position)
        //{
        //    if (position < 85) return RgbColor.FromArgb(position * 3, 255 - position * 3, 0);

        //    if (position < 170)
        //    {
        //        position -= 85;
        //        return RgbColor.FromArgb(255 - position * 3, 0, position * 3);
        //    }

        //    position -= 170;
        //    return RgbColor.FromArgb(0, position * 3, 255 - position * 3);
        //}
    }
}