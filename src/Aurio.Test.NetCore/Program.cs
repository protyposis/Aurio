﻿using System;
using Aurio.FFT;
using Aurio.Resampler;

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