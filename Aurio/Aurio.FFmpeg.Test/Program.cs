using Aurio.Streams;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Aurio.FFmpeg.Test {
    class Program {
        static void Main(string[] args) {
            if (args.Length == 0) {
                Console.WriteLine("no input file specified");
                return;
            }

            // TODO read audio from FFmpeg
            FFmpegReader reader = new FFmpegReader(args[0], Type.Audio);

            Console.WriteLine("length {0}, frame_size {1}, sample_rate {2}, sample_size {3}, channels {4}", 
                reader.AudioOutputConfig.length,
                reader.AudioOutputConfig.frame_size,
                reader.AudioOutputConfig.format.sample_rate,
                reader.AudioOutputConfig.format.sample_size,
                reader.AudioOutputConfig.format.channels);

            int sampleBlockSize = reader.AudioOutputConfig.format.channels * reader.AudioOutputConfig.format.sample_size;

            int output_buffer_size = reader.AudioOutputConfig.frame_size * 
                reader.AudioOutputConfig.format.channels * reader.AudioOutputConfig.format.sample_size;
            byte[] output_buffer = new byte[output_buffer_size];

            int samplesRead;
            long timestamp;
            Type type;
            MemoryStream ms = new MemoryStream();

            // read full stream
            while ((samplesRead = reader.ReadFrame(out timestamp, output_buffer, output_buffer_size, out type)) > 0) {
                Console.WriteLine("read " + samplesRead + " @ " + timestamp);

                // read samples into memory
                int bytesRead = samplesRead * sampleBlockSize;
                ms.Write(output_buffer, 0, bytesRead);
            }

            // seek back to start
            reader.Seek(0, Type.Audio);

            // read again (output should be the same as above)
            while ((samplesRead = reader.ReadFrame(out timestamp, output_buffer, output_buffer_size, out type)) > 0) {
                Console.WriteLine("read " + samplesRead + " @ " + timestamp);
            }

            reader.Dispose();

            // write memory to wav file
            ms.Position = 0;
            MemorySourceStream mss = new MemorySourceStream(ms, new AudioProperties(
                reader.AudioOutputConfig.format.channels, 
                reader.AudioOutputConfig.format.sample_rate, 
                reader.AudioOutputConfig.format.sample_size * 8, 
                reader.AudioOutputConfig.format.sample_size == 4 ? AudioFormat.IEEE : AudioFormat.LPCM));
            IeeeStream ieee = new IeeeStream(mss);
            NAudioSinkStream nAudioSink = new NAudioSinkStream(ieee);
            WaveFileWriter.CreateWaveFile(args[0] + ".ffmpeg.wav", nAudioSink);

        }
    }
}
