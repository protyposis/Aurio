using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio.Streams {
    public class AudioProperties {

        public AudioProperties(int channels, int sampleRate, int bitDepth, AudioFormat format) {
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
            Format = format;
        }

        public int Channels { get; internal set; }
        public int SampleRate { get; internal set; }
        public int BitDepth { get; internal set; }
        public AudioFormat Format { get; internal set; }

        public int SampleByteSize {
            get { return BitDepth / 8; }
        }

        public int SampleBlockByteSize {
            get { return SampleByteSize * Channels; }
        }

        public override string ToString() {
            return String.Format("{0}bit {1}Hz {2}ch {3}", BitDepth, SampleRate, Channels, Format);
        }
    }
}
