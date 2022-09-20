using BenchmarkDotNet.Attributes;
using Blinky.CustomWs2812b;

namespace Benchmark;

public class Display2dBenchmark
{
    private readonly Display2d _display2d;

    public Display2dBenchmark()
    {
        _display2d = new Display2d(new BenchWrapper(), 10, 10);
        for (uint x = 0; x < 10; x++)
        {
            var color = RgbColor.FromHsv(x * 360.0 / 10, 1, 0.1f);
            for (uint y = 0; y < 10; y++)
            {
                _display2d.SetPixel(x, y, color);
            }
        }
    }

    [Benchmark]
    public RgbColor[] ManualMove() => ManualMoveOperation();

    private RgbColor[] ManualMoveOperation()
    {
        for (uint x = 0; x < 10; x++)
        {
            var color = RgbColor.FromHsv(x * 360.0 / 10, 1, 0.1f);
            for (uint y = 0; y < 10; y++)
            {
                _display2d.SetPixel((x+1) % 10, y, color);
            }
        }

        return _display2d.Render();
    }
}

public class BenchWrapper : ITransmitterChannelWrapper
{
    public void Dispose()
    {
    }

    public byte OnePulseDuration0 => 10;
    public byte OnePulseDuration1 => 40;
    public byte ZeroPulseDuration0 => 40;
    public byte ZeroPulseDuration1 => 10;

    public void SendData(byte[] data, bool waitTxDone)
    {
    }

    public void ClearCommands()
    {
    }

    public void AddReset()
    {
    }

    public void AddOnePulse()
    {
    }

    public void AddZeroPulse()
    {
    }

    public void Send(bool waitTxDone)
    {
    }
}