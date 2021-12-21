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
using Aurio.Features;
using Aurio.Project;
using Aurio.Resampler;
using Aurio.Streams;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Aurio.Matching.Wang2003
{
    /// <summary>
    /// Generates fingerprints according to what is described in:
    /// - Wang, Avery. "An Industrial Strength Audio Search Algorithm." ISMIR. 2003.
    /// - Kennedy, Lyndon, and Mor Naaman. "Less talk, more rock: automated organization 
    ///   of community-contributed collections of concert videos." Proceedings of the 
    ///   18th international conference on World wide web. ACM, 2009.
    /// </summary>
    public class FingerprintGenerator
    {

        private const float spectrumMinThreshold = -200; // dB volume

        private Profile profile;

        public event EventHandler<FrameProcessedEventArgs> FrameProcessed;
        public event EventHandler<SubFingerprintsGeneratedEventArgs> SubFingerprintsGenerated;

        public FingerprintGenerator(Profile profile)
        {
            this.profile = profile;
        }

        public void Generate(AudioTrack track)
        {
            IAudioStream audioStream = new ResamplingStream(
                new MonoStream(AudioStreamFactory.FromFileInfoIeee32(track.FileInfo)),
                ResamplingQuality.Medium, profile.SamplingRate);

            STFT stft = new STFT(audioStream, profile.WindowSize, profile.HopSize, WindowType.Hann, STFT.OutputFormat.Decibel);
            int index = 0;
            int indices = stft.WindowCount;
            int processedFrames = 0;

            float[] spectrum = new float[profile.WindowSize / 2];
            float[] smoothedSpectrum = new float[spectrum.Length - profile.SpectrumSmoothingLength + 1]; // the smooved frequency spectrum of the current frame
            var spectrumSmoother = new SimpleMovingAverage(profile.SpectrumSmoothingLength);
            float[] spectrumTemporalAverage = new float[spectrum.Length]; // a running average of each spectrum bin over time
            float[] spectrumResidual = new float[spectrum.Length]; // the difference between the current spectrum and the moving average spectrum

            var peakHistory = new PeakHistory(1 + profile.TargetZoneDistance + profile.TargetZoneLength, spectrum.Length / 2);
            var peakPairs = new List<PeakPair>(profile.PeaksPerFrame * profile.PeakFanout); // keep a single instance of the list to avoid instantiation overhead

            var subFingerprints = new List<SubFingerprint>();

            while (stft.HasNext())
            {
                // Get the FFT spectrum
                stft.ReadFrame(spectrum);

                // Skip frames whose average spectrum volume is below the threshold
                // This skips silent frames (zero samples) that only contain very low noise from the FFT 
                // and that would screw up the temporal spectrum average below for the following frames.
                if (spectrum.Average() < spectrumMinThreshold)
                {
                    index++;
                    continue;
                }

                // Smooth the frequency spectrum to remove small peaks
                if (profile.SpectrumSmoothingLength > 0)
                {
                    spectrumSmoother.Clear();
                    for (int i = 0; i < spectrum.Length; i++)
                    {
                        var avg = spectrumSmoother.Add(spectrum[i]);
                        if (i >= profile.SpectrumSmoothingLength)
                        {
                            smoothedSpectrum[i - profile.SpectrumSmoothingLength] = avg;
                        }
                    }
                }

                // Update the temporal moving bin average
                if (processedFrames == 0)
                {
                    // Init averages on first frame
                    for (int i = 0; i < spectrum.Length; i++)
                    {
                        spectrumTemporalAverage[i] = spectrum[i];
                    }
                }
                else
                {
                    // Update averages on all subsequent frames
                    for (int i = 0; i < spectrum.Length; i++)
                    {
                        spectrumTemporalAverage[i] = ExponentialMovingAverage.UpdateMovingAverage(
                            spectrumTemporalAverage[i], profile.SpectrumTemporalSmoothingCoefficient, spectrum[i]);
                    }
                }

                // Calculate the residual
                // The residual is the difference of the current spectrum to the temporal average spectrum. The higher
                // a bin residual is, the steeper the increase in energy in that peak.
                for (int i = 0; i < spectrum.Length; i++)
                {
                    spectrumResidual[i] = spectrum[i] - spectrumTemporalAverage[i] - 90f;
                }

                // Find local peaks in the residual
                // The advantage of finding peaks in the residual instead of the spectrum is that spectrum energy is usually
                // concentrated in the low frequencies, resulting in a clustering of the highest peaks in the lows. Getting
                // peaks from the residual distributes the peaks more evenly across the spectrum.
                var peaks = peakHistory.List; // take oldest list,
                peaks.Clear(); // clear it, and
                FindLocalMaxima(spectrumResidual, peaks); // refill with new peaks

                // Pick the largest n peaks
                int numMaxima = Math.Min(peaks.Count, profile.PeaksPerFrame);
                if (numMaxima > 0)
                {
                    peaks.Sort((p1, p2) => p1.Value == p2.Value ? 0 : p1.Value < p2.Value ? 1 : -1); // order peaks by height
                    if (peaks.Count > numMaxima)
                    {
                        peaks.RemoveRange(numMaxima, peaks.Count - numMaxima); // select the n tallest peaks by deleting the rest
                    }
                    peaks.Sort((p1, p2) => p1.Index == p2.Index ? 0 : p1.Index < p2.Index ? -1 : 1); // sort peaks by index (not really necessary)
                }

                peakHistory.Add(index, peaks);

                if (FrameProcessed != null)
                {
                    // Mark peaks as 0dB for spectrogram display purposes
                    foreach (var peak in peaks)
                    {
                        spectrum[peak.Index] = 0;
                        spectrumResidual[peak.Index] = 0;
                    }

                    FrameProcessed(this, new FrameProcessedEventArgs
                    {
                        AudioTrack = track,
                        Index = index,
                        Indices = indices,
                        Spectrum = spectrum,
                        SpectrumResidual = spectrumResidual
                    });
                }

                processedFrames++;
                index++;

                if (processedFrames >= peakHistory.Length)
                {
                    peakPairs.Clear();
                    FindPairsWithMaxEnergy(peakHistory, peakPairs);
                    ConvertPairsToSubFingerprints(peakPairs, subFingerprints);
                }

                if (subFingerprints.Count > 512)
                {
                    FireFingerprintHashesGenerated(track, indices, subFingerprints);
                    subFingerprints.Clear();
                }
            }

            // Flush the remaining peaks of the last frames from the history to get all remaining pairs
            for (int i = 0; i < profile.TargetZoneLength; i++)
            {
                var peaks = peakHistory.List;
                peaks.Clear();
                peakHistory.Add(-1, peaks);
                peakPairs.Clear();
                FindPairsWithMaxEnergy(peakHistory, peakPairs);
                ConvertPairsToSubFingerprints(peakPairs, subFingerprints);
            }
            FireFingerprintHashesGenerated(track, indices, subFingerprints);

            audioStream.Close();
        }

        /// <summary>
        /// Local peak picking works as follows: 
        /// A local peak is always a highest value surrounded by lower values. 
        /// In case of a plateau, the index of the first plateu value marks the peak.
        /// 
        ///      |      |    |
        ///      |      |    |
        ///      v      |    |
        ///      ___    |    |
        ///     /   \   v    v
        ///   _/     \       /\_
        ///  /        \_/\  /   \
        /// /             \/     \
        /// </summary>
        private void FindLocalMaxima(float[] data, List<Peak> peakList)
        {
            float val;
            float lastVal = float.MinValue;
            int anchorIndex = -1;
            for (int i = 0; i < data.Length; i++)
            {
                val = data[i];

                if (val > lastVal)
                {
                    // Climbing an increasing slope to a local maximum
                    anchorIndex = i;
                }
                else if (val == lastVal)
                {
                    // Plateau
                    // anchorIndex stays the same, as the maximum is always at the beginning of a plateau
                }
                else
                {
                    // Value is decreasing, going down the decreasing slope
                    if (anchorIndex > -1)
                    {
                        // Local maximum found
                        // The first decrease always comes after a peak (or plateau), 
                        // so the last set anchorIndex is the index of the peak.
                        peakList.Add(new Peak(anchorIndex, lastVal));
                        anchorIndex = -1;
                    }
                }

                lastVal = val;
            }

            //Debug.WriteLine("{0} local maxima found", maxima.Count);
        }

        /// <summary>
        /// Builds pairs of peaks from the current frame and its target area.
        /// 
        /// This is a very naive approach that just iterates linearly through frames and their peaks 
        /// and generates a pair if the constraints of the target area permit, until the max number
        /// of pairs has been generated.
        /// </summary>
        /// <param name="peakHistory">the history structure to read the peaks from</param>
        /// <param name="peakPairs">the list to store the pairs in</param>
        private void FindPairsNaive(PeakHistory peakHistory, List<PeakPair> peakPairs)
        {
            var halfWidth = profile.TargetZoneWidth / 2;

            var index = peakHistory.Index;
            foreach (var peak in peakHistory.Lists[0])
            {
                int count = 0;
                for (int distance = profile.TargetZoneDistance; distance < peakHistory.Length; distance++)
                {
                    foreach (var targetPeak in peakHistory.Lists[distance])
                    {
                        if (peak.Index >= targetPeak.Index - halfWidth && peak.Index <= targetPeak.Index + halfWidth)
                        {
                            peakPairs.Add(new PeakPair { Index = index, Peak1 = peak, Peak2 = targetPeak, Distance = distance });
                            if (++count >= profile.PeakFanout)
                            {
                                break;
                            }
                        }
                    }
                    if (count >= profile.PeakFanout)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Builds pairs of peaks from the current frame and its target area.
        /// 
        /// This approach generates all possible pairs and then picks the most distinct ones
        /// according to their average peak energy. The idea is that these peaks are the ones
        /// that most probably survive in high noise environments.
        /// This approach takes a bit longer to compute compared to the naive approach, but
        /// generates much more diverse peaks, spread more evenly across the hash space (this is 
        /// just a speculation; not validated). Compared to the naive approach, this results 
        /// in much faster hash matching and also a lot more matches.
        /// </summary>
        /// <param name="peakHistory">the history structure to read the peaks from</param>
        /// <param name="peakPairs">the list to store the pairs in</param>
        private void FindPairsWithMaxEnergy(PeakHistory peakHistory, List<PeakPair> peakPairs)
        {
            var halfWidth = profile.TargetZoneWidth / 2;

            // Get pairs from peaks
            // This is a very naive approach that can be improved, e.g. by taking the average peak value into account,
            // which would result in a list of the most prominent peak pairs.
            // For now, this just iterates linearly through frames and their peaks and generates a pair if the
            // constraints of the target area permit, until the max number of pairs has been generated.
            var index = peakHistory.Index;
            foreach (var peak in peakHistory.Lists[0])
            {
                for (int distance = profile.TargetZoneDistance; distance < peakHistory.Length; distance++)
                {
                    foreach (var targetPeak in peakHistory.Lists[distance])
                    {
                        if (peak.Index >= targetPeak.Index - halfWidth && peak.Index <= targetPeak.Index + halfWidth)
                        {
                            peakPairs.Add(new PeakPair { Index = index, Peak1 = peak, Peak2 = targetPeak, Distance = distance });
                        }
                    }
                }
            }

            peakPairs.Sort((pp1, pp2) =>
            {
                var avg1 = pp1.AverageEnergy;
                var avg2 = pp2.AverageEnergy;

                if (avg1 < avg2)
                {
                    return 1;
                }
                else if (avg1 > avg2)
                {
                    return -1;
                }

                return 0;
            });

            int maxPeaks = Math.Min(profile.PeakFanout, peakPairs.Count);
            if (peakPairs.Count > maxPeaks)
            {
                peakPairs.RemoveRange(maxPeaks, peakPairs.Count - maxPeaks); // select the n most prominent peak pairs
            }
        }

        private void ConvertPairsToSubFingerprints(List<PeakPair> peakPairs, List<SubFingerprint> subFingerprints)
        {
            // This sorting step is needed for the Zipper intersection algorithm in the fingerprint 
            // store to find matching hashes, which expects them sorted by frame index. Sorting works
            // because the index is coded in the most significant bits of the hashes.
            var hashes = peakPairs.ConvertAll(pp => new SubFingerprintHash(PeakPair.PeakPairToHash(pp)));
            hashes.Sort();
            subFingerprints.AddRange(hashes.ConvertAll(h => new SubFingerprint(peakPairs[0].Index, h, false)));
        }

        private void FireFingerprintHashesGenerated(AudioTrack track, int indices, List<SubFingerprint> subFingerprints)
        {
            if (SubFingerprintsGenerated != null)
            {
                SubFingerprintsGenerated(this, new SubFingerprintsGeneratedEventArgs(track, subFingerprints, subFingerprints[0].Index, indices));
            }
        }

        public static Profile[] GetProfiles()
        {
            return new Profile[] { new DefaultProfile() };
        }

        [DebuggerDisplay("{index}/{value}")]
        private struct Peak
        {

            private int index;
            private float value;

            public Peak(int index, float value)
            {
                this.index = index;
                this.value = value;
            }
            public int Index { get { return index; } }
            public float Value { get { return value; } }
        }

        [DebuggerDisplay("{Index}:{Peak1.Index} --({Distance})--> {Peak2.Index}")]
        private struct PeakPair
        {
            public int Index { get; set; }
            public Peak Peak1 { get; set; }
            public Peak Peak2 { get; set; }
            public int Distance { get; set; }
            public float AverageEnergy { get { return (Peak1.Value + Peak2.Value) / 2; } }

            public static uint PeakPairToHash(PeakPair pp)
            {
                // Put frequency bins and the distance each in one byte. The actual quantization
                // is configured through the parameters, e.g. the FFT window size determines the
                // number of frequency bins, and the size of the target zone determines the max
                // distance. Their max size can be anywhere in the range of a byte. if it should be 
                // higher, a quantization step must be introduced (which will basically be a division).
                return (uint)((byte)pp.Peak1.Index << 16 | (byte)pp.Peak2.Index << 8 | (byte)pp.Distance);
            }

            public static PeakPair HashToPeakPair(uint hash, int index)
            {
                // The inverse operation of the function above.
                return new PeakPair
                {
                    Index = index,
                    Peak1 = new Peak((int)(hash >> 16 & 0xFF), 0),
                    Peak2 = new Peak((int)(hash >> 8 & 0xFF), 0),
                    Distance = (int)(hash & 0xFF)
                };
            }
        }

        /// <summary>
        /// Helper class to encapsulate the management of the two FIFOs for building a 
        /// peak history over time which is needed to calculate peak pairs. This history
        /// needs to contain the whole target zone of each peak.
        /// The first history entry, which is the oldest, is always the one for which
        /// pairs are calculated. After calculation, the oldest entries can be taken
        /// through the Index/List properties, updated with new data and readded to the history,
        /// which moves the second oldest entry to the first position (thus, it becomes the oldest),
        /// and the oldest entry gets reused and added as the most recent to the end.
        /// </summary>
        private class PeakHistory
        {

            private RingBuffer<int> indexHistory; // a FIFO list of peak list indices
            private RingBuffer<List<Peak>> peakHistory; // a FIFO list of peak lists

            public PeakHistory(int length, int maxPeaksPerFrame)
            {
                indexHistory = new RingBuffer<int>(length);
                peakHistory = new RingBuffer<List<Peak>>(length);

                // Instantiate peak lists for later reuse
                for (int i = 0; i < length; i++)
                {
                    indexHistory.Add(-1);
                    peakHistory.Add(new List<Peak>(maxPeaksPerFrame));
                }
            }

            /// <summary>
            /// The capacity of the history.
            /// </summary>
            public int Length
            {
                get { return peakHistory.Length; }
            }

            /// <summary>
            /// The number of elements in the history.
            /// This always equals to the Length because it gets pre-filled
            /// at construction time.
            /// </summary>
            public int Count
            {
                get { return peakHistory.Count; }
            }

            /// <summary>
            /// The current (oldest) index.
            /// This is the index of the peak list that pairs are calculated for.
            /// </summary>
            public int Index
            {
                get { return indexHistory[0]; }
            }

            /// <summary>
            /// The current (oldest) peak list. 
            /// This is the peak list that pairs are calculated for.
            /// </summary>
            public List<Peak> List
            {
                get { return peakHistory[0]; }
            }

            /// <summary>
            /// Gets the FIFO queue of the indices.
            /// </summary>
            public RingBuffer<int> Indices
            {
                get { return indexHistory; }
            }

            /// <summary>
            /// Gets the FIFO queue of the peak lists.
            /// </summary>
            public RingBuffer<List<Peak>> Lists
            {
                get { return peakHistory; }
            }

            /// <summary>
            /// Adds an indexed list to the top (most recent position) of the FIFO queue.
            /// </summary>
            /// <param name="index"></param>
            /// <param name="list"></param>
            public void Add(int index, List<Peak> list)
            {
                indexHistory.Add(index);
                peakHistory.Add(list);
            }

            public void DebugPrint()
            {
                Console.WriteLine("--------");
                for (int i = 0; i < Length; i++)
                {
                    Console.Write(indexHistory[i] + ": ");
                    foreach (var peak in peakHistory[i])
                    {
                        Console.Write(peak.Index + " ");
                    }
                    Console.WriteLine("");
                }
                Console.WriteLine("--------");
            }
        }
    }
}
