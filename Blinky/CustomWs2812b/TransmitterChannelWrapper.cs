using nanoFramework.Hardware.Esp32.Rmt;

namespace Blinky.CustomWs2812b
{
    public class TransmitterChannelWrapper : ITransmitterChannelWrapper
    {
        // 80MHz / 4 => min pulse 0.00us
        protected const byte ClockDivider = 2;

        // one pulse duration in us
        // ReSharper disable once PossibleLossOfFraction
        protected const float MinPulse = 1000000.0f / (80000000 / ClockDivider);

        // default data sheet values
        private readonly RmtCommand
            _onePulse = new((ushort)(0.8 / MinPulse), true, (ushort)(0.45 / MinPulse), false);

        private readonly RmtCommand
            _resetCommand = new((ushort)(15 / MinPulse), false, (ushort)(16 / MinPulse), false);

        private readonly RmtCommand _zeroPulse = new((ushort)(0.4 / MinPulse), true, (ushort)(0.85 / MinPulse),
            false);

        private readonly TransmitterChannel _transmitterChannel;


        public TransmitterChannelWrapper(int gpioPin)
        {
            _transmitterChannel = new TransmitterChannel(gpioPin)
            {
                CarrierEnabled = false,
                ClockDivider = ClockDivider
            };
        }

        public void Dispose()
        {
            _transmitterChannel.Dispose();
        }

        public byte OnePulseDuration0 => (byte)_onePulse.Duration0;
        public byte OnePulseDuration1 => (byte)_onePulse.Duration1;
        public byte ZeroPulseDuration0 => (byte)_zeroPulse.Duration0;
        public byte ZeroPulseDuration1 => (byte)_zeroPulse.Duration1;

        public void SendData(byte[] data, bool waitTxDone)
        {
            _transmitterChannel.SendData(data, waitTxDone);
        }

        public void ClearCommands()
        {
            _transmitterChannel.ClearCommands();
        }

        public void AddReset()
        {
            _transmitterChannel.AddCommand(_resetCommand);
        }

        public void AddOnePulse()
        {
            _transmitterChannel.AddCommand(_onePulse);
        }

        public void AddZeroPulse()
        {
            _transmitterChannel.AddCommand(_zeroPulse);
        }

        public void Send(bool waitTxDone)
        {
            _transmitterChannel.Send(waitTxDone);
        }
    }
}