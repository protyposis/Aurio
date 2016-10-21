using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurio.Streams;

namespace Aurio.Features {
    /// <summary>
    /// Inverse Short-Time Fourier Tranformation.
    /// Takes a number of raw FFT frames and converts them to a continuous audio stream by using the overlap-add method.
    /// </summary>
    public class InverseSTFT : OLA {

        private float[] frameBuffer;
        private PFFFT.PFFFT fft;
        private WindowFunction synthesisWindow;

        public InverseSTFT(IAudioWriterStream stream, int windowSize, int hopSize, int fftSize, WindowType windowType) 
            : base(stream, windowSize, hopSize) {
            if (fftSize < windowSize) {
                throw new ArgumentOutOfRangeException("fftSize must be >= windowSize");
            }
            frameBuffer = new float[fftSize];
            fft = new PFFFT.PFFFT(fftSize, PFFFT.Transform.Real);
            synthesisWindow = WindowUtil.GetFunction(windowType, windowSize);
        }

        public InverseSTFT(IAudioWriterStream stream, int windowSize, int hopSize, WindowType windowType)
            : this(stream, windowSize, hopSize, windowSize, windowType) {
        }

        /// <summary>
        /// Writes a raw FFT result frame into the output audio stream.
        /// </summary>
        /// <param name="fftResult">raw FFT frame as output by STFT in OutputFormat.Raw mode</param>
        public override void WriteFrame(float[] fftResult) {
            if (fftResult.Length != fft.Size) {
                throw new ArgumentException("the provided FFT result array has an invalid size");
            }

            OnFrameWrittenInverseSTFT(fftResult);

            // do inverse fourier transform
            fft.Backward(fftResult, frameBuffer);

            // Apply synthesis window
            synthesisWindow.Apply(frameBuffer);

            base.WriteFrame(frameBuffer);
        }

        /// <summary>
        /// Flushes remaining buffered data to the output audio stream.
        /// </summary>
        public override void Flush() {
            base.Flush();
        }

        protected virtual void OnFrameWrittenInverseSTFT(float[] frame) {
            // to be overridden
        }
    }
}
