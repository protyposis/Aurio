// 
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2016  Mario Guggenberger <mg@protyposis.net>
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
using System.Text;
using Aurio.Project;
using Aurio.Streams;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Aurio.Features;

namespace Aurio.Matching.HaitsmaKalker2002 {
    /// <summary>
    /// Generates fingerprints according to what is described in:
    /// - Haitsma, Jaap, and Ton Kalker. "A highly robust audio fingerprinting system." ISMIR. 2002.
    /// </summary>
    public class FingerprintGenerator {

        private const int STREAM_INPUT_BUFFER_SIZE = 32768;

        private AudioTrack inputTrack;
        private Profile profile;

        private int flipWeakestBits;

        public event EventHandler<SubFingerprintsGeneratedEventArgs> SubFingerprintsGenerated;
        public event EventHandler Completed;

        public FingerprintGenerator(Profile profile, AudioTrack track) {
            this.inputTrack = track;
            this.profile = profile;
            this.flipWeakestBits = profile.FlipWeakestBits;
        }

        private int index;
        private int indices;
        private float[] frameBuffer;
        private float[] bands = new float[33];
        private float[] bandsPrev = new float[33];

        public void Generate() {
            IAudioStream audioStream = new ResamplingStream(
                new MonoStream(AudioStreamFactory.FromFileInfoIeee32(inputTrack.FileInfo)),
                ResamplingQuality.Medium, profile.SampleRate);

            STFT stft = new STFT(audioStream, profile.FrameSize, profile.FrameStep, WindowType.Hann, STFT.OutputFormat.Decibel);
            index = 0;
            indices = stft.WindowCount;

            frameBuffer = new float[profile.FrameSize / 2];
            List<SubFingerprint> subFingerprints = new List<SubFingerprint>();

            while (stft.HasNext()) {
                // Get FFT spectrum
                stft.ReadFrame(frameBuffer);

                // Sum FFT bins into target frequency bands
                profile.MapFrequencies(frameBuffer, bands);

                CalculateSubFingerprint(bandsPrev, bands, subFingerprints);

                CommonUtil.Swap<float[]>(ref bands, ref bandsPrev);
                index++;

                // Output subfingerprints every once in a while
                if (index % 512 == 0 && SubFingerprintsGenerated != null) {
                    SubFingerprintsGenerated(this, new SubFingerprintsGeneratedEventArgs(inputTrack, subFingerprints, index, indices));
                    subFingerprints.Clear();
                }
            }

            // Output remaining subfingerprints
            if (SubFingerprintsGenerated != null) {
                SubFingerprintsGenerated(this, new SubFingerprintsGeneratedEventArgs(inputTrack, subFingerprints, index, indices));
            }

            if (Completed != null) {
                Completed(this, EventArgs.Empty);
            }

            audioStream.Close();
        }

        private void CalculateSubFingerprint(float[] energyBands, float[] previousEnergyBands, List<SubFingerprint> list) {
            SubFingerprintHash hash = new SubFingerprintHash();
            Dictionary<int, float> bitReliability = new Dictionary<int, float>();
            List<SubFingerprint> subFingerprints = new List<SubFingerprint>(1 + flipWeakestBits);

            for (int m = 0; m < 32; m++) {
                float difference = energyBands[m] - energyBands[m + 1] - (previousEnergyBands[m] - previousEnergyBands[m + 1]);
                hash[m] = difference > 0;
                bitReliability.Add(m, difference > 0 ? difference : -difference); // take absolute value as reliability weight
            }

            list.Add(new SubFingerprint(index, hash, false));

            if (flipWeakestBits > 0) {
                // calculate probable hashes by flipping the most unreliable bits (the bits with the least energy differences)
                List<int> weakestBits = new List<int>(bitReliability.Keys.OrderBy(key => bitReliability[key]));
                // generate hashes with all possible bit combinations flipped
                int variations = 1 << flipWeakestBits;
                for (int i = 1; i < variations; i++) { // start at 1 since i0 equals to the original hash
                    SubFingerprintHash flippedHash = new SubFingerprintHash(hash.Value);
                    for (int j = 0; j < flipWeakestBits; j++) {
                        if (((i >> j) & 1) == 1) {
                            flippedHash[weakestBits[j]] = !flippedHash[weakestBits[j]];
                        }
                    }
                    list.Add(new SubFingerprint(index, flippedHash, true));
                }
            }
        }

        public static Profile[] GetProfiles() {
            return new Profile[] { new DefaultProfile(), new BugProfile(), new VoiceProfile(), new BassProfile(), new HumanProfile() };
        }
    }
}
