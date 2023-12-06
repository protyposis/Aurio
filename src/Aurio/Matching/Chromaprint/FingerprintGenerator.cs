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

using System;
using System.Collections.Generic;
using System.Linq;
using Aurio.DataStructures;
using Aurio.Features;
using Aurio.Project;
using Aurio.Resampler;
using Aurio.Streams;

namespace Aurio.Matching.Chromaprint
{
    /// <summary>
    /// Chromaprint fingerprint generator as described in:
    /// - https://oxygene.sk/2010/07/introducing-chromaprint/
    /// - https://oxygene.sk/2011/01/how-does-chromaprint-work/
    /// - https://bitbucket.org/acoustid/chromaprint/
    /// </summary>
    public class FingerprintGenerator
    {
        private static readonly uint[] grayCodeMapping = { 0, 1, 3, 2 };
        private Profile profile;

        public event EventHandler<SubFingerprintsGeneratedEventArgs> SubFingerprintsGenerated;
        public event EventHandler Completed;

        public FingerprintGenerator(Profile profile)
        {
            this.profile = profile;
        }

        public void Generate(AudioTrack track)
        {
            IAudioStream audioStream = new ResamplingStream(
                new MonoStream(AudioStreamFactory.FromFileInfoIeee32(track.FileInfo)),
                ResamplingQuality.Medium,
                profile.SamplingRate
            );

            var window = WindowUtil.GetFunction(profile.WindowType, profile.WindowSize);
            var chroma = new Chroma(
                audioStream,
                window,
                profile.HopSize,
                profile.ChromaMinFrequency,
                profile.ChromaMaxFrequency,
                false,
                profile.ChromaMappingMode
            );

            float[] chromaFrame;
            var chromaBuffer = new RingBuffer<float[]>(profile.ChromaFilterCoefficients.Length);
            var chromaFilterCoefficients = profile.ChromaFilterCoefficients;
            var filteredChromaFrame = new double[Chroma.Bins];
            var classifiers = profile.Classifiers;
            var maxFilterWidth = classifiers.Max(c => c.Filter.Width);
            var integralImage = new IntegralImage(maxFilterWidth, Chroma.Bins);
            int index = 0;
            int indices = chroma.WindowCount;
            var subFingerprints = new List<SubFingerprint>();
            while (chroma.HasNext())
            {
                // Get chroma frame buffer
                // When the chroma buffer is full, we can take and reuse the oldest array
                chromaFrame =
                    chromaBuffer.Count == chromaBuffer.Length
                        ? chromaBuffer[0]
                        : new float[Chroma.Bins];

                // Read chroma frame into buffer
                chroma.ReadFrame(chromaFrame);

                // ChromaFilter
                chromaBuffer.Add(chromaFrame);
                if (chromaBuffer.Count < chromaBuffer.Length)
                {
                    // Wait for the buffer to fill completely for the filtering to start
                    continue;
                }
                Array.Clear(filteredChromaFrame, 0, filteredChromaFrame.Length);
                for (int i = 0; i < chromaFilterCoefficients.Length; i++)
                {
                    var frame = chromaBuffer[i];
                    for (int j = 0; j < frame.Length; j++)
                    {
                        filteredChromaFrame[j] += frame[j] * chromaFilterCoefficients[i];
                    }
                }

                // ChromaNormalizer
                double euclideanNorm = 0;
                for (int i = 0; i < filteredChromaFrame.Length; i++)
                {
                    var value = filteredChromaFrame[i];
                    euclideanNorm += value * value;
                }
                euclideanNorm = Math.Sqrt(euclideanNorm);
                if (euclideanNorm < profile.ChromaNormalizationThreshold)
                {
                    Array.Clear(filteredChromaFrame, 0, filteredChromaFrame.Length);
                }
                else
                {
                    for (int i = 0; i < filteredChromaFrame.Length; i++)
                    {
                        filteredChromaFrame[i] /= euclideanNorm;
                    }
                }

                // ImageBuilder
                // ... just add one feature vector after another as rows to the image
                integralImage.AddColumn(filteredChromaFrame);

                // FingerprintCalculator
                if (integralImage.Columns < maxFilterWidth)
                {
                    // Wait for the image to fill completely before hashes can be generated
                    continue;
                }
                // Calculate subfingerprint hash
                uint hash = 0;
                for (int i = 0; i < classifiers.Length; i++)
                {
                    hash = (hash << 2) | grayCodeMapping[classifiers[i].Classify(integralImage, 0)];
                }
                // We have a SubFingerprint@frameTime
                subFingerprints.Add(new SubFingerprint(index, new SubFingerprintHash(hash), false));

                index++;

                if (index % 512 == 0 && SubFingerprintsGenerated != null)
                {
                    SubFingerprintsGenerated(
                        this,
                        new SubFingerprintsGeneratedEventArgs(
                            track,
                            subFingerprints,
                            index,
                            indices
                        )
                    );
                    subFingerprints.Clear();
                }
            }

            if (SubFingerprintsGenerated != null)
            {
                SubFingerprintsGenerated(
                    this,
                    new SubFingerprintsGeneratedEventArgs(track, subFingerprints, index, indices)
                );
            }

            if (Completed != null)
            {
                Completed(this, EventArgs.Empty);
            }

            audioStream.Close();
        }

        public static Profile[] GetProfiles()
        {
            return new Profile[] { new DefaultProfile(), new SyncProfile() };
        }
    }
}
