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

using Aurio.DataStructures;
using Aurio.Project;
using Aurio.Resampler;
using Aurio.Streams;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Aurio.Matching.Echoprint
{
    /// <summary>
    /// Echoprint code generator as described in:
    /// - Ellis, Daniel PW, Brian Whitman, and Alastair Porter. "Echoprint:
    ///   An open music identification service." ISMIR 2011 Miami: 12th International
    ///   Society for Music Information Retrieval Conference, October 24-28.
    ///   International Society for Music Information Retrieval, 2011.
    /// - http://echoprint.me/how
    /// - https://github.com/echonest/echoprint-codegen
    /// </summary>
    public class FingerprintGenerator
    {
        private const uint HashSeed = 0x9ea5fa36;
        private const uint HashBitmask = 0x000fffff;

        private Profile profile;

        public FingerprintGenerator(Profile profile)
        {
            this.profile = profile;
        }

        public event EventHandler<SubFingerprintsGeneratedEventArgs> SubFingerprintsGenerated;

        /// <summary>
        /// This method generates hash codes from an audio stream in a streaming fashion,
        /// which means that it only maintains a small constant-size state and can process
        /// streams of arbitrary length.
        ///
        /// Here is a scheme of the data processing flow. After the subband splitting
        /// stage, every subband is processed independently.
        ///
        ///                    +-----------------+   +--------------+   +-----------+
        ///   audio stream +---> mono conversion +---> downsampling +---> whitening |
        ///                    +-----------------+   +--------------+   +---------+-+
        ///                                                                       |
        ///                        +-------------------+   +------------------+   |
        ///                        | subband splitting <---+ subband analysis <---+
        ///                        +--+---+---+---+----+   +------------------+
        ///                           |   |   |   |
        ///                           |   v   v   v
        ///                           |  ... ... ...
        ///                           |
        ///                           |   +------------------+   +-----------------+
        ///                           +---> RMS downsampling +---> onset detection |
        ///                               +------------------+   +----------+------+
        ///                                                                 |
        ///                                    +-----------------+          |
        ///   hash codes   <-------------------+ hash generation <----------+
        ///                                    +-----------------+
        ///
        /// The hash codes from the hash generators of each band are then sent though a
        /// sorter which brings them into sequential temporal order before they are stored
        /// in the final list.
        /// </summary>
        /// <param name="track"></param>
        public void Generate(AudioTrack track)
        {
            IAudioStream audioStream = new ResamplingStream(
                new MonoStream(AudioStreamFactory.FromFileInfoIeee32(track.FileInfo)),
                ResamplingQuality.Medium,
                profile.SamplingRate
            );

            var whiteningStream = new WhiteningStream(
                audioStream,
                profile.WhiteningNumPoles,
                profile.WhiteningDecaySecs,
                profile.WhiteningBlockLength
            );
            var subbandAnalyzer = new SubbandAnalyzer(whiteningStream);

            float[] analyzedFrame = new float[profile.SubBands];

            var bandAnalyzers = new BandAnalyzer[profile.SubBands];
            for (int i = 0; i < profile.SubBands; i++)
            {
                bandAnalyzers[i] = new BandAnalyzer(profile, i);
            }

            List<SubFingerprint> hashes = new List<SubFingerprint>();
            HashTimeSorter hashSorter = new HashTimeSorter(profile.SubBands);

            var sw = new Stopwatch();
            sw.Start();

            int totalFrames = subbandAnalyzer.WindowCount;
            int currentFrame = 0;
            while (subbandAnalyzer.HasNext())
            {
                subbandAnalyzer.ReadFrame(analyzedFrame);

                for (int i = 0; i < profile.SubBands; i++)
                {
                    bandAnalyzers[i].ProcessSample(analyzedFrame[i], hashSorter.Queues[i]);
                }

                if (currentFrame % 4096 == 0)
                {
                    hashSorter.Fill(hashes, false);

                    if (SubFingerprintsGenerated != null)
                    {
                        SubFingerprintsGenerated(
                            this,
                            new SubFingerprintsGeneratedEventArgs(
                                track,
                                hashes,
                                currentFrame,
                                totalFrames
                            )
                        );
                        hashes.Clear();
                    }
                }

                currentFrame++;
            }

            for (int i = 0; i < bandAnalyzers.Length; i++)
            {
                bandAnalyzers[i].Flush(hashSorter.Queues[i]);
            }
            hashSorter.Fill(hashes, true);

            if (SubFingerprintsGenerated != null)
            {
                SubFingerprintsGenerated(
                    this,
                    new SubFingerprintsGeneratedEventArgs(track, hashes, currentFrame, totalFrames)
                );
                hashes.Clear();
            }

            sw.Stop();
            audioStream.Close();
            Console.WriteLine("time: " + sw.Elapsed);
        }

        public static Profile[] GetProfiles()
        {
            return new Profile[] { new DefaultProfile() };
        }

        /// <summary>
        /// Analyzes a subband for onsets and generates hash codes.
        /// </summary>
        private class BandAnalyzer
        {
            private static readonly double[] FilterCoefficientsBn = { 0.1883, 0.4230, 0.3392 }; /* preemph filter */
            private const double FilterCoefficientA1 = 0.98; // feedback filter coefficient

            private Profile profile;
            private int band;

            private double H; // threshold
            private int taus; // decay rate adjustment factor
            private bool contact; // signal-exceeds-threshold marker
            private bool lastContact;
            private int timeSinceLastOnset;
            private double Y0; // last filter level (for feedback)
            private uint onsetCount = 0;

            private RingBuffer<float> rmsBlock;
            private float[] rmsWindow;
            private int sampleCount;
            private int rmsSampleCount;
            private RingBuffer<float> rmsSampleBuffer; // needed for filtering

            private RingBuffer<int> onsetBuffer = new RingBuffer<int>(6); // needed for onset distribution and hash generation

            public BandAnalyzer(Profile profile, int band)
            {
                this.profile = profile;
                this.band = band;

                rmsBlock = new RingBuffer<float>(profile.OnsetRmsWindowSize);
                rmsWindow = WindowUtil.GetArray(
                    profile.OnsetRmsWindowType,
                    profile.OnsetRmsWindowSize
                );
                sampleCount = 0;
                rmsSampleCount = 0;
                rmsSampleBuffer = new RingBuffer<float>(FilterCoefficientsBn.Length * 2 + 1); // needed for filtering
            }

            public void ProcessSample(float energySample, Queue<SubFingerprint> hashes)
            {
                rmsBlock.Add(energySample);
                sampleCount++;

                // The incoming samples are windowed and aggregated to RMS values, making the RMS sample sequence
                // hop-size times shorter than the incoming sample sequence. Onsets will be detected from the
                // aggregated RMS sample sequence.

                if (
                    sampleCount >= profile.OnsetRmsWindowSize
                    && sampleCount % profile.OnsetRmsHopSize == 0
                )
                {
                    float rmsSample = 0;

                    // Compute RMS of a block
                    for (int k = 0; k < profile.OnsetRmsWindowSize; k++)
                    {
                        rmsSample += rmsBlock[k] * rmsWindow[k];
                    }
                    rmsSample = (float)Math.Sqrt(rmsSample);

                    // Initialize onset detector variables once the first rms sample is known
                    if (rmsSampleCount == 0)
                    {
                        H = rmsSample;
                        taus = 1;
                        contact = false;
                        lastContact = false;
                        timeSinceLastOnset = 0;
                        Y0 = 0;
                        onsetCount = 0;
                    }

                    rmsSampleBuffer.Add(rmsSample);
                    int onset = DetectOnset();
                    if (onset != -1 && onsetBuffer.Count == onsetBuffer.Length)
                    {
                        GenerateHashes(band, hashes);
                    }

                    rmsSampleCount++;
                }
            }

            public void Flush(Queue<SubFingerprint> hashes)
            {
                // Generate hashes for the last few onsets
                for (int i = 0; i < onsetBuffer.Count; i++)
                {
                    onsetBuffer.RemoveTail();
                    GenerateHashes(band, hashes);
                }
            }

            private int DetectOnset()
            {
                int onsetCountDelta = 0;

                double xn = 0; // signal level of current frame
                /* calculate the filter -  FIR part */
                if (rmsSampleCount >= 2 * FilterCoefficientsBn.Length)
                {
                    for (int k = 0; k < FilterCoefficientsBn.Length; ++k)
                    {
                        xn +=
                            FilterCoefficientsBn[k]
                            * (
                                rmsSampleBuffer[rmsSampleBuffer.Length - 1 - k]
                                - rmsSampleBuffer[
                                    rmsSampleBuffer.Length
                                        - 1
                                        - (2 * FilterCoefficientsBn.Length - k)
                                ]
                            );
                    }
                }
                /* IIR part */
                xn = xn + FilterCoefficientA1 * Y0;

                /* remember the last filtered level */
                Y0 = xn;

                // Check if the signal exceeds the threshold
                contact = (xn > H);

                if (contact)
                {
                    /* update with new threshold */
                    H = xn * profile.OnsetOverfact;
                }
                else
                {
                    /* apply decays */
                    H = H * Math.Exp(-1.0 / (double)taus);
                }

                // When the signal does not exceed the threshold anymore, but did in the last frame, we have an onset detected
                if (!contact && lastContact)
                {
                    // If the distance between the previous and current onset is too short, we replace the previous onset
                    if (
                        onsetCount > 0
                        && rmsSampleCount - onsetBuffer[onsetBuffer.Count - 1]
                            < profile.OnsetMinDistance
                    )
                    {
                        onsetBuffer.RemoveHead(); // TODO check if working correctly
                        --onsetCount;
                        onsetCountDelta--;
                    }

                    // Store the onset
                    onsetBuffer.Add(rmsSampleCount);
                    ++onsetCount;
                    timeSinceLastOnset = 0;
                    onsetCountDelta++;
                }
                ++timeSinceLastOnset;

                // Adjust the decay rate
                if (timeSinceLastOnset > profile.OnsetTargetDistance)
                {
                    // Increase decay above the target onset distance (makes it easier to detect an onset)
                    if (taus > 1)
                    {
                        taus--;
                    }
                }
                else
                {
                    // Decrease decay rate below target onset distance
                    taus++;
                }

                lastContact = contact;

                if (onsetCountDelta > 0 && onsetCount > 1)
                {
                    // Return the second most recent onset
                    // The current detected onset could still change, but detecting an onset means
                    // means that the previously detected onset is now fixed.
                    return onsetBuffer[onsetBuffer.Count - 2];
                }
                else
                {
                    return -1;
                }
            }

            /// <summary>
            /// The quantized_time_for_frame_delta and quantized_time_for_frame_absolute functions in the original
            /// Echoprint source are way too complicated and can be simplified to this function. The offset is omitted
            /// here as it is not needed.
            /// </summary>
            private int QuantizeFrameTime(int frame)
            {
                return (int)Math.Round(frame / profile.HashTimeQuantizationFactor);
            }

            private void GenerateHashes(int band, Queue<SubFingerprint> hashes)
            {
                if (onsetBuffer.Count > 2)
                {
                    byte[] hashMaterial = new byte[5];
                    // What time was this onset at?
                    int quantizedOnsetTime = QuantizeFrameTime(onsetBuffer[0]);

                    int[,] deltaPairs = new int[2, 6];

                    deltaPairs[0, 0] = (onsetBuffer[1] - onsetBuffer[0]);
                    deltaPairs[1, 0] = (onsetBuffer[2] - onsetBuffer[1]);
                    if (onsetBuffer.Count > 3)
                    {
                        deltaPairs[0, 1] = (onsetBuffer[1] - onsetBuffer[0]);
                        deltaPairs[1, 1] = (onsetBuffer[3] - onsetBuffer[1]);
                        deltaPairs[0, 2] = (onsetBuffer[2] - onsetBuffer[0]);
                        deltaPairs[1, 2] = (onsetBuffer[3] - onsetBuffer[2]);
                        if (onsetBuffer.Count > 4)
                        {
                            deltaPairs[0, 3] = (onsetBuffer[1] - onsetBuffer[0]);
                            deltaPairs[1, 3] = (onsetBuffer[4] - onsetBuffer[1]);
                            deltaPairs[0, 4] = (onsetBuffer[2] - onsetBuffer[0]);
                            deltaPairs[1, 4] = (onsetBuffer[4] - onsetBuffer[2]);
                            deltaPairs[0, 5] = (onsetBuffer[3] - onsetBuffer[0]);
                            deltaPairs[1, 5] = (onsetBuffer[4] - onsetBuffer[3]);
                        }
                    }

                    // For each pair emit a hash
                    // NOTE This always generates 6 hashes, even at the end of a band where < 6 pairs
                    //      are formed. Thats not really a problem though as their delta times will
                    //      always be zero, whereas valid pairs have delta times above zero; therefore
                    //      the chance is very low to get spurious collisions.
                    for (uint k = 0; k < 6; k++)
                    {
                        // Quantize the time deltas to 23ms
                        short deltaTime0 = (short)QuantizeFrameTime(deltaPairs[0, k]);
                        short deltaTime1 = (short)QuantizeFrameTime(deltaPairs[1, k]);
                        // Create a key from the time deltas and the band index
                        hashMaterial[0] = (byte)((deltaTime0 >> 8) & 0xFF);
                        hashMaterial[1] = (byte)((deltaTime0) & 0xFF);
                        hashMaterial[2] = (byte)((deltaTime1 >> 8) & 0xFF);
                        hashMaterial[3] = (byte)((deltaTime1) & 0xFF);
                        hashMaterial[4] = (byte)band;
                        uint hashCode = MurmurHash2.Hash(hashMaterial, HashSeed) & HashBitmask;

                        // Set the hash alongside the time of onset
                        hashes.Enqueue(
                            new SubFingerprint(
                                quantizedOnsetTime,
                                new SubFingerprintHash(hashCode),
                                false
                            )
                        );
                    }
                }
            }
        }

        /// <summary>
        /// This class collects hashes from different bands, sorts them internally by time,
        /// and fills a list with the sorted hashes.
        ///
        /// Fresh hashes should be written to the queue of the source band (Queues[i]),
        /// and sorted hashes should be fetched through the Fill method. When no more
        /// hashes are going to be added, the remaining ones can be fetched by flushing
        /// through the Fill method.
        /// </summary>
        private class HashTimeSorter
        {
            private Queue<SubFingerprint>[] queues;

            public HashTimeSorter(int bands)
            {
                queues = new Queue<SubFingerprint>[bands];
                for (int i = 0; i < bands; i++)
                {
                    queues[i] = new Queue<SubFingerprint>();
                }
            }

            /// <summary>
            /// Gets the array of queues to collect the hashes of the separate bands.
            /// </summary>
            public Queue<SubFingerprint>[] Queues
            {
                get { return queues; }
            }

            /// <summary>
            /// Fills a list with sorted hashes.
            /// </summary>
            /// <param name="list">the list to add the sorted hashes to</param>
            /// <param name="flush">If true, all remaining buffered hashes will be added to the list</param>
            /// <returns></returns>
            public int Fill(List<SubFingerprint> list, bool flush)
            {
                int hashesTransferred = 0;

                // This block loops until a queue is empty (default mode) or all queues
                // are empty (flush mode). In default mode, looping stops when a queue is
                // empty because the minimal frame index of the remaining filled queues
                // could still be higher than the frame index thats going to be filled next
                // into the empty queue.
                while (true)
                {
                    int minFrame = int.MaxValue;
                    int minFrameBand = -1;

                    for (int i = 0; i < queues.Length; i++)
                    {
                        // Find the lowest frame index
                        if (queues[i].Count == 0)
                        {
                            if (flush)
                            {
                                // When flushing, just skip the empty queues
                                continue;
                            }
                            minFrameBand = -1;
                            break;
                        }
                        else if (queues[i].Peek().Index < minFrame)
                        {
                            minFrame = queues[i].Peek().Index;
                            minFrameBand = i;
                        }
                    }

                    // Check loop break condition (one or all queues empty, depending on mode)
                    if (minFrameBand == -1)
                    {
                        return hashesTransferred;
                    }

                    // Transfer all hashes from the current minimal frame index to the output list
                    while (
                        queues[minFrameBand].Count > 0
                        && queues[minFrameBand].Peek().Index == minFrame
                    )
                    {
                        list.Add(queues[minFrameBand].Dequeue());
                        hashesTransferred++;
                    }
                }
            }
        }
    }
}
