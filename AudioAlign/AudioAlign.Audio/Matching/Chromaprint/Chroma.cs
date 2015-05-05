using AudioAlign.Audio.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Chromaprint {
    /// <summary>
    /// Generates a chromatogram from an input audio stream as described in section III.B. of
    /// - Bartsch, Mark A., and Gregory H. Wakefield. "Audio thumbnailing of popular music 
    ///   using chroma-based representations." Multimedia, IEEE Transactions on 7.1 (2005): 96-104.
    /// </summary>
    class Chroma : STFT {

        public const int Bins = 12;

        private float[] fftFrameBuffer;
        private int[] fftToChromaBinMapping;
        private int fftToChromaBinMappingOffset;
        private int[] fftToChromaBinCount;

        private bool normalize;

        public Chroma(IAudioStream stream, int windowSize, int hopSize, WindowType windowType, float minFreq, float maxFreq, bool normalize)
            : base(stream, windowSize, hopSize, windowType, false) {
            fftFrameBuffer = new float[windowSize / 2];

            // Precompute FFT bin to Chroma bin mapping
            double freqToBinRatio = (double)stream.Properties.SampleRate / windowSize;
            int minBin = (int)Math.Ceiling(minFreq / freqToBinRatio);
            int maxBin = (int)Math.Floor(maxFreq / freqToBinRatio);
            minBin = Math.Max(minBin, 1); // skip bin 0 in any case (zero f cannot be converted to chroma)

            fftToChromaBinMapping = new int[maxBin - minBin];
            fftToChromaBinMappingOffset = minBin;
            fftToChromaBinCount = new int[Bins];

            for (int i = minBin; i < fftFrameBuffer.Length; i++) {
                double fftBinCenterFreq = i * freqToBinRatio;
                double c = Math.Log(fftBinCenterFreq, 2) - Math.Floor(Math.Log(fftBinCenterFreq, 2)); // paper formula (3)
                // c ∈ [0, 1) must be mapped to chroma bins {0...11}
                // The paper says that the first class is centered around 0. This means that the first class has only half the
                // size of the others, but also that there's a 13. class (index 12) at the upper end of c. Therefore, we wrap
                // around the edges with modulo and put the lower and upper end into bin 0, making them all the same size.
                int chromaBin = (int)Math.Round(c * Bins) % Bins;
                fftToChromaBinMapping[i - minBin] = chromaBin;
                fftToChromaBinCount[chromaBin]++; // needed to take the arithmetic mean in formula (6)
            }

            this.normalize = normalize;
        }

        public override void ReadFrame(float[] chromaFrame) {
            if (chromaFrame.Length != Bins) {
                throw new ArgumentException("expected chroma frame length is 12");
            }

            // Read FFT frame
            base.ReadFrame(fftFrameBuffer);

            // Convert to chroma frame
            
            // Sum log magnitude DFT bins in the chroma bins
            Array.Clear(chromaFrame, 0, chromaFrame.Length);
            for (int i = 0; i < fftToChromaBinMapping.Length; i++) {
                int bin = fftToChromaBinMapping[i];
                chromaFrame[bin] += fftFrameBuffer[i + fftToChromaBinMappingOffset];
            }

            if (normalize) {
                // Take the arithmetic mean
                for (int i = 0; i < chromaFrame.Length; i++) {
                    chromaFrame[i] /= fftToChromaBinCount[i];
                }

                // Normalize feature vector by subtracting the scalar mean
                float mean = chromaFrame.Sum() / Bins;
                for (int i = 0; i < chromaFrame.Length; i++) {
                    chromaFrame[i] -= mean;
                }
            }
        }
    }
}
