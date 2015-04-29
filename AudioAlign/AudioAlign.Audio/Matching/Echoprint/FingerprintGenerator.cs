using AudioAlign.Audio.DataStructures;
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

        public event EventHandler<FingerprintCodeEventArgs> FingerprintHashesGenerated;

        /// <summary>
        /// This method generates hash codes from an audio stream in a streaming fashion,
        /// which means that it only maintains a small consntat-size state and can process
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
        ///   hash codes   <-------------------+ code generation <----------+        
        ///                                    +-----------------+                   
        /// 
        /// The hash codes from the code generators of each band are then sent though a code
        /// sorter which brings them into sequential temporal order before they are stored
        /// in the final list.
        /// </summary>
        /// <param name="track"></param>
        public void Generate(AudioTrack track) {
            IAudioStream audioStream = new ResamplingStream(
                new MonoStream(AudioStreamFactory.FromFileInfoIeee32(track.FileInfo)),
                ResamplingQuality.Medium, 11025);

            var whiteningStream = new WhiteningStream(audioStream, 40, 8, 10000);
            var subbandAnalyzer = new SubbandAnalyzer(whiteningStream);

            float[] analyzedFrame = new float[SubBands];

            var bandAnalyzers = new BandAnalyzer[SubBands];
            for (int i = 0; i < SubBands; i++) {
                bandAnalyzers[i] = new BandAnalyzer(i);
            }

            List<FPCode> codes = new List<FPCode>();
            CodeSorter codeSorter = new CodeSorter(SubBands);

            var sw = new Stopwatch();
            sw.Start();

            int totalFrames = subbandAnalyzer.WindowCount;
            int currentFrame = 0;
            while (subbandAnalyzer.HasNext()) {
                subbandAnalyzer.ReadFrame(analyzedFrame);

                for (int i = 0; i < SubBands; i++) {
                    bandAnalyzers[i].ProcessSample(analyzedFrame[i], codeSorter.Queues[i]);
                }
                
                if (currentFrame % 4096 == 0) {
                    codeSorter.Fill(codes, false);

                    if (FingerprintHashesGenerated != null) {
                        FingerprintHashesGenerated(this, new FingerprintCodeEventArgs {
                            AudioTrack = track,
                            Index = currentFrame,
                            Indices = totalFrames,
                            Codes = codes
                        });
                        codes.Clear();
                    }
                }

                currentFrame++;
            }

            for (int i = 0; i < bandAnalyzers.Length; i++) {
                bandAnalyzers[i].Flush(codeSorter.Queues[i]);
            }
            codeSorter.Fill(codes, true);

            if (FingerprintHashesGenerated != null) {
                FingerprintHashesGenerated(this, new FingerprintCodeEventArgs {
                    AudioTrack = track,
                    Index = currentFrame,
                    Indices = totalFrames,
                    Codes = codes
                });
                codes.Clear();
            }

            sw.Stop();
            Console.WriteLine("time: " + sw.Elapsed);
        }

        /// <summary>
        /// Analyzes a subband for onsets and generates hash codes.
        /// </summary>
        private class BandAnalyzer {

            private const int MinOnsetDistance = 128;
            private const double Overfact = 1.1;  /* threshold rel. to actual peak */ // paper says 1.05
            private const int TargetOnsetDistance = 345; // 345 ~= 1 sec (11025 / 8 [subband downsampling factor in SubbandAnalyzer] / 4 [RMS downsampling in adaptiveOnsets()] ~= 345 frames per second)
            private const int RmsWindowSize = 8;
            private const int RmsHopSize = 4;

            private static readonly double[] FilterCoefficientsBn = { 0.1883, 0.4230, 0.3392 }; /* preemph filter */
            private const double FilterCoefficientA1 = 0.98; // feedback filter coefficient

            private int band;
            
            private double H; // threshold
            private int taus; // decay rate adjustment factor
            private bool contact; // signal-exceeds-threshold marker
            private bool lastContact;
            private int timeSinceLastOnset;
            private double Y0; // last filter level (for feedback)
            private uint onsetCount = 0;
            
            private RingBuffer<float> rmsBlock = new RingBuffer<float>(RmsWindowSize);
            private float[] rmsWindow = WindowUtil.GetArray(WindowType.Hann, RmsWindowSize);
            private int sampleCount = 0;
            private int rmsSampleCount = 0;
            private RingBuffer<float> rmsSampleBuffer = new RingBuffer<float>(FilterCoefficientsBn.Length * 2 + 1); // needed for filtering

            private RingBuffer<int> onsetBuffer = new RingBuffer<int>(6);

            public BandAnalyzer(int band) {
                this.band = band;
            }

            public void ProcessSample(float energySample, Queue<FPCode> codes) {
                rmsBlock.Add(energySample);
                sampleCount++;

                // The incoming samples are windowed and aggregated to RMS values, making the RMS sample sequence
                // hop-size times shorter than the incoming sample sequence. Onsets will be detected from the
                // aggregated RMS sample sequence.

                if (sampleCount >= 8 && sampleCount % RmsHopSize == 0) {
                    float rmsSample = 0;

                    // Compute RMS of a block
                    for (int k = 0; k < RmsWindowSize; k++) {
                        rmsSample += rmsBlock[k] * rmsWindow[k];
                    }
                    rmsSample = (float)Math.Sqrt(rmsSample);

                    // Initialize onset detector variables once the first rms sample is known
                    if (rmsSampleCount == 0) {
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
                    if (onset != -1 && onsetBuffer.Count == onsetBuffer.Length) {
                        GenerateCodes(band, codes);
                    }

                    rmsSampleCount++;
                }
            }

            public void Flush(Queue<FPCode> codes) {
                // Generate codes for last few onsets
                for (int i = 0; i < onsetBuffer.Count; i++) {
                    onsetBuffer.RemoveTail();
                    GenerateCodes(band, codes);
                }
            }

            private int DetectOnset() {
                int onsetCountDelta = 0;

                double xn = 0; // signal level of current frame
                /* calculate the filter -  FIR part */
                if (rmsSampleCount >= 2 * FilterCoefficientsBn.Length) {
                    for (int k = 0; k < FilterCoefficientsBn.Length; ++k) {
                        xn += FilterCoefficientsBn[k] * (rmsSampleBuffer[rmsSampleBuffer.Length - 1 - k] - rmsSampleBuffer[rmsSampleBuffer.Length - 1 - (2 * FilterCoefficientsBn.Length - k)]);
                    }
                }
                /* IIR part */
                xn = xn + FilterCoefficientA1 * Y0;

                /* remember the last filtered level */
                Y0 = xn;

                // Check if the signal exceeds the threshold
                contact = (xn > H);

                if (contact) {
                    /* update with new threshold */
                    H = xn * Overfact;
                }
                else {
                    /* apply decays */
                    H = H * Math.Exp(-1.0 / (double)taus);
                }

                // When the signal does not exceed the threshold anymore, but did in the last frame, we have an onset detected
                if (!contact && lastContact) {
                    // If the distance between the previous and current onset is too short, we replace the previous onset
                    if (onsetCount > 0 && rmsSampleCount - onsetBuffer[onsetBuffer.Count - 1] < MinOnsetDistance) {
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
                if (timeSinceLastOnset > TargetOnsetDistance) {
                    // Increase decay above the target onset distance (makes it easier to detect an onset)
                    if (taus > 1) {
                        taus--;
                    }
                }
                else {
                    // Decrease decay rate below target onset distance
                    taus++;
                }

                lastContact = contact;

                if (onsetCountDelta > 0 && onsetCount > 1) {
                    // Return the second most recent onset
                    // The current detected onset could still change, but detecting an onset means
                    // means that the previously detected onset is now fixed.
                    return onsetBuffer[onsetBuffer.Count - 2];
                }
                else {
                    return -1;
                }
            }

            /// <summary>
            /// The quantized_time_for_frame_delta and quantized_time_for_frame_absolute functions in the original
            /// Echoprint source are way too complicated and can be simplified to this function. The offset is omitted
            /// here as it is not needed.
            /// </summary>
            private static int QuantizeFrameTime(int frame) {
                return (int)Math.Round(frame / 8d);
            }

            private void GenerateCodes(int band, Queue<FPCode> codes) {
                if (onsetBuffer.Count > 2) {
                    byte[] hashMaterial = new byte[5];
                    // What time was this onset at?
                    int quantizedOnsetTime = QuantizeFrameTime(onsetBuffer[0]);

                    int[,] deltaPairs = new int[2, 6];

                    deltaPairs[0, 0] = (onsetBuffer[1] - onsetBuffer[0]);
                    deltaPairs[1, 0] = (onsetBuffer[2] - onsetBuffer[1]);
                    if (onsetBuffer.Count > 3) {
                        deltaPairs[0, 1] = (onsetBuffer[1] - onsetBuffer[0]);
                        deltaPairs[1, 1] = (onsetBuffer[3] - onsetBuffer[1]);
                        deltaPairs[0, 2] = (onsetBuffer[2] - onsetBuffer[0]);
                        deltaPairs[1, 2] = (onsetBuffer[3] - onsetBuffer[2]);
                        if (onsetBuffer.Count > 4) {
                            deltaPairs[0, 3] = (onsetBuffer[1] - onsetBuffer[0]);
                            deltaPairs[1, 3] = (onsetBuffer[4] - onsetBuffer[1]);
                            deltaPairs[0, 4] = (onsetBuffer[2] - onsetBuffer[0]);
                            deltaPairs[1, 4] = (onsetBuffer[4] - onsetBuffer[2]);
                            deltaPairs[0, 5] = (onsetBuffer[3] - onsetBuffer[0]);
                            deltaPairs[1, 5] = (onsetBuffer[4] - onsetBuffer[3]);
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
                        hashMaterial[4] = (byte)band;
                        uint hashCode = MurmurHash2.Hash(hashMaterial, HashSeed) & HashBitmask;

                        // Set the code alongside the time of onset
                        codes.Enqueue(new FPCode((uint)quantizedOnsetTime, hashCode));
                    }
                }
            }
        }

        /// <summary>
        /// This class collects codes from different bands, sorts them internally,
        /// and fills a list with the sorted codes.
        /// 
        /// Fresh codes should be written to the queue of the source band (Queues[i]), 
        /// and sorted codes should be fetched through the Fill method. When no more
        /// codes are going to be written, the remaining ones can be fetched by flushing
        /// through the Fill method.
        /// </summary>
        private class CodeSorter {

            private Queue<FPCode>[] queues;
            private int frame;

            public CodeSorter(int bands) {
                queues = new Queue<FPCode>[bands];
                for(int i = 0; i < bands; i++) {
                    queues[i] = new Queue<FPCode>();
                }
            }

            /// <summary>
            /// Gets the array of queues to collect the codes of the separate bands.
            /// </summary>
            public Queue<FPCode>[] Queues {
                get { return queues; }
            }

            /// <summary>
            /// Fills a list with sorted codes.
            /// </summary>
            /// <param name="list">the list to add the sorted codes to</param>
            /// <param name="flush">If true, all buffered codes will be added to the list</param>
            /// <returns></returns>
            public int Fill(List<FPCode> list, bool flush) {
                int codesTransferred = 0;

                // This block loops until a queue is empty (default mode) or all queues
                // are empty (flush mode). In default mode, looping stops when a queue is
                // empty because the minimal frame index of the remaining filled queues
                // could still be higher than the frame index thats going to be filled next 
                // into the empty queue.
                while (true) {
                    uint minFrame = int.MaxValue;
                    int minFrameBand = -1;

                    for (int i = 0; i < queues.Length; i++) {
                        // Find the lowest frame index
                        if (queues[i].Count == 0) {
                            if (flush) {
                                // When flushing, just skip the empty queues
                                continue;
                            }
                            minFrameBand = -1;
                            break;
                        }
                        else if (queues[i].Peek().Frame < minFrame) {
                            minFrame = queues[i].Peek().Frame;
                            minFrameBand = i;
                        }
                    }

                    // Check loop break condition (one or all queues empty, depending on mode)
                    if (minFrameBand == -1) {
                        return codesTransferred;
                    }

                    // Transfer all codes from the current minimal frame index to the output list
                    while (queues[minFrameBand].Count > 0 && queues[minFrameBand].Peek().Frame == minFrame) {
                        list.Add(queues[minFrameBand].Dequeue());
                        codesTransferred++;
                    }
                }
            }
        }
    }
}
