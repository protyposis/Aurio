using Aurio.FFT;
using Aurio.Resampler;
using System;

namespace Aurio.Test.NetCore
{
    class Program
    {
        static void Main(string[] args)
        {
            FFTFactory.Factory = new Exocortex.FFTFactory();
            ResamplerFactory.Factory = new NAudioWdlResamplerFactory();

            Console.WriteLine("Hello World!");
        }
    }
}
