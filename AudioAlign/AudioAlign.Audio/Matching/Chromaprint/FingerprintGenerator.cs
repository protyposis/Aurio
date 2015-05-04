using AudioAlign.Audio.Project;
using AudioAlign.Audio.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Chromaprint {
    /// <summary>
    /// Chromaprint fingerprint generator as described in:
    /// - https://oxygene.sk/2010/07/introducing-chromaprint/
    /// - https://oxygene.sk/2011/01/how-does-chromaprint-work/
    /// - https://bitbucket.org/acoustid/chromaprint/
    /// </summary>
    public class FingerprintGenerator {

        private Profile profile;

        public FingerprintGenerator(Profile profile) {
            this.profile = profile;
        }

        public void Generate(AudioTrack track) {
            IAudioStream audioStream = new ResamplingStream(
                new MonoStream(AudioStreamFactory.FromFileInfoIeee32(track.FileInfo)),
                ResamplingQuality.Medium, profile.SamplingRate);
        }
    }
}
