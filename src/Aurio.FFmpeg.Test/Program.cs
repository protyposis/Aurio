using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Aurio.Streams;
using NAudio.Wave;

namespace Aurio.FFmpeg.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("usage: Aurio.FFmpeg.Test [options] filename");
                Console.WriteLine("options:");
                Console.WriteLine("  -a   decode audio to wav file (default setting)");
                Console.WriteLine("  -v   decode video frames to jpg files");
                Console.WriteLine("  -vi  video frame decoding interval (default: 1000)");
                Console.WriteLine("no input file specified");
                return;
            }

            Type type = Type.Audio;
            int videoFrameInterval = 1000;
            int i = 0;

            for (; i < args.Length - 1; i++)
            {
                switch (args[i])
                {
                    case "-a":
                        type = Type.Audio;
                        break;
                    case "-v":
                        type = Type.Video;
                        break;
                    case "-vi":
                        videoFrameInterval = int.Parse(args[++i]);
                        break;
                }
            }

            string filename = args[i]; // last argument

            if (type == Type.Audio)
            {
                DecodeAudio(filename);
            }
            else if (type == Type.Video)
            {
                DecodeVideo(filename, videoFrameInterval);
            }
        }

        private static void DecodeAudio(string filename)
        {
            FFmpegReader reader = new(filename, Type.Audio);

            Console.WriteLine(
                "length {0}, frame_size {1}, sample_rate {2}, sample_size {3}, channels {4}",
                reader.AudioOutputConfig.length,
                reader.AudioOutputConfig.frame_size,
                reader.AudioOutputConfig.format.sample_rate,
                reader.AudioOutputConfig.format.sample_size,
                reader.AudioOutputConfig.format.channels
            );

            int sampleBlockSize =
                reader.AudioOutputConfig.format.channels
                * reader.AudioOutputConfig.format.sample_size;

            int output_buffer_size =
                reader.AudioOutputConfig.frame_size
                * reader.AudioOutputConfig.format.channels
                * reader.AudioOutputConfig.format.sample_size;
            byte[] output_buffer = new byte[output_buffer_size];

            int samplesRead;
            long timestamp;
            MemoryStream ms = new();

            // read full stream
            while (
                (
                    samplesRead = reader.ReadFrame(
                        out timestamp,
                        output_buffer,
                        output_buffer_size,
                        out _
                    )
                ) > 0
            )
            {
                Console.WriteLine("read " + samplesRead + " @ " + timestamp);

                // read samples into memory
                int bytesRead = samplesRead * sampleBlockSize;
                ms.Write(output_buffer, 0, bytesRead);
            }

            // seek back to start
            reader.Seek(0, Type.Audio);

            // read again (output should be the same as above)
            while (
                (
                    samplesRead = reader.ReadFrame(
                        out timestamp,
                        output_buffer,
                        output_buffer_size,
                        out _
                    )
                ) > 0
            )
            {
                Console.WriteLine("read " + samplesRead + " @ " + timestamp);
            }

            reader.Dispose();

            // write memory to wav file
            ms.Position = 0;
            MemorySourceStream mss =
                new(
                    ms,
                    new AudioProperties(
                        reader.AudioOutputConfig.format.channels,
                        reader.AudioOutputConfig.format.sample_rate,
                        reader.AudioOutputConfig.format.sample_size * 8,
                        reader.AudioOutputConfig.format.sample_size == 4
                            ? AudioFormat.IEEE
                            : AudioFormat.LPCM
                    )
                );
            IeeeStream ieee = new(mss);
            NAudioSinkStream nAudioSink = new(ieee);
            WaveFileWriter.CreateWaveFile(filename + ".ffmpeg.wav", nAudioSink);
        }

        private static void DecodeVideo(string filename, int videoFrameInterval)
        {
            FFmpegReader reader = new(filename, Type.Video);

            Console.WriteLine(
                "length {0}, frame_size {1}x{2}, frame_rate {3}, aspect_ratio {4}",
                reader.VideoOutputConfig.length,
                reader.VideoOutputConfig.format.width,
                reader.VideoOutputConfig.format.height,
                reader.VideoOutputConfig.format.frame_rate,
                reader.VideoOutputConfig.format.aspect_ratio
            );

            int output_buffer_size =
                reader.VideoOutputConfig.format.width
                * reader.VideoOutputConfig.format.height
                * 3 /* RGB */
            ;
            byte[] output_buffer = new byte[output_buffer_size];
            int frameCount = 0;

            Bitmap rgbFrame =
                new(reader.VideoOutputConfig.format.width, reader.VideoOutputConfig.format.height);

            // read full stream
            while (
                reader.ReadFrame(out long timestamp, output_buffer, output_buffer_size, out _) > 0
            )
            {
                Console.WriteLine("read frame " + frameCount + " @ " + timestamp);

                if (frameCount % videoFrameInterval == 0)
                {
                    BitmapData rgbFrameData = rgbFrame.LockBits(
                        new Rectangle(0, 0, rgbFrame.Width, rgbFrame.Height),
                        ImageLockMode.WriteOnly,
                        PixelFormat.Format24bppRgb
                    );
                    Marshal.Copy(output_buffer, 0, rgbFrameData.Scan0, output_buffer_size);
                    rgbFrame.UnlockBits(rgbFrameData);

                    rgbFrame.Save(
                        String.Format("{0}.{1:00000000}.png", filename, frameCount),
                        ImageFormat.Png
                    );
                }

                frameCount++;
            }

            reader.Dispose();
        }
    }
}
