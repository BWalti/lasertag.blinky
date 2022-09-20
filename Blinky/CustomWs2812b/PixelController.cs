using System;

namespace Blinky.CustomWs2812b
{
    public class PixelController : IDisposable
    {
        private const int BytesPerPixel = 3 * 8 * 4;

        /// <summary>
        ///     Every Pixel contains 3 colors, which are represented by 3 Bytes = 3 * 8 bits.
        ///     Every bit is represented as command.
        ///     Every command contains 4 bytes the first 2 bytes are for the high signal and the second 2 for the low.
        ///     In every signal pair the first is the duration of the signal and the second is the level of the signal high/low.
        /// </summary>
        /// <remarks>
        ///     Thus the total amount of bytes per pixel is 3 (RGB) * 8 (Bytes) * 4 (high/low with durations).
        /// </remarks>
        private byte[] _binaryCommandData;

        private readonly RgbColor[] _pixels;

        protected ITransmitterChannelWrapper Transmitter;

        public PixelController(ITransmitterChannelWrapper transmitterChannel, uint pixelCount)
        {
            Transmitter = transmitterChannel;

            _pixels = new RgbColor[pixelCount];

            for (uint i = 0; i < pixelCount; ++i) _pixels[i] = RgbColor.Black;

            //Populate all bytes of the binaryCommand array that will not change during update operations. Just for faster performance on updating pixels
            _binaryCommandData = InitializeCommandData(_pixels.Length);
        }

        public void Dispose()
        {
            Transmitter.Dispose();
        }

        private byte[] InitializeCommandData(int pixelCount)
        {
            var bytes = new byte[pixelCount * BytesPerPixel + 4];
            for (var i = 1; i <= bytes.Length - 4 - 3; i += 4)
            {
                bytes[i] = 128;
                bytes[i + 2] = 0;
            }

            return bytes;
        }

        /// <summary>
        ///     Set specific rgbColor (HSV format) on pixel at position
        /// </summary>
        /// <param name="index"></param>
        /// <param name="hue"></param>
        /// <param name="saturation"></param>
        /// <param name="value"></param>
        public void SetHsvColor(short index, double hue, float saturation, float value)
        {
            _pixels[index] = RgbColor.FromHsv(hue, saturation, value);
            UpdatePixelBinary(index);
        }

        /// <summary>
        ///     Set specific rgbColor (RGB Format) on pixel at position
        /// </summary>
        public void SetColor(short index, RgbColor rgbColor)
        {
            _pixels[index] = rgbColor;
            UpdatePixelBinary(index);
        }

        public void SetAll(RgbColor[] colors)
        {
            if (colors == null || _pixels.Length != colors.Length)
                throw new ArgumentOutOfRangeException("colors", "The array needs to have the same size as _pixels.");

            Array.Copy(colors, _pixels, colors.Length);
            Update();
        }

        public void ShiftRight(int startIndex, int length, int steps = 1)
        {
            var saved = new RgbColor[steps];
            Array.Copy(_pixels, startIndex + length - steps, saved, 0, steps);
            Array.Copy(_pixels, startIndex, _pixels, startIndex + steps, length - steps);
            Array.Copy(saved, 0, _pixels, startIndex, steps);

            var savedCommandData = new byte[BytesPerPixel * steps];
            Array.Copy(_binaryCommandData, (startIndex + length - steps) * BytesPerPixel, savedCommandData, 0,
                steps * BytesPerPixel);
            Array.Copy(_binaryCommandData, startIndex * BytesPerPixel, _binaryCommandData,
                (startIndex + steps) * BytesPerPixel, (length - steps) * BytesPerPixel);
            Array.Copy(savedCommandData, 0, _binaryCommandData, startIndex * BytesPerPixel, steps * BytesPerPixel);
        }

        public void ShiftLeft(int startIndex, int length, int steps = 1)
        {
            var saved = new RgbColor[steps];
            Array.Copy(_pixels, startIndex, saved, 0, steps);
            Array.Copy(_pixels, startIndex + steps, _pixels, startIndex, length - steps);
            Array.Copy(saved, 0, _pixels, startIndex + length - steps, steps);

            var savedCommandData = new byte[BytesPerPixel * steps];
            Array.Copy(_binaryCommandData, startIndex * BytesPerPixel, savedCommandData, 0, steps * BytesPerPixel);
            Array.Copy(_binaryCommandData, (startIndex + steps) * BytesPerPixel, _binaryCommandData,
                startIndex * BytesPerPixel, (length - steps) * BytesPerPixel);
            Array.Copy(savedCommandData, 0, _binaryCommandData, (startIndex + length - steps) * BytesPerPixel,
                steps * BytesPerPixel);
        }

        /// <summary>
        ///     Set specific rgbColor (RGB Format) on pixel at position
        /// </summary>
        /// <param name="index"></param>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        public void SetColor(short index, byte r, byte g, byte b)
        {
            _pixels[index] = new RgbColor(r, g, b);
            UpdatePixelBinary(index);
        }

        /// <summary>
        ///     Sending current command data.
        /// </summary>
        public void SendCurrentCommandData()
        {
            Transmitter.SendData(_binaryCommandData, false);
        }

        /// <summary>
        ///     Update command data for all pixels and send them.
        /// </summary>
        public void Update()
        {
            for (short index = 0; index < _pixels.Length; ++index) UpdatePixelBinary(index);

            SendCurrentCommandData();
        }

        public void TurnOff()
        {
            _binaryCommandData = InitializeCommandData(_pixels.Length);
            SendCurrentCommandData();
        }

        private void UpdatePixelBinary(short index)
        {
            var commandData = _pixels[index].GetCommandData(Transmitter);

            var commandIndex = BytesPerPixel * index;
            Array.Copy(commandData, 0, _binaryCommandData, commandIndex, BytesPerPixel);
        }
    }
}