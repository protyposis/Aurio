using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Aurio.Resampler
{
    /// <summary>
    /// Provides access to NAudio's internal WdlResampler class via reflection
    /// https://github.com/naudio/NAudio/blob/master/NAudio/Dsp/WdlResampler.cs
    /// </summary>
    public class NAudioWdlResamplerWrapper
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
            // Use a random NAudio type to get a reference to the assembly
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
