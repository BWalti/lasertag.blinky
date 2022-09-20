using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.Threading;

namespace SimpleButton
{
    public class RgbButtonHandler : IDisposable
    {
        private readonly HsvColor _fire;
        private readonly RgbLed _rgbLed;
        private readonly GpioPin _buttonPin;

        public RgbButtonHandler(GpioPin buttonPin, int red, int green, int blue, bool isInverted)
        {
            _buttonPin = buttonPin;
            _buttonPin.DebounceTimeout = TimeSpan.FromMilliseconds(50);
            _buttonPin.ValueChanged += ButtonPinOnValueChanged;

            _rgbLed = new RgbLed(red, green, blue, isInverted);
            _rgbLed.TurnOff();

            _fire = HsvColor.FromHsv(0, 1, 1f);
        }

        private void ButtonPinOnValueChanged(object sender, PinValueChangedEventArgs e)
        {
            Debug.WriteLine($"Got a Button changed event: {e.ChangeType}");

            switch (e.ChangeType)
            {
                case PinEventTypes.Falling:
                    _rgbLed.Set(_fire);
                    Thread.Sleep(TimeSpan.FromMilliseconds(50));
                    _rgbLed.TurnOff();
                    break;

                case PinEventTypes.Rising:
                case PinEventTypes.None:
                default:
                    _rgbLed.TurnOff();
                    break;
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