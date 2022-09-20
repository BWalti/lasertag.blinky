using System;

namespace Blinky.CustomWs2812b
{
    public class RgbColor
    {
        public RgbColor(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;

            Bits = CalculateBits(r, g, b);
        }

        private static byte[] CalculateBits(byte r, byte g, byte b)
        {
            var bits = new byte[24];
            //Get all 24 bits of the changed pixel
            for (var i = 0; i < 8; i++)
            {
                bits[i] = (byte)((g & (1u << 7)) != 0 ? 1 : 0);
                g <<= 1;

                bits[i + 8] = (byte)((r & (1u << 7)) != 0 ? 1 : 0);
                r <<= 1;

                bits[i + 16] = (byte)((b & (1u << 7)) != 0 ? 1 : 0);
                b <<= 1;
            }

            return bits;
        }

        public byte[] Bits { get; set; }

        public byte B { get; }
        public byte G { get; }
        public byte R { get; }
        
        public static RgbColor Black = new(0, 0, 0);
        private byte[] _commandData;

        /// <summary>
        ///     Creates a color depending on Hue Saturation and Value.
        /// </summary>
        /// <param name="hue">Value between 0 and 360</param>
        /// <param name="saturation">Value between 0 and 1</param>
        /// <param name="value">Value between 0 and 1</param>
        /// <returns>The RgbColor matching the input values.</returns>
        public static RgbColor FromHsv(double hue, double saturation, float value)
        {
            if (hue is < 0 or >= 360)
                throw new ArgumentOutOfRangeException("hue", "Hue must be set to a value: 0 <= hue < 360");

            if (saturation is < 0 or > 1)
                throw new ArgumentOutOfRangeException("saturation",
                    "Saturation must be set to a (percentage) value: 0 <= saturation <= 1");

            if (value is < 0 or > 1)
                throw new ArgumentOutOfRangeException("value",
                    "Value must be set to a (percentage) value: 0 <= value <= 1");

            // Conversion according to: https://www.rapidtables.com/convert/color/hsv-to-rgb.html
            byte red = 0, green = 0, blue = 0;

            var c = value * saturation;
            var x = c * (1 - Math.Abs(hue / 60 % 2 - 1));
            var m = value - c;

            var cmValue = (byte)((c + m) * 255);
            var xmValue = (byte)((x + m) * 255);

            switch (hue)
            {
                case >= 0 and < 60:
                    red = cmValue;
                    green = xmValue;
                    blue = 0;
                    break;
                case >= 60 and < 120:
                    red = xmValue;
                    green = cmValue;
                    blue = 0;
                    break;
                case >= 120 and < 180:
                    red = 0;
                    green = cmValue;
                    blue = xmValue;
                    break;
                case >= 180 and < 240:
                    red = 0;
                    green = xmValue;
                    blue = cmValue;
                    break;
                case >= 240 and < 300:
                    red = xmValue;
                    green = 0;
                    blue = cmValue;
                    break;
                case >= 300 and < 360:
                    red = cmValue;
                    green = 0;
                    blue = xmValue;
                    break;
            }

            return new RgbColor(red, green, blue);
        }

        public byte[] GetCommandData(ITransmitterChannelWrapper transmitter)
        {
            // Here we construct the binary command by setting durations for every bit of the pixel.
            // So we have total of 96 bytes for every pixel.
            
            if (_commandData == null)
            {
                _commandData = new byte[24 * 4];

                for (var i = 0; i < Bits!.Length; i++)
                {
                    _commandData[i * 4 + 1] = 128; // High
                    _commandData[i * 4 + 3] = 0; // Low

                    // durations:
                    if (Bits[i] == 1)
                    {
                        _commandData[i * 4] = transmitter.OnePulseDuration0;
                        _commandData[i * 4 + 2] = transmitter.OnePulseDuration1;
                    }
                    else
                    {
                        _commandData[i * 4] = transmitter.ZeroPulseDuration0;
                        _commandData[i * 4 + 2] = transmitter.ZeroPulseDuration1;
                    }
                }
            }

            return _commandData;
        }
    }
}