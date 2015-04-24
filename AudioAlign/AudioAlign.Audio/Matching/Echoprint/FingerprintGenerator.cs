using AudioAlign.Audio.Project;
using AudioAlign.Audio.Streams;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Echoprint {
    /// <summary>
    /// Echoprint code generator as described in:
    /// - Ellis, Daniel PW, Brian Whitman, and Alastair Porter. "Echoprint: 
    ///   An open music identification service." ISMIR 2011 Miami: 12th International 
    ///   Society for Music Information Retrieval Conference, October 24-28. 
    ///   International Society for Music Information Retrieval, 2011.
    /// - http://echoprint.me/how
    /// - https://github.com/echonest/echoprint-codegen
    /// </summary>
    public class FingerprintGenerator {

        public void Generate(AudioTrack track) {
            IAudioStream audioStream = new ResamplingStream(
                new MonoStream(AudioStreamFactory.FromFileInfoIeee32(track.FileInfo)),
                ResamplingQuality.Medium, 11025);


        }

        private class SubbandAnalyzer : StreamWindower {

            /// <summary>
            /// The 128 point subband filter bank coefficients for the decomposition into 8 frequency bands.
            /// 
            /// They are downsampled from the 512 subband filter bank coefficients from the MPEG-1 audio standard 
            /// (ISO/IEC 11172-3:1993), which is a 32-band filter bank for 44.1 kHz audio. We only deal with 11kHz 
            /// audio and only need the lowest 8 bands (44/11 == 32/8 == 512/128).
            /// 
            /// Source: http://heim.ifi.uio.no/~inf3440/MP1/Table_analysis_window.m
            /// 
            /// A filter bank facilitates non-uniform frequency resolution and specification of filters separately
            /// for each bank, whereas transforms (e.g. FFT) are uniform. Transforms are faster with lots of subbands.
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

            public SubbandAnalyzer(IAudioStream stream) : base(stream, C.Length, SubBands) {
                frameBuffer = new float[WindowSize];
                subbandBuffer = new float[SubBands];
                subbandFrameSize = WindowSize / SubBands;

                // Precalculate the analysis filter bank DFT coefficients
                mRe = new float[SubBands, subbandFrameSize];
                mIm = new float[SubBands, subbandFrameSize];

                for (int i = 0; i < SubBands; i++) {
                    for (int j = 0; j < subbandFrameSize; j++) {
                        mRe[i, j] = (float)Math.Cos((2 * i + 1) * (j - 4) * (Math.PI / 16.0));
                        mIm[i, j] = (float)Math.Sin((2 * i + 1) * (j - 4) * (Math.PI / 16.0));
                    }
                }
            }

            public override void ReadFrame(float[] subbandEnergies) {
                if (subbandEnergies.Length != SubBands) {
                    throw new ArgumentOutOfRangeException("wrong subband array size");
                }

                base.ReadFrame(frameBuffer);

                // Multiply frame with filter bank coefficients, ...
                for (int i = 0; i < C.Length; i++) {
                    frameBuffer[i] = frameBuffer[i] * C[i];
                }
                // (the subbands are now interleaved in the frame buffer)

                // ...take the first subsection of fB, which contains the first sample of each subband spectrum(?), ...
                for (int i = 0; i < subbandFrameSize; i++) {
                    subbandBuffer[i] = frameBuffer[i];
                }
                // (subbandBuffer now contains, for each subband, the first sample)

                // ...add up all remaining interleaved samples to their subband, ...
                for (int i = 0; i < subbandFrameSize; i++) {
                    for (int j = 1; j < SubBands; j++) {
                        subbandBuffer[i] += frameBuffer[i + subbandFrameSize * j];
                    }
                }
                // (Y now contains, for each subband, its sum of samples)

                // ...and finally compute the frequency energy of each subband by applying the DFT and calculating the squared magnitude
                for (int i = 0; i < SubBands; i++) {
                    float Dr = 0, Di = 0;
                    for (int j = 0; j < subbandFrameSize; j++) {
                        Dr += mRe[i, j] * subbandBuffer[j];
                        Di -= mIm[i, j] * subbandBuffer[j];
                    }
                    subbandEnergies[i] = Dr * Dr + Di * Di;
                }
                // (subbandEnergies not contains the result as the total energy of each subband)
            }
        }
    }
}
