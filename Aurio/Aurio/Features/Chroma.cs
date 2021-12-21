// 
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2017  Mario Guggenberger <mg@protyposis.net>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using Aurio.Matching;
using Aurio.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Features
{
    /// <summary>
    /// Generates a chromagram from an input audio stream as described in section III.B. of
    /// - Bartsch, Mark A., and Gregory H. Wakefield. "Audio thumbnailing of popular music 
    ///   using chroma-based representations." Multimedia, IEEE Transactions on 7.1 (2005): 96-104.
    /// </summary>
    public class Chroma : STFT
    {

        public enum MappingMode
        {
            /// <summary>
            /// The type of frequency bin to chroma mapping described in
            /// - Bartsch, Mark A., and Gregory H. Wakefield. "Audio thumbnailing of popular music 
            ///   using chroma-based representations." Multimedia, IEEE Transactions on 7.1 (2005): 96-104.
            /// </summary>
            Paper,
            /// <summary>
            /// The type of frequency bin to chroma mapping applied by Chromaprint.
            /// </summary>
            Chromaprint
        }

        public const int Bins = 12;

        private float[] fftFrameBuffer;
        private int[] fftToChromaBinMapping;
        private int fftToChromaBinMappingOffset;
        private int[] fftToChromaBinCount;

        private bool normalize;

        public Chroma(IAudioStream stream, int windowSize, int hopSize, WindowType windowType, float minFreq, float maxFreq, bool normalize, MappingMode mappingMode)
            : base(stream, windowSize, hopSize, windowType, OutputFormat.MagnitudesSquared)
        {
            fftFrameBuffer = new float[windowSize / 2];

            // Precompute FFT bin to Chroma bin mapping
            double freqToBinRatio = (double)stream.Properties.SampleRate / windowSize;
            int minBin = (int)Math.Floor(minFreq / freqToBinRatio);
            int maxBin = (int)Math.Ceiling(maxFreq / freqToBinRatio);
            minBin = Math.Max(minBin, 1); // skip bin 0 in any case (zero f cannot be converted to chroma)
            maxBin = Math.Min(maxBin, fftFrameBuffer.Length - 1);

            fftToChromaBinMapping = new int[maxBin - minBin];
            fftToChromaBinMappingOffset = minBin;
            fftToChromaBinCount = new int[Bins];

            if (mappingMode == MappingMode.Paper)
            {
                for (int i = minBin; i < maxBin; i++)
                {
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
            }
            else if (mappingMode == MappingMode.Chromaprint)
            {
                double A0 = 440.0 / 16.0; // Hz
                for (int i = minBin; i < maxBin; i++)
                {
                    double fftBinCenterFreq = i * freqToBinRatio;
                    double c = Math.Log(fftBinCenterFreq / A0, 2) - Math.Floor(Math.Log(fftBinCenterFreq / A0, 2)); // Chromaprint additionally divides by A0 - WHY?
                    int chromaBin = (int)(c * Bins); // Chromaprint does the mapping more bluntly
                    fftToChromaBinMapping[i - minBin] = chromaBin;
                    fftToChromaBinCount[chromaBin]++; // needed to take the arithmetic mean in formula (6)
                }
            }
            else
            {
                throw new ArgumentException("unknown chroma mapping mode " + mappingMode);
            }

            this.normalize = normalize;
        }

        public Chroma(IAudioStream stream, int windowSize, int hopSize, WindowType windowType, float minFreq, float maxFreq)
            : this(stream, windowSize, hopSize, windowType, minFreq, maxFreq, true, Chroma.MappingMode.Paper)
        {
            //
        }

        public Chroma(IAudioStream stream, int windowSize, int hopSize, WindowType windowType)
            : this(stream, windowSize, hopSize, windowType, 0, stream.Properties.SampleRate / 2, true, Chroma.MappingMode.Paper)
        {
            //
        }

        public override void ReadFrame(float[] chromaFrame)
        {
            if (chromaFrame.Length != Bins)
            {
                throw new ArgumentException("expected chroma frame length is 12");
            }

            // Read FFT frame
            base.ReadFrame(fftFrameBuffer);

            // Convert to chroma frame

            // Sum log magnitude DFT bins in the chroma bins
            Array.Clear(chromaFrame, 0, chromaFrame.Length);
            for (int i = 0; i < fftToChromaBinMapping.Length; i++)
            {
                int bin = fftToChromaBinMapping[i];
                chromaFrame[bin] += fftFrameBuffer[i + fftToChromaBinMappingOffset];
            }

            if (normalize)
            {
                // Take the arithmetic mean
                for (int i = 0; i < chromaFrame.Length; i++)
                {
                    chromaFrame[i] /= fftToChromaBinCount[i];
                }

                // Normalize feature vector by subtracting the scalar mean
                float mean = chromaFrame.Sum() / Bins;
                for (int i = 0; i < chromaFrame.Length; i++)
                {
                    chromaFrame[i] -= mean;
                }
            }
        }
    }
}
