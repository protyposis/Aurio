using AudioAlign.Audio.Streams;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AudioAlign.FFmpeg.Test {
    class Program {
        static void Main(string[] args) {
            if (args.Length == 0) {
                Console.WriteLine("no input file specified");
                return;
            }

            // TODO read audio from FFmpeg
            FFmpegReader reader = new FFmpegReader(args[0]);

            Console.WriteLine("length {0}, frame_size {1}, sample_rate {2}, sample_size {3}, channels {4}", 
                reader.OutputConfig.length,
                reader.OutputConfig.frame_size,
                reader.OutputConfig.format.sample_rate,
                reader.OutputConfig.format.sample_size,
                reader.OutputConfig.format.channels);

            int samplesRead;
            long timestamp;

            // read full stream
            while ((samplesRead = reader.ReadFrame(out timestamp)) > 0) {
                Console.WriteLine("read " + samplesRead + " @ " + timestamp);
            }

            // seek back to start
            reader.Seek(0);

            // read again (output should be the same as above)
            while ((samplesRead = reader.ReadFrame(out timestamp)) > 0) {
                Console.WriteLine("read " + samplesRead + " @ " + timestamp);
            }

            reader.Dispose();

            //IeeeStream ieee = new IeeeStream(nAudioSource);
            //NAudioSinkStream nAudioSink = new NAudioSinkStream(ieee);
            //WaveFileWriter.CreateWaveFile(dlg.FileName + ".processed.wav", nAudioSink);

        }
    }
}
