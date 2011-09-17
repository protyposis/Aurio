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

        // <summary>
        /// Initializes a new STFT for the specified stream with the specified window and hop size.
        /// </summary>
        /// <param name="stream">the stream to read the audio data to process from</param>
        /// <param name="windowSize">the window size in the dimension of samples</param>
        /// <param name="hopSize">the hop size in the dimension of samples</param>
        /// <param name="windowType">the type of the window function to apply</param>
        public STFT(IAudioStream stream, int windowSize, int hopSize, WindowType windowType)
            : base(stream, windowSize, hopSize) {
                windowFunction = WindowUtil.GetFunction(windowType, WindowSize);
                frameBuffer = new float[WindowSize];
        }

        public override void ReadFrame(float[] fftResult) {
            if (fftResult.Length != WindowSize / 2) {
                throw new ArgumentException("the provided FFT result array has an invalid size");
            }

            base.ReadFrame(frameBuffer);

            // apply window function
            windowFunction.Apply(frameBuffer);

            // do fourier transform
            FFTUtil.FFT(frameBuffer);

            // normalize fourier results
            // TODO check if calculation corresponds to Haitsma & Kalker paper
            FFTUtil.Results(frameBuffer, fftResult);
        }
    }
}
