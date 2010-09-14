using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio {
    public class AudioProperties {

        internal AudioProperties(int channels, int sampleRate, int bitDepth) {
            if (channels < 1) {
                throw new Exception("invalid number of channels: " + channels);
            }
            if (sampleRate < 1) {
                throw new Exception("invalid sample rate: " + sampleRate);
            }
            if (bitDepth < 1 || bitDepth % 8 != 0) {
                throw new Exception("invalid bit depth: " + bitDepth);
            }

            Channels = channels;
            SampleRate = sampleRate;
            BitDepth = bitDepth;
        }

        public int Channels { get; private set; }
        public int SampleRate { get; private set; }
        public int BitDepth { get; private set; }
    }
}
