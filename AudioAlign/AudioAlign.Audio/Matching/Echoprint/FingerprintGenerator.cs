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

            float[,] outm;
            uint[] onset_counter_for_band;
            uint res = adaptiveOnsets(E, 345, out outm, out onset_counter_for_band);
        }

        uint adaptiveOnsets(float[,] E, int ttarg, out float[,] outm, out uint[] onset_counter_for_band) {
            //  E is a sgram-like matrix of energies.
            //const float *pE;
            int bands, frames, i, j, k;
            int deadtime = 128;
            double[] H = new double[SubBands], taus = new double[SubBands], N = new double[SubBands];
            int[] contact = new int[SubBands], lcontact = new int[SubBands], tsince = new int[SubBands];
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

            outm = new float[SubBands, frames];
            onset_counter_for_band = new uint[SubBands];

            double[] bn = { 0.1883, 0.4230, 0.3392 }; /* preemph filter */   // new
            int nbn = 3;
            double a1 = 0.98;
            double[] Y0 = new double[SubBands];

            for (j = 0; j < bands; ++j) {
                onset_counter_for_band[j] = 0;
                N[j] = 0.0;
                taus[j] = 1.0;
                H[j] = E[0, j];
                contact[j] = 0;
                lcontact[j] = 0;
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

                    contact[j] = (xn > H[j]) ? 1 : 0;

                    if (contact[j] == 1 && lcontact[j] == 0) {
                        /* attach - record the threshold level unless we have one */
                        if (N[j] == 0) {
                            N[j] = H[j];
                        }
                    }
                    if (contact[j] == 1) {
                        /* update with new threshold */
                        H[j] = xn * overfact;
                    }
                    else {
                        /* apply decays */
                        H[j] = H[j] * Math.Exp(-1.0 / (double)taus[j]);
                    }

                    if (contact[j] == 0 && lcontact[j] == 1) {
                        /* detach */
                        if (onset_counter_for_band[j] > 0 && (int)outm[j, onset_counter_for_band[j] - 1] > i - deadtime) {
                            // overwrite last-written time
                            --onset_counter_for_band[j];
                            --onset_counter;
                        }
                        outm[j, onset_counter_for_band[j]++] = i;
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

                    if ((contact[j] == 0) && (tsince[j] > deadtime)) {
                        /* forget the threshold where we recently hit */
                        N[j] = 0;
                    }
                    lcontact[j] = contact[j];
                }
            }

            return onset_counter;
        }

        private uint quantized_time_for_frame_delta(uint frame_delta) {
            double time_for_frame_delta = (double)frame_delta / ((double)11025 / 32.0);
            return (uint)(((int)Math.Floor((time_for_frame_delta * 1000.0) / (float)Quantize_DT_S) * Quantize_DT_S) / Math.Floor(Quantize_DT_S * 1000.0));
        }

        private uint quantized_time_for_frame_absolute(uint frame) {
            double time_for_frame = _Offset + (double)frame / ((double)11025 / 32.0);
            return (uint)(((int)Math.Round((time_for_frame * 1000.0) / (float)Quantize_A_S) * Quantize_A_S) / Math.Floor(Quantize_A_S * 1000.0));
        }
    }
}
