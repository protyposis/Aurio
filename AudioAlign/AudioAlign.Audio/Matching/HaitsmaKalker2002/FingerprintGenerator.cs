using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Project;
using AudioAlign.Audio.Streams;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.InteropServices;
using AudioAlign.Audio.Features;

namespace AudioAlign.Audio.Matching.HaitsmaKalker2002 {
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

        public FingerprintGenerator(Profile profile, AudioTrack track)
            : this(profile, track, 0) {
        }

        public FingerprintGenerator(Profile profile, AudioTrack track, int flipWeakestBits) {
            this.inputTrack = track;
            this.profile = profile;
            this.flipWeakestBits = flipWeakestBits;
        }

        private int index;
        private int indices;
        private float[] frameBuffer;

        public void Generate() {
            IAudioStream audioStream = new ResamplingStream(
                new MonoStream(AudioStreamFactory.FromFileInfoIeee32(inputTrack.FileInfo)),
                ResamplingQuality.Medium, profile.SampleRate);

            STFT stft = new STFT(audioStream, profile.FrameSize, profile.FrameStep, WindowType.Hann);
            index = 0;
            indices = stft.WindowCount;

            frameBuffer = new float[profile.FrameSize / 2];

            while (stft.HasNext()) {
                stft.ReadFrame(frameBuffer);
                ProcessFrame(frameBuffer);
                index++;
            }

            if (Completed != null) {
                Completed(this, EventArgs.Empty);
            }
        }

        private float[] bands = new float[33];
        private float[] bandsPrev = new float[33];

        private void ProcessFrame(float[] fftResult) {
            if (fftResult.Length != profile.FrameSize / 2) {
                throw new Exception();
            }

            profile.MapFrequencies(fftResult, bands);

            CalculateSubFingerprint(bandsPrev, bands);

            CommonUtil.Swap<float[]>(ref bands, ref bandsPrev);
        }

        private void CalculateSubFingerprint(float[] energyBands, float[] previousEnergyBands) {
            SubFingerprintHash hash = new SubFingerprintHash();
            Dictionary<int, float> bitReliability = new Dictionary<int, float>();

            for (int m = 0; m < 32; m++) {
                float difference = energyBands[m] - energyBands[m + 1] - (previousEnergyBands[m] - previousEnergyBands[m + 1]);
                hash[m] = difference > 0;
                bitReliability.Add(m, difference > 0 ? difference : -difference); // take absolute value as reliability weight
            }

            if (SubFingerprintsGenerated != null) {
                SubFingerprintsGenerated(this, new SubFingerprintsGeneratedEventArgs(inputTrack, new SubFingerprint(index, hash, false), index, indices));
            }

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
                    if (SubFingerprintsGenerated != null) {
                        SubFingerprintsGenerated(this, new SubFingerprintsGeneratedEventArgs(inputTrack, new SubFingerprint(index, flippedHash, true), index, indices));
                    }
                }
            }
        }

        public static TimeSpan SubFingerprintIndexToTimeSpan(Profile profile, int index) {
            return new TimeSpan((long)Math.Round((double)index * profile.FrameStep / profile.SampleRate * 1000 * 1000 * 10));
        }

        public static int TimeStampToSubFingerprintIndex(Profile profile, TimeSpan timeSpan) {
            return (int)Math.Round((double)timeSpan.Ticks / 10 / 1000 / 1000 * profile.SampleRate / profile.FrameStep);
        }

        public static Profile[] GetProfiles() {
            return new Profile[] { new DefaultProfile(), new BugProfile(), new VoiceProfile(), new BassProfile(), new HumanProfile() };
        }
    }
}
