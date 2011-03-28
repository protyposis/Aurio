using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Project;
using AudioAlign.Audio.Streams;
using System.Collections.Specialized;
using System.Diagnostics;

namespace AudioAlign.Audio.Matching.HaitsmaKalker2002 {
    public class FingerprintGenerator {

        private const int SAMPLE_BYTE_SIZE = 4;
        private const int STREAM_INPUT_BUFFER_SIZE = 32768;
        private const int FRAME_SIZE = 2048; // 2048 samples per window
        private const int FRAME_STEP = 64; // take a window every 64 samples (WINDOW_SIZE / WINDOW_STEP = frame overlap)
        private const int SAMPLERATE = 5512;

        private const int FREQ_MIN = 300;
        private const int FREQ_MAX = 2000;
        private const int FREQ_BANDS = 33;

        private AudioTrack inputTrack;
        private WindowFunction windowFunction;
        private double[] frequencyBands;

        private int flipWeakestBits;

        public event EventHandler<SubFingerprintEventArgs> SubFingerprintCalculated;
        public event EventHandler Completed;

        public FingerprintGenerator(AudioTrack track)
            : this(track, 0) {
        }

        public FingerprintGenerator(AudioTrack track, int flipWeakestBits) {
            this.inputTrack = track;
            this.windowFunction = WindowUtil.GetFunction(WindowType.Hann, FRAME_SIZE);
            this.frequencyBands = FFTUtil.CalculateFrequencyBoundariesLog(FREQ_MIN, FREQ_MAX, FREQ_BANDS);
            this.flipWeakestBits = flipWeakestBits;
        }

        private TimeSpan timestamp = TimeSpan.Zero;
        public void Generate() {
            IAudioStream audioStream = new ResamplingStream(
                new MonoStream(AudioStreamFactory.FromFileInfoIeee32(inputTrack.FileInfo)),
                ResamplingQuality.SincFastest, SAMPLERATE);
            int sampleBytes = audioStream.Properties.SampleByteSize;
            byte[] streamBuffer = new byte[STREAM_INPUT_BUFFER_SIZE * sampleBytes];
            float[] frameBufferF = new float[FRAME_SIZE];
            int streamBufferOffsetF = 0;
            int streamBufferLevelF = 0;
            int frameOffsetF = 0;
            int index = 0;

            unsafe {
                fixed (byte* streamBufferB = streamBuffer) {
                    float* streamBufferF = (float*)streamBufferB;

                    while (audioStream.Position <= audioStream.Length) {
                        // fill the stream input buffer, if no bytes returned we have reached the end of the stream
                        int streamBufferOffsetB = streamBufferOffsetF * sampleBytes;
                        streamBufferLevelF = StreamUtil.ForceRead(audioStream, streamBuffer,
                            streamBufferOffsetB, streamBuffer.Length - streamBufferOffsetB) / sampleBytes;
                        if (streamBufferLevelF == 0) {
                            Debug.WriteLine("subfingerprint generation finished - end position {0}/{1}", audioStream.Position, audioStream.Length);

                            if (Completed != null) {
                                Completed(this, EventArgs.Empty);
                            }
                            
                            return; // whole stream has been processed
                        }
                        streamBufferLevelF += streamBufferOffsetF;
                        streamBufferOffsetF = 0;

                        // iterate through windows in current buffer
                        while (frameOffsetF + FRAME_SIZE <= streamBufferLevelF) {
                            // copy window to window buffer
                            for (int x = 0; x < FRAME_SIZE; x++) {
                                frameBufferF[x] = streamBufferF[x + frameOffsetF];
                            }

                            timestamp = SubFingerprintIndexToTimeSpan(index++);
                            ProcessFrame(frameBufferF);

                            frameOffsetF += FRAME_STEP;
                        }

                        // carry over unprocessed samples from the end of the stream buffer to its beginning
                        streamBufferOffsetF = streamBufferLevelF - frameOffsetF;
                        for (int x = 0; x < streamBufferOffsetF; x++) {
                            streamBufferF[x] = streamBufferF[frameOffsetF + x];
                        }
                        frameOffsetF = 0;
                    }
                }
            }
        }

        private float[] fftResult = new float[FRAME_SIZE / 2];
        float[] bands, bandsPrev;
        private void ProcessFrame(float[] frame) {
            if (frame.Length != FRAME_SIZE) {
                throw new Exception();
            }

            // apply window function
            windowFunction.Apply(frame);

            // do fourier transform
            FFTUtil.FFT(frame);

            // normalize fourier results
            // TODO check if calculation corrensponds to paper
            FFTUtil.NormalizeResults(frame, fftResult);

            // sum up the frequency bins
            // TODO check energy computation formula from paper
            // TODO index-mapping can be precomputed
            bands = new float[33];
            float bandWidth = SAMPLERATE / fftResult.Length;
            for (int x = 0; x < frequencyBands.Length - 1; x++) {
                int lowerIndex = (int)(frequencyBands[x] / bandWidth);
                int upperIndex = (int)(frequencyBands[x + 1] / bandWidth);
                for (int y = lowerIndex; y <= upperIndex; y++) {
                    bands[x] += fftResult[x];
                }
            }

            if (bandsPrev != null) {
                CalculateSubFingerprint(bandsPrev, bands);
            }

            bandsPrev = bands;
        }

        private void CalculateSubFingerprint(float[] energyBands, float[] previousEnergyBands) {
            SubFingerprint subFingerprint = new SubFingerprint();
            Dictionary<int, float> bitReliability = new Dictionary<int, float>();

            for (int m = 0; m < 32; m++) {
                float difference = energyBands[m] - energyBands[m + 1] - (previousEnergyBands[m] - previousEnergyBands[m + 1]);
                subFingerprint[m] = difference > 0;
                bitReliability.Add(m, difference);
            }

            if (SubFingerprintCalculated != null) {
                SubFingerprintCalculated(this, new SubFingerprintEventArgs(inputTrack, subFingerprint, timestamp));
            }

            if (flipWeakestBits > 0) {
                // calculate probable subfingerprints by flipping the most unreliable bits (the bits with the least energy differences)
                List<int> weakestBits = new List<int>(bitReliability.Keys.OrderByDescending(key => bitReliability[key]));
                for (int i = 0; i < flipWeakestBits; i++) {
                    SubFingerprint flippedSubFingerprint = new SubFingerprint(subFingerprint.Value);
                    flippedSubFingerprint[weakestBits[i]] = !flippedSubFingerprint[weakestBits[i]];
                    if (SubFingerprintCalculated != null) {
                        SubFingerprintCalculated(this, new SubFingerprintEventArgs(inputTrack, flippedSubFingerprint, timestamp));
                    }
                }
            }
        }

        public static TimeSpan SubFingerprintIndexToTimeSpan(int index) {
            return new TimeSpan((long)Math.Round((double)index * FRAME_STEP / SAMPLERATE * 1000 * 1000 * 10));
        }

        public static int TimeStampToSubFingerprintIndex(TimeSpan timeSpan) {
            return (int)Math.Round((double)timeSpan.Ticks / 10 / 1000 / 1000 * SAMPLERATE / FRAME_STEP);
        }
    }
}
