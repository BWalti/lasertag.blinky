using System;
using System.Diagnostics;

namespace Blinky.CustomWs2812b
{
    public class Display2d {
        private readonly PixelController _pixelController;

        public uint Width { get; }
        public uint Height { get; }
        public LineMode LineMode { get; }

        private readonly RgbColor[] _image;

        public Display2d(ITransmitterChannelWrapper transmitter, uint width, uint height, LineMode lineMode = LineMode.AllChained)
        {
            Width = width;
            Height = height;
            LineMode = lineMode;

            _image = new RgbColor[width * height];
            _pixelController = new PixelController(transmitter, width * height);
        }

        public void SetPixel(uint x, uint y, RgbColor rgbColor)
        {
            _image[x + y * Width] = rgbColor;
        }

        public void ShiftRight(int steps = 1)
        {
            switch (LineMode)
            {
                case LineMode.AllChained:
                    for (var y = 0; y < Height; y ++)
                    {
                        _pixelController.ShiftRight((int)(y * Width), (int)Width, steps);
                    }
                    return;

                case LineMode.AllReverse:
                    for (var y = 0; y < Height; y ++)
                    {
                        _pixelController.ShiftLeft((int)(y * Width), (int)Width, steps);
                    }
                    return;

                case LineMode.EvenReverse:
                    for (var y = 0; y < Height; y+=2)
                    {
                        _pixelController.ShiftLeft((int)(y * Width), (int)Width, steps);
                        _pixelController.ShiftRight((int)((y + 1) * Width), (int)Width, steps);
                    }
                    return;

                case LineMode.OddReverse:
                    for (var y = 0; y < Height; y += 2)
                    {
                        _pixelController.ShiftRight((int)(y * Width), (int)Width, steps);
                        _pixelController.ShiftLeft((int)((y + 1) * Width), (int)Width, steps);
                    }
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            _pixelController.SendCurrentCommandData();
        }

        public RgbColor[] Render()
        {
            var rendered = TransformToRenderSpace();

            _pixelController.SetAll(rendered);
            
            return rendered;
        }

        private RgbColor[] TransformToRenderSpace()
        {
            RgbColor[] result;
            switch (LineMode)
            {
                case LineMode.AllChained:
                    return _image;

                case LineMode.AllReverse:
                    result = new RgbColor[Width * Height];
                    for (var y = 0; y < Height; y++)
                    {
                        for (var x = 0; x < Width; x++)
                        {
                            result[x + y * Width] = _image[Width - x - 1 + y * Width];
                        }
                    }

                    return result;

                case LineMode.EvenReverse:
                    result = new RgbColor[Width * Height];

                    // copy everything, then fix the "even lines":
                    Array.Copy(_image, result, _image.Length);

                    for (var y = 0; y < Height; y+=2)
                    {
                        for (var x = 0; x < Width; x++)
                        {
                            result[x + y * Width] = _image[Width - x - 1 + y * Width];
                        }
                    }

                    return result;

                case LineMode.OddReverse:
                    result = new RgbColor[Width * Height];

                    // copy everything, then fix the "odd lines":
                    Array.Copy(_image, result, _image.Length);

                    for (var y = 1; y < Height; y+=2)
                    {
                        for (var x = 0; x < Width; x++)
                        {
                            result[x + y * Width] = _image[Width - x - 1 + y * Width];
                        }
                    }

                    return result;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}