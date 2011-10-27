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
        private long position;

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

            position = 0;
        }

        public ResamplingStream(IAudioStream sourceStream, ResamplingQuality quality, int outputSampleRate)
            : this(sourceStream, quality) {
                TargetSampleRate = outputSampleRate;
        }

        public ResamplingStream(IAudioStream sourceStream, ResamplingQuality quality, double sampleRateRatio)
            : this(sourceStream, quality) {
                SampleRateRatio = sampleRateRatio;
        }

        public double TargetSampleRate {
            get { return targetSampleRate; }
            set {
                targetSampleRate = value;
                sampleRateRatio = value / sourceStream.Properties.SampleRate;
                src.SetRatio(sampleRateRatio);
                properties.SampleRate = (int)targetSampleRate;
            }
        }

        public double SampleRateRatio {
            get { return sampleRateRatio; }
            set { TargetSampleRate = sourceStream.Properties.SampleRate * value; }
        }

        public int BufferedBytes {
            get { return src.BufferedBytes; }
        }

        public override AudioProperties Properties {
            get { return properties; }
        }

        public override long Length {
            get { 
                return StreamUtil.AlignToBlockSize(
                    (long)Math.Ceiling(sourceStream.Length * sampleRateRatio), SampleBlockSize);
            }
        }

        public override long Position {
            get { return position; }
            set {
                position = value;
                long pos = (long)Math.Ceiling(value / sampleRateRatio);
                pos -= pos % sourceStream.SampleBlockSize;
                sourceStream.Position = pos;
                src.Reset(); // clear buffered data in the SRC
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
            // TODO debug this block (it might give problems if the sampling rate of the stream is changed
            //      dynamically - there might be source stream position issues (because of the SRC prereading,
            //      and there might be local buffering issues in terms of the buffer containing unfitting samples)
            if (properties.SampleRate == sourceStream.Properties.SampleRate) {
                return sourceStream.Read(buffer, offset, count);
            }

            // dynamically increase buffer size
            if (sourceBuffer.Length < count) {
                int oldSize = sourceBuffer.Length;
                sourceBuffer = new byte[count];
                Debug.WriteLine("ResamplingStream: buffer size increased: " + oldSize + " -> " + count);
            }

            int inputLengthUsed, outputLengthGenerated;
            bool endOfStream = false;

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
                // this is also the reason why the source stream's Read() method may be called multiple times although
                // it already signalled that it has reached the end of the stream
                endOfStream = sourceStream.Position >= sourceStream.Length && sourceBufferFillLevel == 0;
                src.Process(sourceBuffer, sourceBufferPosition, sourceBufferFillLevel - sourceBufferPosition,
                    buffer, offset, count, endOfStream, out inputLengthUsed, out outputLengthGenerated);
                sourceBufferPosition += inputLengthUsed;
            } 
            while (inputLengthUsed > 0 && outputLengthGenerated == 0);

            position += outputLengthGenerated;

            // in some cases it can happen that the SRC returns more data than the stream length suggests,
            // this data is cut off here to avoid a stream position that is greater than the stream's length.
            // NOTE max observed overflow: 8 bytes (1 2ch 32bit sample)
            if (position > Length) {
                int overflow = (int)(position - Length);
                Debug.WriteLine("ResamplingStream OVERFLOW WARNING: {0} bytes cut off", overflow);
                position -= overflow;
                return outputLengthGenerated - overflow;
            }
            else if (position < Length && inputLengthUsed == 0 && outputLengthGenerated == 0 && endOfStream) {
                int underflow = (int)(Length - position);
                if (count < underflow) {
                    underflow = count;
                }
                Debug.WriteLine("ResamplingStream UNDERFLOW WARNING: {0} bytes added", underflow);
                position += underflow;
                Array.Clear(buffer, offset, underflow); // set bytes to zero
                return underflow;
            }

            return outputLengthGenerated;
        }

        public bool CheckTargetSampleRate(double sampleRate) {
            return CheckSampleRateRatio(sampleRate / sourceStream.Properties.SampleRate);
        }

        public bool CheckSampleRateRatio(double ratio) {
            return SampleRateConverter.CheckRatio(ratio);
        }
    }
}
