using System;
using System.Device.Gpio;
using Blinky.CustomWs2812b;

namespace Blinky
{
    public class RgbButtonHandler : IDisposable
    {
        private readonly RgbColor _color1;
        private readonly RgbColor _color2;
        private readonly RgbLed _rgbLed;
        private readonly GpioPin _buttonPin;

        public RgbButtonHandler(GpioPin buttonPin, int red, int green, int blue, bool isInverted)
        {
            _buttonPin = buttonPin;
            _buttonPin.DebounceTimeout = TimeSpan.FromMilliseconds(5);
            _buttonPin.ValueChanged += ButtonPinOnValueChanged;
            
            _rgbLed = new RgbLed(red, green, blue, isInverted);
            _rgbLed.TurnOff();

            _color1 = RgbColor.FromHsv(0, 1, 0.1f);
            _color2 = RgbColor.FromHsv(180, 1, 0.1f);
        }

        private void ButtonPinOnValueChanged(object sender, PinValueChangedEventArgs e)
        {
            if (e.ChangeType == PinEventTypes.Rising)
            {
                _rgbLed.Set(_color1);
            }
            else
            {
                _rgbLed.Set(_color2);
            }
        }

        public void Dispose()
        {
            _buttonPin.ValueChanged -= ButtonPinOnValueChanged;
            _buttonPin.Dispose();
            _rgbLed.TurnOff();
        }
    }
}