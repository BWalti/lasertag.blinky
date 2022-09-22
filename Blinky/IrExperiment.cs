using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Threading;
using nanoFramework.Hardware.Esp32.Rmt;

namespace Blinky
{
    public class IrExperiment
    {
        /// <summary>
        ///     Number of command bytes per byte calculated as:
        ///     - 8 bits per Byte
        ///     - per bit a command = 4 bytes (high / low with duration + signal level)
        /// </summary>
        private const byte CommandBytesPerByte = BitsPerByte * CommandBytesPerBit;

        private const byte BitsPerByte = 8;
        private const byte CommandBytesPerBit = 4;

        private static readonly ArrayList CommandBytesLookup;

        private const uint SourceClockHz = 80000000;
        private const ushort CarrierHz = 40000;
        private const byte ClockDivider = 200;
        private const float TickDurationMicroSecond = 1000000 / (SourceClockHz / (float)ClockDivider);
        
        private const ushort HighSignalDuration = (ushort)(500 / TickDurationMicroSecond);
        private const ushort LowSignalDuration = (ushort)(250 / TickDurationMicroSecond);
        private const ushort PauseSignalDuration = (ushort)(250 / TickDurationMicroSecond);

        private const ushort InitialPulseDuration0 = 200 * 7;
        private const ushort InitialPulseDuration1 = 200;
        private const ushort Margin = 30;
        
        private readonly ReceiverChannel _receiver;
        private readonly TransmitterChannel _sender;
        private byte[] _dataBytes;

        private byte[] _payload;
        private byte[] _initialPulseCommandBytes;

        static IrExperiment()
        {
            var bitLookup = BuildUpByteLookup();
            CommandBytesLookup = BuildCommandBytesLookup(bitLookup);
        }

        public IrExperiment()
        {
            _sender = new TransmitterChannel(21)
            {
                IsChannelIdle = true,
                IdleLevel = false,

                SourceClock = SourceClock.APB,
                ClockDivider = ClockDivider,

                CarrierEnabled = true,
                CarrierLevel = true,
                CarrierLowDuration = (ushort)(SourceClockHz / (2 * CarrierHz)), // 50% of Carrier
                CarrierHighDuration = (ushort)(SourceClockHz / (2 * CarrierHz)) // 50% of Carrier
            };

            _receiver = new ReceiverChannel(23, 30 * BitsPerByte)
            {
                SourceClock = SourceClock.APB,
                ClockDivider = ClockDivider
            };

            _receiver.EnableFilter(true, 200);
            _receiver.SetIdleThresold(2000*9);
            _receiver.ReceiveTimeout = TimeSpan.FromMilliseconds(10);
            _receiver.Start(true);
        }

        private void SenderThread()
        {
            byte i = 0;
            while (true)
            {
                PrepareData(1,1, i++);
                _sender.SendData(_payload, true);
                Thread.Sleep(1000);

                i %= 20;
            }
        }

        private void ReceiverThread()
        {
            while (true)
            {
                Thread.Sleep(300);
                
                var result = _receiver.GetAllItems();
                if (result == null || result.Length < 8) continue;

                var initialPulse = result[0];
                if (initialPulse.Duration0 < InitialPulseDuration0 - Margin ||
                    initialPulse.Duration0 > InitialPulseDuration0 + Margin)
                    continue;
                
                var payloadLength = result.Length - 1;
                var remainder = payloadLength % 8;
                if (remainder != 0)
                {
                    continue;
                }

                var bytes = new byte[payloadLength / 8];
                if (bytes.Length == 3 && TryParseSignal(result, bytes))
                {
                    Debug.WriteLine($"Source: {bytes[0]}, Attack: {bytes[1]}, Counter: {bytes[2]}");
                }
            }
        }

        private static bool TryParseSignal(RmtCommand[] result, byte[] bytes)
        {
            var payloadLength = result.Length - 1;
            for (var i = 0; i < payloadLength; i++)
            {
                var cmd = result[1 + i];
                if (cmd.Duration0 > LowSignalDuration - Margin && cmd.Duration0 < LowSignalDuration + Margin)
                {
                    // 0 bit:
                    // do nothing, as per default all bits are zero
                }
                else if (cmd.Duration0 > HighSignalDuration - Margin && cmd.Duration0 < HighSignalDuration + Margin)
                {
                    // 1 bit:
                    var bitIndex = i % 8;
                    var byteIndex = i / 8;
                    bytes[byteIndex] |= (byte)(1 << (7 - bitIndex));
                }
                else
                {
                    // cannot parse!
                    return false;
                }
            }

            return true;
        }

        private static byte[] ConvertToRmtCommandBytes(ushort duration0, bool level0, ushort duration1, bool level1)
        {
            var result = new byte[4];
            if (duration0 <= 255)
            {
                result[0] = (byte)duration0;
                result[1] = (byte)(level0 ? 128 : 0);
            }
            else
            {
                var remainder = duration0 % 256;
                result[0] = (byte)remainder;
                result[1] = (byte)((level0 ? 128 : 0) + (byte)(duration0 / 256));
            }

            if (duration1 <= 255)
            {
                result[2] = (byte)duration1;
                result[3] = (byte)(level1 ? 128 : 0);
            }
            else
            {
                var remainder = duration1 % 256;
                result[2] = (byte)remainder;
                result[3] = (byte)((level1 ? 128 : 0) + (byte)(duration1 / 256));
            }

            return result;
        }

        private static ArrayList BuildCommandBytesLookup(IList bitLookup)
        {
            var result = new ArrayList();

            var zeroCommand = ConvertToRmtCommandBytes(LowSignalDuration, true, PauseSignalDuration, false);
            var oneCommand = ConvertToRmtCommandBytes(HighSignalDuration, true, PauseSignalDuration, false);

            for (byte b = 0; b < 255; b++)
            {
                var commandBytes = new byte[CommandBytesPerByte];
                var bits = (byte[])bitLookup[b];

                for (var i = 0; i < BitsPerByte; i++)
                    Array.Copy(bits[i] == 1 ? oneCommand : zeroCommand, 0, commandBytes, i * CommandBytesPerBit,
                        CommandBytesPerBit);

                result.Add(commandBytes);
            }

            return result;
        }

        private static ArrayList BuildUpByteLookup()
        {
            var result = new ArrayList();

            for (byte b = 0; b < 255; b++)
            {
                var bits = ConvertToBits(b);
                result.Add(bits);
            }

            return result;
        }

        private static byte[] ConvertToBits(byte b)
        {
            var bits = new byte[8];

            for (var i = 0; i < BitsPerByte; i++)
            {
                bits[i] = (byte)((b & (1u << 7)) != 0 ? 1 : 0);
                b <<= 1;
            }

            return bits;
        }

        public void PrepareData(byte source, byte attack, byte shotCounter)
        {
            _dataBytes = new[] { source, attack, shotCounter };
            
            var commandBytes = new byte[4 + _dataBytes.Length * CommandBytesPerByte];
            Array.Copy(_initialPulseCommandBytes, commandBytes, 4);

            for (var i = 0; i < _dataBytes.Length; i++)
            {
                var b = _dataBytes[i];
                var command = (byte[])CommandBytesLookup[b];
                Array.Copy(command, 0, commandBytes, 4 + i * CommandBytesPerByte, CommandBytesPerByte);
            }

            _payload = commandBytes;
        }

        public void IrReceiver()
        {
            _initialPulseCommandBytes = ConvertToRmtCommandBytes(InitialPulseDuration0, true, InitialPulseDuration1, false);

            var receiver = new Thread(ReceiverThread);
            receiver.Start();

            var sender = new Thread(SenderThread);
            sender.Start();

            Thread.Sleep(Timeout.Infinite);
        }
    }
}