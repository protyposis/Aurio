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
        private const uint HashBitmask = 0x000fffff;
        private const int SubBands = 8;

        public void Generate(AudioTrack track) {
            IAudioStream audioStream = new ResamplingStream(
                new MonoStream(AudioStreamFactory.FromFileInfoIeee32(track.FileInfo)),
                ResamplingQuality.Medium, 11025);

            var whiteningStream = new WhiteningStream(audioStream, 40, 8, 10000);
            var subbandAnalyzer = new SubbandAnalyzer(whiteningStream);

            float[,] E = new float[SubBands, subbandAnalyzer.WindowCount];
            float[] analyzedFrame = new float[SubBands];

            var sw = new Stopwatch();
            sw.Start();

            int count = 0;
            while (subbandAnalyzer.HasNext()) {
                subbandAnalyzer.ReadFrame(analyzedFrame);

                for (int i = 0; i < SubBands; i++) {
                    E[i, count] = analyzedFrame[i];
                }

                count++;
            }

            Console.WriteLine("analysis time: " + sw.Elapsed);

            sw.Restart();
            var codes = GetCodes(E);
            sw.Stop();
            Console.WriteLine("codegen time: " + sw.Elapsed);
        }

        private uint GetAdaptiveOnsets(float[,] E, int targetOnsetDistance, out uint[,] bandOnsets, out uint[] bandOnsetCount) {
            int minOnsetDistance = 128;
            double[] H = new double[SubBands]; // threshold
            int[] taus = new int[SubBands]; // decay rate adjustment factor
            bool[] contact = new bool[SubBands], lastContact = new bool[SubBands]; // signal-exceeds-threshold marker
            int[] timeSinceLastOnset = new int[SubBands];
            double overfact = 1.1;  /* threshold rel. to actual peak */
            uint onsetCount = 0;

            // Take successive stretches of 8 subband samples and sum their energy under a hann window, then hop by 4 samples (50% window overlap).
            int hopSize = 4;
            int windowSize = 8;
            float[] window = WindowUtil.GetArray(WindowType.Hann, windowSize);

            int nc = (int)(Math.Floor((float)E.GetLength(1) / (float)hopSize) - (Math.Floor((float)windowSize / (float)hopSize) - 1));
            float[,] Eb = new float[nc, 8];

            for (int i = 0; i < nc; i++) {
                for (int band = 0; band < SubBands; band++) {
                    // Compute RMS of each block
                    for (int k = 0; k < windowSize; k++) {
                        Eb[i, band] = Eb[i, band] + (E[band, (i * hopSize) + k] * window[k]);
                    }
                    Eb[i, band] = (float)Math.Sqrt(Eb[i, band]);
                }
            }

            int frames = Eb.GetLength(0);
            int bands = Eb.GetLength(1);

            bandOnsets = new uint[SubBands, frames];
            bandOnsetCount = new uint[SubBands];

            double[] bn = { 0.1883, 0.4230, 0.3392 }; /* preemph filter */
            double a1 = 0.98;
            double[] Y0 = new double[SubBands];

            for (int band = 0; band < bands; ++band) {
                bandOnsetCount[band] = 0;
                taus[band] = 1;
                H[band] = Eb[0, band];
                contact[band] = false;
                lastContact[band] = false;
                timeSinceLastOnset[band] = 0;
                Y0[band] = 0;
            }

            for (int frame = 0; frame < frames; ++frame) {
                for (int band = 0; band < SubBands; ++band) {
                    double xn = 0; // signal level of current frame
                    /* calculate the filter -  FIR part */
                    if (frame >= 2 * bn.Length) {
                        for (int k = 0; k < bn.Length; ++k) {
                            xn += bn[k] * (Eb[frame - k, band] - Eb[frame - (2 * bn.Length - k), band]);
                        }
                    }
                    /* IIR part */
                    xn = xn + a1 * Y0[band];

                    /* remember the last filtered level */
                    Y0[band] = xn;

                    // Check if the signal exceeds the threshold
                    contact[band] = (xn > H[band]);

                    if (contact[band]) {
                        /* update with new threshold */
                        H[band] = xn * overfact;
                    }
                    else {
                        /* apply decays */
                        H[band] = H[band] * Math.Exp(-1.0 / (double)taus[band]);
                    }

                    // When the signal does not exceed the threshold anymore, but did in the last frame, we have an onset detected
                    if (!contact[band] && lastContact[band]) {
                        // If the distance between the previous and current onset is too short, we replace the previous onset
                        if (bandOnsetCount[band] > 0 && frame - (int)bandOnsets[band, bandOnsetCount[band] - 1] < minOnsetDistance) {
                            --bandOnsetCount[band];
                            --onsetCount;
                        }

                        // Store the onset
                        bandOnsets[band, bandOnsetCount[band]++] = (uint)frame;
                        ++onsetCount;
                        timeSinceLastOnset[band] = 0;
                    }
                    ++timeSinceLastOnset[band];

                    // Adjust the decay rate
                    if (timeSinceLastOnset[band] > targetOnsetDistance) {
                        // Increase decay above the target onset distance (makes it easier to detect an onset)
                        if (taus[band] > 1) {
                            taus[band]--;
                        }
                    }
                    else {
                        // Decrease decay rate below target onset distance
                        taus[band]++;
                    }

                    lastContact[band] = contact[band];
                }
            }

            return onsetCount;
        }

        /// <summary>
        /// The quantized_time_for_frame_delta and quantized_time_for_frame_absolute functions in the original
        /// Echoprint source are way too complicated and can be simplified to this function. The offset is omitted
        /// here as it is not needed.
        /// </summary>
        private uint QuantizeFrameTime(uint frame) {
            return (uint)Math.Round(frame / 8d);
        }

        private List<FPCode> GetCodes(float[,] E) {
            byte[] hashMaterial = new byte[5];
            uint[] bandOnsetCount;
            uint[,] bandOnsets;

            // 345 ~= 1 sec (11025 / 8 [subband downsampling factor in SubbandAnalyzer] / 4 [RMS downsampling in adaptiveOnsets()] ~= 345 frames per second)
            uint onsetCount = GetAdaptiveOnsets(E, 345, out bandOnsets, out bandOnsetCount);
            List<FPCode> codes = new List<FPCode>((int)onsetCount * 6);

            for (byte band = 0; band < SubBands; band++) {
                if (bandOnsetCount[band] > 2) {
                    for (uint onset = 0; onset < bandOnsetCount[band] - 2; onset++) {
                        // What time was this onset at?
                        uint quantizedOnsetTime = QuantizeFrameTime(bandOnsets[band, onset]);

                        uint[,] deltaPairs = new uint[2, 6];

                        deltaPairs[0, 0] = (bandOnsets[band, onset + 1] - bandOnsets[band, onset]);
                        deltaPairs[1, 0] = (bandOnsets[band, onset + 2] - bandOnsets[band, onset + 1]);
                        if (onset < bandOnsetCount[band] - 3) {
                            deltaPairs[0, 1] = (bandOnsets[band, onset + 1] - bandOnsets[band, onset]);
                            deltaPairs[1, 1] = (bandOnsets[band, onset + 3] - bandOnsets[band, onset + 1]);
                            deltaPairs[0, 2] = (bandOnsets[band, onset + 2] - bandOnsets[band, onset]);
                            deltaPairs[1, 2] = (bandOnsets[band, onset + 3] - bandOnsets[band, onset + 2]);
                            if (onset < bandOnsetCount[band] - 4) {
                                deltaPairs[0, 3] = (bandOnsets[band, onset + 1] - bandOnsets[band, onset]);
                                deltaPairs[1, 3] = (bandOnsets[band, onset + 4] - bandOnsets[band, onset + 1]);
                                deltaPairs[0, 4] = (bandOnsets[band, onset + 2] - bandOnsets[band, onset]);
                                deltaPairs[1, 4] = (bandOnsets[band, onset + 4] - bandOnsets[band, onset + 2]);
                                deltaPairs[0, 5] = (bandOnsets[band, onset + 3] - bandOnsets[band, onset]);
                                deltaPairs[1, 5] = (bandOnsets[band, onset + 4] - bandOnsets[band, onset + 3]);
                            }
                        }

                        // For each pair emit a code
                        // NOTE This always generates 6 codes, even at the end of a band where < 6 pairs
                        //      are formed. Thats not really a problem though as their delta times will
                        //      always be zero, whereas valid pairs have delta times above zero; therefore
                        //      the chance is very low to get spurious collisions.
                        for (uint k = 0; k < 6; k++) {
                            // Quantize the time deltas to 23ms
                            short deltaTime0 = (short)QuantizeFrameTime(deltaPairs[0, k]);
                            short deltaTime1 = (short)QuantizeFrameTime(deltaPairs[1, k]);
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
