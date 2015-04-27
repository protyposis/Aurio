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

        private const uint HashSeed = 0x9ea5fa36;
        private const float Quantize_DT_S = 256f / 11025f;
        private const float Quantize_A_S = 256f / 11025f;
        private const uint HashBitmask = 0x000fffff;
        private const int SubBands = 8;

        private const int _Offset = 0;

        public void Generate(AudioTrack track) {
            IAudioStream audioStream = new ResamplingStream(
                new MonoStream(AudioStreamFactory.FromFileInfoIeee32(track.FileInfo)),
                ResamplingQuality.Medium, 11025);

            var whiteningStream = new WhiteningStream(audioStream, 40, 8, 10000);
            var subbandAnalyzer = new SubbandAnalyzer(whiteningStream);

            float[,] E = new float[SubBands, subbandAnalyzer.WindowCount];
            float[] analyzedFrame = new float[SubBands];

            int count = 0;
            while (subbandAnalyzer.HasNext()) {
                subbandAnalyzer.ReadFrame(analyzedFrame);

                for (int i = 0; i < SubBands; i++) {
                    E[i, count] = analyzedFrame[i];
                }

                count++;
            }

            uint[,] bandOnsets;
            uint[] bandOnsetCount;
            // 345 ~= 1 sec (11025 / 8 [subband downsampling factor in SubbandAnalyzer] / 4 [RMS downsampling in adaptiveOnsets()] ~= 345 frames per second)
            uint res = GetAdaptiveOnsets(E, 345, out bandOnsets, out bandOnsetCount);
            var codes = GetCodes(E);
        }

        private uint GetAdaptiveOnsets(float[,] E, int ttarg, out uint[,] bandOnsets, out uint[] bandOnsetCount) {
            //  E is a sgram-like matrix of energies.
            //const float *pE;
            int bands, frames, i, j, k;
            int deadtime = 128;
            double[] H = new double[SubBands], taus = new double[SubBands], N = new double[SubBands];
            bool[] contact = new bool[SubBands], lcontact = new bool[SubBands];
            int[] tsince = new int[SubBands];
            double overfact = 1.1;  /* threshold rel. to actual peak */
            uint onset_counter = 0;

            // Take successive stretches of 8 subband samples and sum their energy under a hann window, then hop by 4 samples (50% window overlap).
            int hop = 4;
            int nsm = 8;
            float[] ham = new float[nsm];
            WindowUtil.Hann(ham, 0, nsm);


            int nc = (int)(Math.Floor((float)E.GetLength(1) / (float)hop) - (Math.Floor((float)nsm / (float)hop) - 1));
            float[,] Eb = new float[nc, 8];

            for (i = 0; i < nc; i++) {
                for (j = 0; j < SubBands; j++) {
                    // Compute RMS of each block
                    for (k = 0; k < nsm; k++) Eb[i, j] = Eb[i, j] + (E[j, (i * hop) + k] * ham[k]);
                    Eb[i, j] = (float)Math.Sqrt(Eb[i, j]);
                }
            }

            frames = Eb.GetLength(0);
            bands = Eb.GetLength(1);

            bandOnsets = new uint[SubBands, frames];
            bandOnsetCount = new uint[SubBands];

            double[] bn = { 0.1883, 0.4230, 0.3392 }; /* preemph filter */   // new
            int nbn = 3;
            double a1 = 0.98;
            double[] Y0 = new double[SubBands];

            for (j = 0; j < bands; ++j) {
                bandOnsetCount[j] = 0;
                N[j] = 0.0;
                taus[j] = 1.0;
                H[j] = E[0, j];
                contact[j] = false;
                lcontact[j] = false;
                tsince[j] = 0;
                Y0[j] = 0;
            }

            for (i = 0; i < frames; ++i) {
                for (j = 0; j < SubBands; ++j) {

                    double xn = 0;
                    /* calculate the filter -  FIR part */
                    if (i >= 2 * nbn) {
                        for (k = 0; k < nbn; ++k) {
                            //xn += bn[k]*(pE[j-SubBands*k] - pE[j-SubBands*(2*nbn-k)]);
                            xn += bn[k] * (E[j, i - k] - E[j, i - (2 * nbn - k)]);
                        }
                    }
                    /* IIR part */
                    xn = xn + a1 * Y0[j];
                    /* remember the last filtered level */
                    Y0[j] = xn;

                    contact[j] = (xn > H[j]);

                    if (contact[j] && !lcontact[j]) {
                        /* attach - record the threshold level unless we have one */
                        if (N[j] == 0) {
                            N[j] = H[j];
                        }
                    }
                    if (contact[j]) {
                        /* update with new threshold */
                        H[j] = xn * overfact;
                    }
                    else {
                        /* apply decays */
                        H[j] = H[j] * Math.Exp(-1.0 / (double)taus[j]);
                    }

                    if (!contact[j] && lcontact[j]) {
                        /* detach */
                        if (bandOnsetCount[j] > 0 && (int)bandOnsets[j, bandOnsetCount[j] - 1] > i - deadtime) {
                            // overwrite last-written time
                            --bandOnsetCount[j];
                            --onset_counter;
                        }
                        bandOnsets[j, bandOnsetCount[j]++] = (uint)i;
                        ++onset_counter;
                        tsince[j] = 0;
                    }
                    ++tsince[j];
                    if (tsince[j] > ttarg) {
                        taus[j] = taus[j] - 1;
                        if (taus[j] < 1) taus[j] = 1;
                    }
                    else {
                        taus[j] = taus[j] + 1;
                    }

                    if (!contact[j] && (tsince[j] > deadtime)) {
                        /* forget the threshold where we recently hit */
                        N[j] = 0;
                    }
                    lcontact[j] = contact[j];
                }
            }

            return onset_counter;
        }

        private uint CalculateQuantizedTimeForFrameDelta(uint frameDelta) {
            double frameDeltaTime = (double)frameDelta / ((double)11025 / 32.0);
            return (uint)(((int)Math.Floor((frameDeltaTime * 1000.0) / (float)Quantize_DT_S) * Quantize_DT_S) / Math.Floor(Quantize_DT_S * 1000.0));
        }

        private uint CalculateQuantizedTimeForFrameAbsolute(uint frame) {
            double frameTime = _Offset + (double)frame / ((double)11025 / 32.0);
            return (uint)(((int)Math.Round((frameTime * 1000.0) / (float)Quantize_A_S) * Quantize_A_S) / Math.Floor(Quantize_A_S * 1000.0));
        }

        private List<FPCode> GetCodes(float[,] E) {
            byte[] hashMaterial = new byte[5];
            uint[] bandOnsetCount;
            uint[,] bandOnsets;
            uint onsetCount = GetAdaptiveOnsets(E, 345, out bandOnsets, out bandOnsetCount);
            List<FPCode> codes = new List<FPCode>((int)onsetCount * 6);

            for (byte band = 0; band < SubBands; band++) {
                if (bandOnsetCount[band] > 2) {
                    for (uint onset = 0; onset < bandOnsetCount[band] - 2; onset++) {
                        // What time was this onset at?
                        uint quantizedOnsetTime = CalculateQuantizedTimeForFrameAbsolute(bandOnsets[band, onset]);

                        uint[,] p = new uint[2, 6];
                        for (int i = 0; i < 6; i++) {
                            p[0, i] = 0;
                            p[1, i] = 0;
                        }
                        int hashCount = 6;

                        if ((int)onset == (int)bandOnsetCount[band] - 4) { hashCount = 3; }
                        if ((int)onset == (int)bandOnsetCount[band] - 3) { hashCount = 1; }
                        p[0, 0] = (bandOnsets[band, onset + 1] - bandOnsets[band, onset]);
                        p[1, 0] = (bandOnsets[band, onset + 2] - bandOnsets[band, onset + 1]);
                        if (hashCount > 1) {
                            p[0, 1] = (bandOnsets[band, onset + 1] - bandOnsets[band, onset]);
                            p[1, 1] = (bandOnsets[band, onset + 3] - bandOnsets[band, onset + 1]);
                            p[0, 2] = (bandOnsets[band, onset + 2] - bandOnsets[band, onset]);
                            p[1, 2] = (bandOnsets[band, onset + 3] - bandOnsets[band, onset + 2]);
                            if (hashCount > 3) {
                                p[0, 3] = (bandOnsets[band, onset + 1] - bandOnsets[band, onset]);
                                p[1, 3] = (bandOnsets[band, onset + 4] - bandOnsets[band, onset + 1]);
                                p[0, 4] = (bandOnsets[band, onset + 2] - bandOnsets[band, onset]);
                                p[1, 4] = (bandOnsets[band, onset + 4] - bandOnsets[band, onset + 2]);
                                p[0, 5] = (bandOnsets[band, onset + 3] - bandOnsets[band, onset]);
                                p[1, 5] = (bandOnsets[band, onset + 4] - bandOnsets[band, onset + 3]);
                            }
                        }

                        // For each pair emit a code
                        for (uint k = 0; k < 6; k++) {
                            // Quantize the time deltas to 23ms
                            short deltaTime0 = (short)CalculateQuantizedTimeForFrameDelta(p[0, k]);
                            short deltaTime1 = (short)CalculateQuantizedTimeForFrameDelta(p[1, k]);
                            // Create a key from the time deltas and the band index
                            hashMaterial[0] = (byte)((deltaTime0 >> 8) & 0xFF);
                            hashMaterial[1] = (byte)((deltaTime0) & 0xFF);
                            hashMaterial[2] = (byte)((deltaTime1 >> 8) & 0xFF);
                            hashMaterial[3] = (byte)((deltaTime1) & 0xFF);
                            hashMaterial[4] = band;
                            uint hashCode = MurmurHash2.Hash(hashMaterial, HashSeed) & HashBitmask;

                            // Set the code alongside the time of onset
                            codes.Add(new FPCode(quantizedOnsetTime, hashCode));
                        }
                    }
                }
            }

            codes.TrimExcess();

            return codes;
        }
    }
}
