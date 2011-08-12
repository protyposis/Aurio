using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.LibSampleRate;
using System.Diagnostics;

namespace AudioAlign.Audio.Streams {
    public class ResamplingStream : AbstractAudioStreamWrapper {

        private SampleRateConverter src;
        private AudioProperties properties;
        private byte[] sourceBuffer;
        private int sourceBufferPosition;
        private int sourceBufferFillLevel;
        private double targetSampleRate;
        private double sampleRateRatio;

        public ResamplingStream(IAudioStream sourceStream, ResamplingQuality quality)
            : base(sourceStream) {
            if (!(sourceStream.Properties.Format == AudioFormat.IEEE && sourceStream.Properties.BitDepth == 32)) {
                throw new ArgumentException("unsupported source format: " + sourceStream.Properties);
            }

            properties = new AudioProperties(sourceStream.Properties.Channels, sourceStream.Properties.SampleRate, 
                sourceStream.Properties.BitDepth, sourceStream.Properties.Format);

            src = new SampleRateConverter((ConverterType)quality, properties.Channels);
            sourceBuffer = new byte[0];
            sourceBufferFillLevel = 0;
            sourceBufferPosition = 0;

            TargetSampleRate = properties.SampleRate;
        }

        public ResamplingStream(IAudioStream sourceStream, ResamplingQuality quality, int outputSampleRate)
            : this(sourceStream, quality) {
                TargetSampleRate = outputSampleRate;
        }

        public double TargetSampleRate {
            get { return targetSampleRate; }
            set {
                targetSampleRate = value;
                sampleRateRatio = value / sourceStream.Properties.SampleRate;
                src.SetRatio(sampleRateRatio);
            }
        }

        public override AudioProperties Properties {
            get {
                properties.SampleRate = (int)TargetSampleRate;
                return properties; 
            }
        }

        public override long Length {
            get { return (long)Math.Ceiling(sourceStream.Length * sampleRateRatio); }
        }

        public override long Position {
            get { 
                long pos = (long)Math.Ceiling(sourceStream.Position * sampleRateRatio);
                pos -= pos % SampleBlockSize;
                return pos;
            }
            set { 
                long pos = (long)Math.Ceiling(value / sampleRateRatio);
                pos -= pos % sourceStream.SampleBlockSize;
                sourceStream.Position = pos;
            }
        }

        /// <summary>
        /// Reads resampled data from the stream.
        /// </summary>
        /// <remarks>
        /// this function has been extensively analyzed and should not contain any bugs
        /// </remarks>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count) {
            // dynamically increase buffer size
            if (sourceBuffer.Length < count) {
                int oldSize = sourceBuffer.Length;
                sourceBuffer = new byte[count];
                Debug.WriteLine("ResamplingStream: buffer size increased: " + oldSize + " -> " + count);
            }

            int inputLengthUsed, outputLengthGenerated;

            // loop while the sample rate converter consumes samples until it produces an output
            do {
                if (sourceBufferFillLevel == 0 || sourceBufferPosition == sourceBufferFillLevel) {
                    // buffer is empty or all data has already been read -> refill
                    sourceBufferPosition = 0;
                    sourceBufferFillLevel = sourceStream.Read(sourceBuffer, 0, count);
                }
                // if the sourceBufferFillLevel is 0 at this position, the end of the source stream has been reached,
                // and endOfInput needs to be set to true in order to retrieve eventually buffered samples from
                // the sample rate converter
                src.Process(sourceBuffer, sourceBufferPosition, sourceBufferFillLevel - sourceBufferPosition,
                    buffer, offset, count, sourceBufferFillLevel == 0, out inputLengthUsed, out outputLengthGenerated);
                sourceBufferPosition += inputLengthUsed;
            } 
            while (inputLengthUsed > 0 && outputLengthGenerated == 0);

            return outputLengthGenerated;
        }

        public bool CheckTargetSampleRate(double sampleRate) {
            return src.CheckRatio(sampleRate / sourceStream.Properties.SampleRate);
        }
    }
}
