using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using nanoFramework.Hardware.Esp32;

namespace Blinky
{
    public class IrExperimentCom
    {
        private readonly GpioPin _buttonPin;
        private readonly SerialPort _serialDevice;

        public IrExperimentCom()
        {
            var gpioController = new GpioController();

            Configuration.SetPinFunction(19, DeviceFunction.COM2_RX);
            Configuration.SetPinFunction(21, DeviceFunction.COM2_TX);
            _serialDevice = new SerialPort("COM2")
            {
                BaudRate = 9600,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                DataBits = 8,
                //NewLine = "\n",
                //WatchChar = '\n',
                //Mode = SerialMode.RS485,
                WriteBufferSize = 500,
                ReadBufferSize = 500
            };
            _serialDevice.Open();
            _serialDevice.DataReceived += SerialDeviceOnDataReceived;

            _buttonPin = gpioController.OpenPin(4, PinMode.InputPullDown);
            _buttonPin.DebounceTimeout = TimeSpan.FromMilliseconds(10);
            _buttonPin.ValueChanged += ButtonClicked;
        }

        private void SerialDeviceOnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var serialPort = (SerialPort)sender;

            var bytesToRead = serialPort.BytesToRead;
            if (bytesToRead > 0)
            {
                Debug.WriteLine($"Got {bytesToRead}# bytes to read!");
                var bytes = new byte[bytesToRead];
                serialPort.Read(bytes, 0, bytesToRead);
                Debug.WriteLine($"Bytes read: {bytes}");
            }
        }

        public void SendData(string data)
        {
            _serialDevice.Write(data);
        }

        public void IrReceiver()
        {
            Thread.Sleep(Timeout.Infinite);
        }

        private void ButtonClicked(object sender, PinValueChangedEventArgs e)
        {
            Debug.WriteLine($"Going to send message through IR! {DateTime.UtcNow}");
            SendData("Hello World!");
        }
    }
}