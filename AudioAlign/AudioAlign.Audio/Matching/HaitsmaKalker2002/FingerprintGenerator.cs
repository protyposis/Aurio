using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Project;
using AudioAlign.Audio.Streams;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AudioAlign.Audio.Matching.HaitsmaKalker2002 {
    public class FingerprintGenerator {

        private const int STREAM_INPUT_BUFFER_SIZE = 32768;

        private AudioTrack inputTrack;
        private IProfile profile;

        private int flipWeakestBits;
        private bool generateAllBitCombinations;

        public event EventHandler<SubFingerprintEventArgs> SubFingerprintCalculated;
        public event EventHandler Completed;

        public FingerprintGenerator(IProfile profile, AudioTrack track)
            : this(profile, track, 0, false) {
        }

        public FingerprintGenerator(IProfile profile, AudioTrack track, int flipWeakestBits, bool generateAllBitCombinations) {
            this.inputTrack = track;
            this.profile = profile;
            this.flipWeakestBits = flipWeakestBits;
            this.generateAllBitCombinations = generateAllBitCombinations;
        }

        private TimeSpan timestamp = TimeSpan.Zero;
        private float[] frameBuffer;

        public void Generate() {
            IAudioStream audioStream = new ResamplingStream(
                new MonoStream(AudioStreamFactory.FromFileInfoIeee32(inputTrack.FileInfo)),
                ResamplingQuality.SincFastest, profile.SampleRate);

            STFT stft = new STFT(audioStream, profile.FrameSize, profile.FrameStep, WindowType.Hann);
            int index = 0;

            frameBuffer = new float[profile.FrameSize / 2];

            while (stft.HasNext()) {
                stft.ReadFrame(frameBuffer);
                ProcessFrame(frameBuffer);
                timestamp = SubFingerprintIndexToTimeSpan(profile, index++);
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
            SubFingerprint subFingerprint = new SubFingerprint();
            Dictionary<int, float> bitReliability = new Dictionary<int, float>();

            for (int m = 0; m < 32; m++) {
                float difference = energyBands[m] - energyBands[m + 1] - (previousEnergyBands[m] - previousEnergyBands[m + 1]);
                subFingerprint[m] = difference > 0;
                bitReliability.Add(m, difference > 0 ? difference : -difference); // take absolute value as reliability weight
            }

            if (SubFingerprintCalculated != null) {
                SubFingerprintCalculated(this, new SubFingerprintEventArgs(inputTrack, subFingerprint, timestamp, false));
            }

            if (flipWeakestBits > 0) {
                // calculate probable subfingerprints by flipping the most unreliable bits (the bits with the least energy differences)
                List<int> weakestBits = new List<int>(bitReliability.Keys.OrderBy(key => bitReliability[key]));
                if (!generateAllBitCombinations) {
                    // generate fingerprints with one bit flipped
                    for (int i = 0; i < flipWeakestBits; i++) {
                        SubFingerprint flippedSubFingerprint = new SubFingerprint(subFingerprint.Value);
                        flippedSubFingerprint[weakestBits[i]] = !flippedSubFingerprint[weakestBits[i]];
                        if (SubFingerprintCalculated != null) {
                            SubFingerprintCalculated(this, new SubFingerprintEventArgs(inputTrack, flippedSubFingerprint, timestamp, true));
                        }
                    }
                }
                else {
                    // generate fingerprints with all possible bit combinations flipped
                    int variations = 1 << flipWeakestBits;
                    for (int i = 1; i < variations; i++) { // start at 1 since i0 equals to the original subfingerprint
                        SubFingerprint flippedSubFingerprint = new SubFingerprint(subFingerprint.Value);
                        for (int j = 0; j < flipWeakestBits; j++) {
                            if (((i >> j) & 1) == 1) {
                                flippedSubFingerprint[weakestBits[j]] = !flippedSubFingerprint[weakestBits[j]];
                            }
                        }
                        if (SubFingerprintCalculated != null) {
                            SubFingerprintCalculated(this, new SubFingerprintEventArgs(inputTrack, flippedSubFingerprint, timestamp, true));
                        }
                    }
                }
            }
        }

        public static TimeSpan SubFingerprintIndexToTimeSpan(IProfile profile, int index) {
            return new TimeSpan((long)Math.Round((double)index * profile.FrameStep / profile.SampleRate * 1000 * 1000 * 10));
        }

        public static int TimeStampToSubFingerprintIndex(IProfile profile, TimeSpan timeSpan) {
            return (int)Math.Round((double)timeSpan.Ticks / 10 / 1000 / 1000 * profile.SampleRate / profile.FrameStep);
        }

        public static IProfile[] GetProfiles() {
            return new IProfile[] { new DefaultProfile(), new BugProfile(), new VoiceProfile(), new BassProfile(), new HumanProfile() };
        }
    }
}
