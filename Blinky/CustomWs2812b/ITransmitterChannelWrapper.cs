using System;

namespace Blinky.CustomWs2812b
{
    public interface ITransmitterChannelWrapper : IDisposable
    {
        byte OnePulseDuration0 { get; }
        byte OnePulseDuration1 { get; }
        byte ZeroPulseDuration0 { get; }
        byte ZeroPulseDuration1 { get; }

        void SendData(byte[] data, bool waitTxDone);
    }
}