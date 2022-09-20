using System.Device.Pwm;
using nanoFramework.Hardware.Esp32;

namespace SimpleButton
{
    public class RgbLed
    {
        private readonly PwmChannel _blue;
        private readonly PwmChannel _green;
        private readonly bool _isInverted;
        private readonly PwmChannel _red;

        public RgbLed(int gpioPinRed, int gpioPinGreen, int gpioPinBlue, bool isInverted)
        {
            _isInverted = isInverted;

            // RGB LED (flat to round):
            // Red: 10 kOhm
            // Positive
            // Green: 10 kOhm
            // Blue: 2.7 kOhm

            _red = CreatePwmPin(gpioPinRed);
            _green = CreatePwmPin(gpioPinGreen);
            _blue = CreatePwmPin(gpioPinBlue);
        }

        public void Set(HsvColor c)
        {
            var redFraction = c.R / (double)255;
            var greenFraction = c.G / (double)255;
            var blueFraction = c.B / (double)255;

            if (_isInverted)
            {
                _red.DutyCycle = 1 - redFraction;
                _green.DutyCycle = 1 - greenFraction;
                _blue.DutyCycle = 1 - blueFraction;
            }
            else
            {
                _red.DutyCycle = redFraction;
                _green.DutyCycle = greenFraction;
                _blue.DutyCycle = blueFraction;
            }
        }

        public void TurnOff()
        {
            if (_isInverted)
            {
                _red.DutyCycle = _green.DutyCycle = _blue.DutyCycle = 1;
            }
            else
            {
                _red.DutyCycle = _green.DutyCycle = _blue.DutyCycle = 0;
            }
        }

        private static PwmChannel CreatePwmPin(int pin, int frequency = 40000, double dutyCyclePercentage = 0)
        {
            Configuration.SetPinFunction(pin, DeviceFunction.PWM1);
            var pwmPin = PwmChannel.CreateFromPin(pin, frequency, dutyCyclePercentage);
            pwmPin.Start();

            return pwmPin;
        }
    }
}