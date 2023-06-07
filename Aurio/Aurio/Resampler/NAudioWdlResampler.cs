using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Resampler
{
    public class NAudioWdlResampler : IResampler
    {
        private NAudioWdlResamplerWrapper _resampler;

        public NAudioWdlResampler(ResamplingQuality quality, int channels, double sampleRateRatio)
        {
            Quality = quality;
            Channels = channels;
            Ratio = sampleRateRatio;

            _resampler = new NAudioWdlResamplerWrapper();

            // TODO apply fitting configuration settings for the different quality levels
            switch (quality)
            {
                case ResamplingQuality.VeryLow:
                    break;
                case ResamplingQuality.Low:
                    break;
                case ResamplingQuality.Medium:
                    break;
                case ResamplingQuality.High:
                    break;
                case ResamplingQuality.VeryHigh:
                    break;
                case ResamplingQuality.VariableRate:
                    break;
            }

            // Config in NAudio: https://github.com/naudio/NAudio/blob/master/NAudio/Wave/SampleProviders/WdlResamplingSampleProvider.cs
            _resampler.SetMode(true, 2, false);
            _resampler.SetFilterParms();
            _resampler.SetFeedMode(true);
            // Set input sample rate to 1 because we do not know the actual sample rates in here, only the desired output ratio
            // The actual sample rates should be irrelevant anyway, the only thing that counts is the ratio between input and putput
            _resampler.SetRates(1, sampleRateRatio);
        }

        public ResamplingQuality Quality { get; private set; }

        public int Channels { get; private set; }

        public double Ratio { get; private set; }

        public bool VariableRate
        {
            get { return true; }
        }

        public void Clear()
        {
            _resampler.Reset();
        }

        public void Dispose()
        {
            // Nothing to do
        }

        public double GetOutputDelay()
        {
            // TODO check if the return unit in seconds is correct
            return _resampler.GetCurrentLatency();
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
            // TODO implement endOfInput flushing

            // DWL resampler API docs: https://github.com/justinfrankel/WDL/blob/master/WDL/resample.h
            // Seems like all sample counts are expected per-channel (not as sum of all channels)

            float[] inBuffer;
            int inBufferOffset;
            int inputSamples = inputLength / 4 / Channels; // The max number of samples that we can offer the resampler

            // Ask the resampler how many input samples we should write into the input array
            int inNeeded = _resampler.ResamplePrepare(
                inputSamples,
                Channels,
                out inBuffer,
                out inBufferOffset
            );
            inputLengthUsed = inNeeded * Channels * 4;

            // Copy the requested number of samples into the input
            // Here we copy floats from a raw byte to a float array and we need to convert all numbers to byte counts
            Buffer.BlockCopy(input, inputOffset, inBuffer, inBufferOffset * 4, inputLengthUsed);

            // Read resampled output
            int outputSamplesRequested = outputLength / 4 / Channels;
            float[] outBuffer = new float[outputSamplesRequested * Channels]; // TODO create reuseable instance field instance
            int outAvailable = _resampler.ResampleOut(
                outBuffer,
                0,
                inNeeded,
                outputSamplesRequested,
                Channels
            );
            outputLengthGenerated = outAvailable * 4 * Channels;

            // Copy the output into the output buffer
            Buffer.BlockCopy(outBuffer, 0, output, outputOffset, outputLengthGenerated);
        }

        public void SetRatio(double ratio, int transitionLength)
        {
            if (transitionLength > 0)
            {
                throw new NotSupportedException("WdlResampler does not support rate transitions");
            }

            _resampler.SetRates(1, ratio);
            Ratio = ratio;
        }
    }
}
