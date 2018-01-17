using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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
            get
            {
                return true;
            }
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

        public void Process(byte[] input, int inputOffset, int inputLength, 
            byte[] output, int outputOffset, int outputLength, 
            bool endOfInput, out int inputLengthUsed, out int outputLengthGenerated)
        {
            // TODO implement endOfInput flushing

            // DWL resampler API docs: https://github.com/justinfrankel/WDL/blob/master/WDL/resample.h
            // Seems like all sample counts are expected per-channel (not as sum of all channels)

            float[] inBuffer;
            int inBufferOffset;
            int inputSamples = inputLength / 4 / Channels; // The max number of samples that we can offer the resampler

            // Ask the resampler how many input samples we should write into the input array
            int inNeeded = _resampler.ResamplePrepare(inputSamples, Channels, out inBuffer, out inBufferOffset);
            inputLengthUsed = inNeeded * Channels * 4;

            // Copy the requested number of samples into the input
            // Here we copy floats from a raw byte to a float array and we need to convert all numbers to byte counts
            Buffer.BlockCopy(input, inputOffset, inBuffer, inBufferOffset * 4, inputLengthUsed);

            // Read resampled output
            int outputSamplesRequested = outputLength / 4 / Channels;
            float[] outBuffer = new float[outputSamplesRequested * Channels]; // TODO create reuseable instance field instance
            int outAvailable = _resampler.ResampleOut(outBuffer, 0, inNeeded, outputSamplesRequested, Channels);
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

    /// <summary>
    /// Provides access to NAudio's internal WdlResampler class via reflection
    /// https://github.com/naudio/NAudio/blob/master/NAudio/Dsp/WdlResampler.cs
    /// </summary>
    class NAudioWdlResamplerWrapper
    {
        private static readonly Type _type;
        private static readonly MethodInfo _setMode;
        private static readonly MethodInfo _setFilterParms;
        private static readonly MethodInfo _setFeedMode;
        private static readonly MethodInfo _reset;
        private static readonly MethodInfo _setRates;
        private static readonly MethodInfo _getCurrentLatency;
        private static readonly MethodInfo _resamplePrepare;
        private static readonly MethodInfo _resampleOut;

        // Create delegates for the most frequently called methods because it's much faster than Invoke calls
        // We need a delegate for ResamplePrepare to nicely handle the out parameters
        private delegate int ResamplePrepareDelegate(int out_samples, int nch, out float[] inbuffer, out int inbufferOffset);
        private delegate int ResampleOutDelegate(float[] outBuffer, int outBufferIndex, int nsamples_in, int nsamples_out, int nch);

        private readonly ResamplePrepareDelegate _resamplePrepareDelegate;
        private readonly ResampleOutDelegate _resampleOutDelegate;

        private readonly object _instance;

        static NAudioWdlResamplerWrapper()
        {
            Assembly nAudioAssembly = typeof(WdlResamplingSampleProvider).Assembly;
            _type = nAudioAssembly.GetType("NAudio.Dsp.WdlResampler");
            _setMode = _type.GetMethod("SetMode");
            _setFilterParms = _type.GetMethod("SetFilterParms");
            _setFeedMode = _type.GetMethod("SetFeedMode");
            _reset = _type.GetMethod("Reset");
            _setRates = _type.GetMethod("SetRates");
            _getCurrentLatency = _type.GetMethod("GetCurrentLatency");
            _resamplePrepare = _type.GetMethod("ResamplePrepare");
            _resampleOut = _type.GetMethod("ResampleOut");
        }

        public NAudioWdlResamplerWrapper()
        {
            _instance = Activator.CreateInstance(_type);

            _resamplePrepareDelegate = (ResamplePrepareDelegate)Delegate.CreateDelegate(
                typeof(ResamplePrepareDelegate), _instance, _resamplePrepare);

            _resampleOutDelegate = (ResampleOutDelegate)Delegate.CreateDelegate(
                typeof(ResampleOutDelegate), _instance, _resampleOut);
        }

        public void SetMode(bool interp, int filtercnt, bool sinc, int sinc_size = 64, int sinc_interpsize = 32)
        {
            _setMode.Invoke(_instance, new object[] { interp, filtercnt, sinc, sinc_size, sinc_interpsize });
        }

        public void SetFilterParms(float filterpos = 0.693f, float filterq = 0.707f)
        {
            _setFilterParms.Invoke(_instance, new object[] { filterpos, filterq });
        }

        public void SetFeedMode(bool wantInputDriven)
        {
            _setFeedMode.Invoke(_instance, new object[] { wantInputDriven });
        }

        public void Reset(double fracpos = 0.0)
        {
            _reset.Invoke(_instance, new object[] { fracpos });
        }

        public void SetRates(double rate_in, double rate_out)
        {
            _setRates.Invoke(_instance, new object[] { rate_in, rate_out });
        }

        public double GetCurrentLatency()
        {
            return (double)_getCurrentLatency.Invoke(_instance, new object[0]);
        }

        public int ResamplePrepare(int out_samples, int nch, out float[] inbuffer, out int inbufferOffset)
        {
            return _resamplePrepareDelegate(out_samples, nch, out inbuffer, out inbufferOffset);
        }

        public int ResampleOut(float[] outBuffer, int outBufferIndex, int nsamples_in, int nsamples_out, int nch)
        {
            return _resampleOutDelegate(outBuffer, outBufferIndex, nsamples_in, nsamples_out, nch);
        }
    }
}
