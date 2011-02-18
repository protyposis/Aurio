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
            set { targetSampleRate = value; src.SetRatio(value / sourceStream.Properties.SampleRate); }
        }

        public override AudioProperties Properties {
            get {
                properties.SampleRate = (int)TargetSampleRate;
                return properties; 
            }
        }

        public override int Read(byte[] buffer, int offset, int count) {
            // dynamically increase buffer size
            if (sourceBuffer.Length < count) {
                int oldSize = sourceBuffer.Length;
                sourceBuffer = new byte[count];
                Debug.WriteLine("ResamplingStream: buffer size increased: " + oldSize + " -> " + count);
            }

            int inputLengthUsed, outputLengthGenerated;

            do {
                if (sourceBufferFillLevel == 0 || sourceBufferPosition == sourceBufferFillLevel) {
                    // buffer is empty or all data has already been read -> refill
                    sourceBufferPosition = 0;
                    sourceBufferFillLevel = sourceStream.Read(sourceBuffer, 0, count);
                    //Debug.WriteLine("source read {0}", sourceBufferFillLevel);
                }
                src.Process(sourceBuffer, sourceBufferPosition, sourceBufferFillLevel - sourceBufferPosition, 
                    buffer, offset, count, false, out inputLengthUsed, out outputLengthGenerated);
                //Debug.WriteLine("source available {0} used {1} / target requested {2} generated {3}", 
                //    sourceBufferFillLevel - sourceBufferPosition, inputLengthUsed, count, outputLengthGenerated);
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
