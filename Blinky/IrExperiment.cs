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
        private const ushort HighSignalDuration = 480;
        private const ushort LowSignalDuration = 240;
        private const byte ClockDivider = 200;

        private readonly ReceiverChannel _receiver;
        private readonly TransmitterChannel _sender;
        private string _data;
        private byte[] _dataBytes;

        private byte[] _payload;

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
                CarrierLowDuration = 5,
                CarrierHighDuration = 5
            };

            _receiver = new ReceiverChannel(19, 500)
            {
                ClockDivider = ClockDivider,
                SourceClock = SourceClock.APB
            };

            _receiver.EnableFilter(true, 210);
            _receiver.SetIdleThresold(2000);
            _receiver.ReceiveTimeout = TimeSpan.FromMilliseconds(10);
            _receiver.Start(true);
        }

        private void SenderThread()
        {
            _sender.AddCommand(new RmtCommand(200 * 7, true, 200, false));
            for (var i = 0; i < 16; i++)
                _sender.AddCommand(new RmtCommand(HighSignalDuration, i % 2 == 0, LowSignalDuration, i % 2 != 0));

            while (true)
            {
                _sender.SendData(_payload, true);
                //_sender.Send(false);

                Thread.Sleep(500);

                var result = _receiver.GetAllItems();

                if (result == null) continue;

                Debug.WriteLine("====================");
                Debug.WriteLine("New Set of commands:");
                for (var i = 0; i < result.Length; i++)
                {
                    var cmd = result[i];
                    Debug.WriteLine($"{cmd.Duration0},{cmd.Level0} / {cmd.Duration1},{cmd.Level1}");
                }
            }
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

            var zeroCommand = ConvertToRmtCommandBytes(LowSignalDuration, true, LowSignalDuration, false);
            var oneCommand = ConvertToRmtCommandBytes(HighSignalDuration, true, LowSignalDuration, false);

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

        public void PrepareData(string data)
        {
            _data = data;
            var dataBytes = Encoding.UTF8.GetBytes(_data);
            _dataBytes = dataBytes;

            var commandBytes = new byte[8 + _dataBytes.Length * CommandBytesPerByte];
            commandBytes[0] = 52; // (200 * 7) % 256;
            commandBytes[1] = 128 + 5; // (200*7 / 256)
            commandBytes[2] = 200;
            commandBytes[3] = 0;

            for (var i = 0; i < _dataBytes.Length; i++)
            {
                var b = _dataBytes[i];
                DebugOutputByte(b);

                var command = (byte[])CommandBytesLookup[b];
                Array.Copy(command, 0, commandBytes, 4 + i * CommandBytesPerByte, CommandBytesPerByte);
            }

            commandBytes[commandBytes.Length - 4] = 200;
            commandBytes[commandBytes.Length - 3] = 128;
            commandBytes[commandBytes.Length - 2] = 50;
            commandBytes[commandBytes.Length - 1] = 0;

            _payload = commandBytes;
        }

        private static void DebugOutputByte(byte b)
        {
            var bits = ConvertToBits(b);
            Debug.Write($"{b}: ");
            for (var j = 0; j < bits.Length; j++)
            {
                Debug.Write($"{bits[j]}");
            }

            Debug.WriteLine(string.Empty);
        }

        public void IrReceiver()
        {
            PrepareData("Hi");

            //var receiver = new Thread(ReceiverThread);
            //receiver.Start();

            var sender = new Thread(SenderThread);
            sender.Start();

            Thread.Sleep(Timeout.Infinite);
        }
    }
}