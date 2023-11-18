using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aurio.Resampler;

namespace Aurio.UnitTest
{
    internal class MockResamplerFactory : IResamplerFactory
    {
        public bool CheckRatio(double sampleRateRatio)
        {
            return true;
        }

        public IResampler CreateInstance(
            ResamplingQuality quality,
            int channels,
            double sampleRateRatio
        )
        {
            return new MockResampler(quality, channels, sampleRateRatio);
        }
    }

    internal class MockResampler : IResampler
    {
        public MockResampler(ResamplingQuality quality, int channels, double sampleRateRatio)
        {
            Quality = quality;
            Channels = channels;
            Ratio = sampleRateRatio;
        }

        public ResamplingQuality Quality { get; set; }

        public int Channels { get; set; }

        public double Ratio { get; set; }

        public bool VariableRate => false;

        public void Clear() { }

        public void Dispose() { }

        public double GetOutputDelay()
        {
            return 0;
        }

        public void Process(
            byte[] input,
            int inputOffset,
            int inputLength,
            byte[] output,
            int outputOffset,
            int outputLength,
            bool endOfInput,
            out int inputLengthUsed,
            out int outputLengthGenerated
        )
        {
            Array.Copy(input, inputOffset, output, outputOffset, inputLength);
            inputLengthUsed = outputLengthGenerated = inputLength;
        }

        public void SetRatio(double ratio, int transitionLength)
        {
            Ratio = ratio;
        }
    }
}
