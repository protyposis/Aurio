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

            int samplesRead;
            while ((samplesRead = reader.ReadFrame()) > 0) {
                Console.WriteLine("read " + samplesRead);
            }

            reader.Dispose();

            //IeeeStream ieee = new IeeeStream(nAudioSource);
            //NAudioSinkStream nAudioSink = new NAudioSinkStream(ieee);
            //WaveFileWriter.CreateWaveFile(dlg.FileName + ".processed.wav", nAudioSink);

        }
    }
}
