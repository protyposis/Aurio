using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Streams;

namespace AudioAlign.Audio.Matching {
    /// <summary>
    /// Short-Time Fourier Tranformation
    /// </summary>
    class STFT: StreamWindower {

        private WindowFunction windowFunction;
        private float[] frameBuffer;
        private FFTW.FFTW fftw;
        private bool normalizeTo_dB;

        // <summary>
        /// Initializes a new STFT for the specified stream with the specified window and hop size.
        /// </summary>
        /// <param name="stream">the stream to read the audio data to process from</param>
        /// <param name="windowSize">the window size in the dimension of samples</param>
        /// <param name="hopSize">the hop size in the dimension of samples</param>
        /// <param name="windowType">the type of the window function to apply</param>
        /// <param name="normalizeTo_dB">true if the FFT result should be normalized to dB scale, false if raw FFT magnitudes are desired</param>
        public STFT(IAudioStream stream, int windowSize, int hopSize, WindowType windowType, bool normalizeTo_dB)
            : base(stream, windowSize, hopSize) {
                windowFunction = WindowUtil.GetFunction(windowType, WindowSize);
                frameBuffer = new float[WindowSize];
                fftw = new FFTW.FFTW(WindowSize);
                this.normalizeTo_dB = normalizeTo_dB;
        }

        public STFT(IAudioStream stream, int windowSize, int hopSize, WindowType windowType)
            : this(stream, windowSize, hopSize, windowType, true) {

        }

        public override void ReadFrame(float[] fftResult) {
            if (fftResult.Length != WindowSize / 2) {
                throw new ArgumentException("the provided FFT result array has an invalid size");
            }

            base.ReadFrame(frameBuffer);

            // apply window function
            windowFunction.Apply(frameBuffer);

            // do fourier transform
            //FFTUtil.FFT(frameBuffer);
            fftw.Execute(frameBuffer);

            // normalize fourier results
            if (normalizeTo_dB) {
                // TODO check if calculation corresponds to Haitsma & Kalker paper
                FFTUtil.Results(frameBuffer, fftResult);
            }
            else {
                //FFTUtil.CalculateMagnitudes(frameBuffer, fftResult);
                // TEMP TEST FOR OLTW
                FFTUtil.CalculateMagnitudesSquared(frameBuffer, fftResult);
            }
        }
    }
}
