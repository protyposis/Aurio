using System;
using System.Threading;
using System.Threading.Tasks;
using Aurio.FFT;
using Aurio.Matching.HaitsmaKalker2002;
using Aurio.Project;
using Aurio.Resampler;
using Aurio.Streams;

namespace Aurio.Test.RealtimeFingerprinting
{
    class Program
    {
        private static long _dataGenerated = 0;

        static void Main(string[] args)
        {
            var audioProperties = new AudioProperties(1, 44100, 32, AudioFormat.IEEE);

            // Aurio resampling & FFT initialization
            ResamplerFactory.Factory = new Soxr.ResamplerFactory();
            FFTFactory.Factory = new PFFFT.FFTFactory();

            // Create a 10-second FIFO buffer
            var fifoStream = new BlockingFixedLengthFifoStream(
                audioProperties,
                audioProperties.SampleBlockByteSize * audioProperties.SampleRate * 10
            );

            // Create fingerprinter
            // Emit subfingerprints every 20 generated
            // Keep the buffer to its minimum value (the subfingerprint frame size) to keep the processing latency as low as possible
            var audioTrack = new AudioTrack(fifoStream, "realtime fifo stream");
            var fingerprintingProfile = FingerprintGenerator.GetProfiles()[0];
            fingerprintingProfile.FlipWeakestBits = 0; // Don't generate flipped hashes for simplicities's sake (we're not working with the result anyway)
            var fingerprinter = new FingerprintGenerator(
                fingerprintingProfile,
                audioTrack,
                20,
                fingerprintingProfile.FrameSize
            );

            long subfingerprintsGenerated = 0;
            fingerprinter.SubFingerprintsGenerated += (
                object sender,
                Matching.SubFingerprintsGeneratedEventArgs e
            ) =>
            {
                subfingerprintsGenerated += e.SubFingerprints.Count;

                // Calculate some stats
                var ingressed = TimeUtil.BytesToTimeSpan(_dataGenerated, audioProperties);
                var buffered = TimeUtil.BytesToTimeSpan(
                    fifoStream.WritePosition - fifoStream.Position,
                    audioProperties
                );
                var processed = new TimeSpan(
                    (long)
                        Math.Round(
                            subfingerprintsGenerated
                                * fingerprintingProfile.HashTimeScale
                                * TimeUtil.SECS_TO_TICKS
                        )
                );

                // print the stats
                Console.WriteLine(
                    "{0} ingressed, {1} buffered, {2} processed, {3} subfingerprints generated",
                    ingressed,
                    buffered,
                    processed,
                    subfingerprintsGenerated
                );
            };

            // Start stream input
            StartSineWaveRealtimeGenerator(audioProperties, fifoStream);

            // Start output processing
            fingerprinter.Generate();
        }

        /// <summary>
        /// Simulates a realtime audio stream by generating a sine wave in realtime and ingesting it into the FIFO buffer.
        /// </summary>
        private static void StartSineWaveRealtimeGenerator(
            AudioProperties audioProperties,
            IAudioWriterStream targetStream
        )
        {
            // Create a stream that generates 1 second of a sine wave
            var sineWaveStream = new SineGeneratorStream(
                audioProperties.SampleRate,
                440,
                new TimeSpan(0, 0, 1)
            );

            // Store the sine wave in a buffer
            // We can concatenate this buffer over and over again to create an infinitely long sine wave
            var sineWaveBuffer = new byte[sineWaveStream.Length];
            var bytesRead = sineWaveStream.Read(sineWaveBuffer, 0, sineWaveBuffer.Length);
            if (bytesRead < sineWaveBuffer.Length)
            {
                throw new Exception("incomplete buffer read");
            }

            Task.Factory.StartNew(() =>
            {
                // Each realtime second, write the 1-second sine wave to the target stream to
                // simulate an infinitely long realtime sine wave stream.
                //
                // For low-latency processing use-cases, writes would ideally be shorter and happen
                // more frequently to keep the delay between input and output of the FIFO stream
                // as low as possible.
                while (true)
                {
                    Thread.Sleep(1000);
                    Console.WriteLine("Writing 1 second into buffer");
                    targetStream.Write(sineWaveBuffer, 0, sineWaveBuffer.Length);
                    _dataGenerated += sineWaveBuffer.Length;
                }
            });
        }
    }
}
