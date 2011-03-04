using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace AudioAlign.Audio.Streams {
    public class NAudioSinkStream : WaveStream {

        private IAudioStream sourceStream;

        private WaveFormat waveFormat;

        public NAudioSinkStream(IAudioStream sourceStream) {
            AudioProperties sourceProperties = sourceStream.Properties;

            if (sourceProperties.Format == AudioFormat.LPCM) {
                waveFormat = WaveFormat.CreateCustomFormat(
                    WaveFormatEncoding.Pcm, 
                    sourceProperties.SampleRate,
                    sourceProperties.Channels,
                    sourceProperties.SampleRate * sourceProperties.Channels * sourceProperties.SampleByteSize,
                    sourceProperties.Channels * sourceProperties.SampleByteSize, sourceProperties.BitDepth);
            }
            else if (sourceProperties.Format == AudioFormat.IEEE) {
                waveFormat = WaveFormat.CreateCustomFormat(
                    WaveFormatEncoding.IeeeFloat,
                    sourceProperties.SampleRate,
                    sourceProperties.Channels,
                    sourceProperties.SampleRate * sourceProperties.Channels * sourceProperties.SampleByteSize,
                    sourceProperties.Channels * sourceProperties.SampleByteSize, sourceProperties.BitDepth);
            }
            else {
                throw new ArgumentException("unsupported source format: " + sourceProperties.ToString());
            }

            this.sourceStream = sourceStream;
        }

        public override WaveFormat WaveFormat {
            get { return waveFormat; }
        }

        public override long Length {
            get { return sourceStream.Length; }
        }

        public override long Position {
            get { return sourceStream.Position; }
            set { sourceStream.Position = value; }
        }

        public override int Read(byte[] buffer, int offset, int count) {
            if (count % BlockAlign != 0) {
                throw new Exception("misaligned read length!");
            }
            return sourceStream.Read(buffer, offset, count);
        }
    }
}
