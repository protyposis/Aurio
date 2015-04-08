using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using AudioAlign.Soxr;

namespace AudioAlign.Audio.Streams {
    public class ResamplingStream : AbstractAudioStreamWrapper {

        private ResamplingQuality quality;
        private SoxResampler soxr;
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

            this.quality = quality;
            SetupResampler();
            
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

        private void SetupResampler() {
            if (soxr != null) {
                soxr.Dispose(); // delete previous resampler instance
            }

            Soxr.QualityRecipe qr = QualityRecipe.SOXR_HQ;
            Soxr.QualityFlags qf = QualityFlags.SOXR_ROLLOFF_SMALL;

            switch (quality) {
                case ResamplingQuality.VeryLow:
                    qr = QualityRecipe.SOXR_QQ; break;
                case ResamplingQuality.Low:
                    qr = QualityRecipe.SOXR_LQ; break;
                case ResamplingQuality.Medium:
                    qr = QualityRecipe.SOXR_MQ; break;
                case ResamplingQuality.High:
                    qr = QualityRecipe.SOXR_HQ; break;
                case ResamplingQuality.VeryHigh:
                    qr = QualityRecipe.SOXR_VHQ; break;
                case ResamplingQuality.VariableRate:
                    qr = QualityRecipe.SOXR_HQ; qf = QualityFlags.SOXR_VR; break;
            }

            double inputRate = sourceStream.Properties.SampleRate;
            double outputRate = properties.SampleRate;

            if (qf == QualityFlags.SOXR_VR) {
                // set max variable rate
                inputRate = 10.0;
                outputRate = 1.0;
            }

            soxr = new SoxResampler(inputRate, outputRate, properties.Channels, qr, qf);
        }

        public double TargetSampleRate {
            get { return targetSampleRate; }
            set {
                targetSampleRate = value;
                sampleRateRatio = value / sourceStream.Properties.SampleRate;
                properties.SampleRate = (int)targetSampleRate;

                if (soxr.VariableRate) {
                    soxr.SetRatio(sampleRateRatio, 0);
                }
                else {
                    SetupResampler();
                }
            }
        }

        public double SampleRateRatio {
            get { return sampleRateRatio; }
            set { TargetSampleRate = sourceStream.Properties.SampleRate * value; }
        }

        public int BufferedBytes {
            get { return (int)soxr.GetOutputDelay(); }
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
                if (soxr.VariableRate) {
                    soxr.Clear(); // clear buffered data in soxr
                    soxr.SetRatio(sampleRateRatio, 0); // re-init soxr instance
                }
                else {
                    SetupResampler();
                }
                sourceBufferFillLevel = 0; // clear locally buffered data
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
                int bytesRead = sourceStream.Read(buffer, offset, count);
                position += bytesRead;
                return bytesRead;
            }

            int inputLengthUsed, outputLengthGenerated;
            bool endOfStream = false;

            // loop while the sample rate converter consumes samples until it produces an output
            do {
                if (sourceBufferFillLevel == 0 || sourceBufferPosition == sourceBufferFillLevel) {
                    // dynamically increase buffer size
                    // Here's a good place to resize the buffer because it is guaranteed to be empty / fully consumed
                    if (sourceBuffer.Length < count) {
                        int oldSize = sourceBuffer.Length;
                        sourceBuffer = new byte[count];
                        Debug.WriteLine("ResamplingStream: buffer size increased: " + oldSize + " -> " + count);
                    }

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
                soxr.Process(sourceBuffer, sourceBufferPosition, sourceBufferFillLevel - sourceBufferPosition,
                    buffer, offset, count, endOfStream, out inputLengthUsed, out outputLengthGenerated);
                sourceBufferPosition += inputLengthUsed;
            } 
            while (inputLengthUsed > 0 && outputLengthGenerated == 0);

            position += outputLengthGenerated;

            // in some cases it can happen that the SRC returns more data than the stream length suggests,
            // this data is cut off here to avoid a stream position that is greater than the stream's length.
            // NOTE max observed overflow: 8 bytes (1 2ch 32bit sample)
            long length = Length;
            if (length == 0) {
                // This is a special case in which the length suddenly drops to zero, which can happen when reading
                // from a dynamic source (e.g. a mixer). In this case no valid overflow can be calculated, so just
                // return the read bytes.
                return outputLengthGenerated;
            }
            else if (position > length) {
                int overflow = (int)(position - length);
                // The resampler is expected to return a few samples too much, and they can just be thrown away
                // http://comments.gmane.org/gmane.comp.audio.src.general/168
                Debug.WriteLine("ResamplingStream overflow: {0} bytes cut off", overflow);
                position -= overflow;
                return outputLengthGenerated - overflow;
            }
            else if (position < length && inputLengthUsed == 0 && outputLengthGenerated == 0 && endOfStream) {
                int underflow = (int)(length - position);
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

        public static bool CheckSampleRateRatio(double ratio) {
            return SoxResampler.CheckRatio(ratio);
        }
    }
}
