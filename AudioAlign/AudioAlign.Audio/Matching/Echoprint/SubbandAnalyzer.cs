using AudioAlign.Audio.Features;
using AudioAlign.Audio.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Echoprint {
    /// <summary>
    /// Divides a source signal into 8 bands by converting every 8 samples of a source signal into a vector 
    /// of 8 values containing the total energy of each band. The series of result vectors form a spectrogram-like
    /// matrix of subband energies.
    /// </summary>
    class SubbandAnalyzer : StreamWindower {

        /// <summary>
        /// The 128 point subband filter analysis window coefficients for the decomposition into 8 frequency bands.
        /// 
        /// They are downsampled from the 512 subband filter bank coefficients from the MPEG-1 audio standard 
        /// (ISO/IEC 11172-3:1993 pp. 68--69), which is a window for a 32-band filter bank for 44.1 kHz audio. 
        /// We only deal with 11kHz audio and only need the lowest 8 bands (44/11 == 32/8 == 512/128).
        /// 
        /// Window source: http://heim.ifi.uio.no/~inf3440/MP1/Table_analysis_window.m
        /// </summary>
        private static readonly float[] C = {
                     0.000000477f,  0.000000954f,  0.000001431f,  0.000002384f,  0.000003815f,  0.000006199f,  0.000009060f,  0.000013828f,
                     0.000019550f,  0.000027657f,  0.000037670f,  0.000049591f,  0.000062943f,  0.000076771f,  0.000090599f,  0.000101566f,
                    -0.000108242f, -0.000106812f, -0.000095367f, -0.000069618f, -0.000027180f,  0.000034332f,  0.000116348f,  0.000218868f,
                     0.000339031f,  0.000472546f,  0.000611782f,  0.000747204f,  0.000866413f,  0.000954151f,  0.000994205f,  0.000971317f,
                    -0.000868797f, -0.000674248f, -0.000378609f,  0.000021458f,  0.000522137f,  0.001111031f,  0.001766682f,  0.002457142f,
                     0.003141880f,  0.003771782f,  0.004290581f,  0.004638195f,  0.004752159f,  0.004573822f,  0.004049301f,  0.003134727f,
                    -0.001800537f, -0.000033379f,  0.002161503f,  0.004756451f,  0.007703304f,  0.010933399f,  0.014358521f,  0.017876148f,
                     0.021372318f,  0.024725437f,  0.027815342f,  0.030526638f,  0.032754898f,  0.034412861f,  0.035435200f,  0.035780907f,
                    -0.035435200f, -0.034412861f, -0.032754898f, -0.030526638f, -0.027815342f, -0.024725437f, -0.021372318f, -0.017876148f,
                    -0.014358521f, -0.010933399f, -0.007703304f, -0.004756451f, -0.002161503f,  0.000033379f,  0.001800537f,  0.003134727f,
                    -0.004049301f, -0.004573822f, -0.004752159f, -0.004638195f, -0.004290581f, -0.003771782f, -0.003141880f, -0.002457142f,
                    -0.001766682f, -0.001111031f, -0.000522137f, -0.000021458f,  0.000378609f,  0.000674248f,  0.000868797f,  0.000971317f,
                    -0.000994205f, -0.000954151f, -0.000866413f, -0.000747204f, -0.000611782f, -0.000472546f, -0.000339031f, -0.000218868f,
                    -0.000116348f, -0.000034332f,  0.000027180f,  0.000069618f,  0.000095367f,  0.000106812f,  0.000108242f,  0.000101566f,
                    -0.000090599f, -0.000076771f, -0.000062943f, -0.000049591f, -0.000037670f, -0.000027657f, -0.000019550f, -0.000013828f,
                    -0.000009060f, -0.000006199f, -0.000003815f, -0.000002384f, -0.000001431f, -0.000000954f, -0.000000477f, 0 };

        public const int SubBands = 8;

        private float[] frameBuffer;
        private float[] subbandBuffer;
        private int subbandFrameSize;

        private float[,] mRe;
        private float[,] mIm;

        public SubbandAnalyzer(IAudioStream stream)
            : base(stream, C.Length, SubBands) {

            if (stream.Properties.SampleRate != 11025) {
                throw new ArgumentException("stream sample rate must be 11025");
            }

            frameBuffer = new float[WindowSize];
            subbandFrameSize = WindowSize / SubBands;
            subbandBuffer = new float[subbandFrameSize];

            mRe = new float[SubBands, subbandFrameSize];
            mIm = new float[SubBands, subbandFrameSize];

            // Precalculate the analysis filter bank coefficients
            for (int i = 0; i < SubBands; i++) {
                for (int j = 0; j < subbandFrameSize; j++) {
                    mRe[i, j] = (float)Math.Cos((2 * i + 1) * (j - 4) * (Math.PI / 16.0)); // from the ISO standard
                    mIm[i, j] = (float)Math.Sin((2 * i + 1) * (j - 4) * (Math.PI / 16.0)); // added for DFT
                }
            }
        }

        /// <summary>
        /// Every frame is basically its previous frame with 8 new samples added to the end and
        /// the oldest 8 samples removed from the beginning. For every 8 new samples, the data 
        /// is divided into 8 bands, one sample each.
        /// 
        /// This process is described in 
        ///  - ISO/IEC 11172-3:1993 p. 67 (text) and p. 78 (flowchart)
        ///  - Pan Davis, A Tutorial on MPEG/Audio Compression
        /// </summary>
        /// <param name="subbandEnergies">the output</param>
        public override void ReadFrame(float[] subbandEnergies) {
            if (subbandEnergies.Length != SubBands) {
                throw new ArgumentOutOfRangeException("wrong subband array size");
            }

            base.ReadFrame(frameBuffer);

            // The following resembles the flow chart from figure C.4 of ISO/IEC 11172-3:1993 p. 78
            // NOTE in the standard, the most recent sample is at index 0, while here it is at length-1

            // Multiply frame X with the analysis window coefficients C to get Z, ...
            for (int i = 0; i < C.Length; i++) {
                frameBuffer[i] = frameBuffer[i] * C[i];
            }

            // ...do the partial calculation to get Yi, ...
            for (int i = 0; i < subbandFrameSize; i++) {
                subbandBuffer[i] = frameBuffer[i];
            }
            for (int i = 0; i < subbandFrameSize; i++) {
                for (int j = 1; j < SubBands; j++) {
                    subbandBuffer[i] += frameBuffer[i + subbandFrameSize * j];
                }
            }

            // ...and finally compute the output by matrixing
            for (int i = 0; i < SubBands; i++) {
                float Dr = 0, Di = 0;
                for (int j = 0; j < subbandFrameSize; j++) {
                    Dr += mRe[i, j] * subbandBuffer[j]; // calculate output sample of subband i
                    Di -= mIm[i, j] * subbandBuffer[j];
                }
                // calculate the frequency energy of subband i by applying the DFT and calculating the squared magnitude (???)
                // TODO find out whats really happening here... is the enhancement to the ISO standard here really 
                //      to get the energy magnitude instead of the sample value?
                subbandEnergies[i] = Dr * Dr + Di * Di;
            }

            // subbandEnergies now contains the result as the total energy of each subband
        }
    }
}
