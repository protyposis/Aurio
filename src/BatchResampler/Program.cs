using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Aurio;
using Aurio.FFmpeg;
using Aurio.FFT;
using Aurio.Resampler;
using Aurio.Streams;

namespace BatchResampler
{
    class Program
    {
        private static void Main(string[] args)
        {
            // Use PFFFT as FFT implementation
            FFTFactory.Factory = new Aurio.PFFFT.FFTFactory();
            // Use Soxr as resampler implementation
            ResamplerFactory.Factory = new Aurio.Soxr.ResamplerFactory();
            // Use FFmpeg for file reading/decoding
            AudioStreamFactory.AddFactory(new FFmpegAudioStreamFactory());

            try
            {
                // read config file
                Dictionary<string, double> mapping = ReadConfig(args[0]);

                // read input dir
                DirectoryInfo indir = new DirectoryInfo(args[1]);

                // read output dir
                DirectoryInfo outdir = new DirectoryInfo(args[2]);
                if (!outdir.Exists)
                    outdir.Create();

                Process(mapping, indir, outdir);
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    "Batch Resampling Tool to change the playback speed of audio files"
                );
                Console.WriteLine();
                Console.WriteLine("Error: " + e.Message);
                Console.WriteLine();

                var info =
                    "Usage: BatchResampler mappingFile inputDirectory outputDirectory"
                    + Environment.NewLine
                    + Environment.NewLine
                    + "The mappingFile is a text file with at least one line of the pattern 'filenamePattern;resamplingFactor' (without quotes), "
                    + "where the filenamePattern can be a filename or a filename wildcard expression to be matched in the input directory, and the "
                    + "resamplingFactor is a floating point value specifying the input:output resampling ratio with which the files will be written"
                    + "to the output directory. The simplest mappingFile would be '*;1', which means that all files in the input directory will be"
                    + "resampled with a factor of 1.0 (i.e. no resampling at all) and written to the output directory. The nominal sampling rate of a file"
                    + "stays untouched, leading to altered playback speeds."
                    + Environment.NewLine
                    + "A mappingFile with the content '*.wav;0.5' speeds up all wave audio files to 200% playback speed because they are getting resampled "
                    + "to half of their effective sampling rate while the nominal playback sample rate stays the same, cutting their playback duration in half.";

                Console.WriteLine(info);
            }
        }

        static void Process(
            Dictionary<string, double> mapping,
            DirectoryInfo indir,
            DirectoryInfo outdir
        )
        {
            Dictionary<FileInfo, double> fileMapping = new Dictionary<FileInfo, double>();

            foreach (string fileNamePattern in mapping.Keys)
            {
                double factor = mapping[fileNamePattern];
                foreach (FileInfo fileInfo in indir.EnumerateFiles(fileNamePattern))
                {
                    fileMapping.Add(fileInfo, factor);
                }
            }

            Parallel.ForEach<FileInfo>(
                fileMapping.Keys,
                (fileInfo) =>
                {
                    double factor = fileMapping[fileInfo];
                    FileInfo outputFileInfo = new FileInfo(
                        Path.Combine(outdir.FullName, fileInfo.Name)
                    );

                    if (outputFileInfo.Exists)
                    {
                        Console.WriteLine(fileInfo.Name + " SKIP (file already existing)");
                        return;
                    }

                    Console.WriteLine(fileInfo.Name);
                    try
                    {
                        IAudioStream inputStream = AudioStreamFactory.FromFileInfoIeee32(fileInfo);
                        IAudioStream resamplingStream = new ResamplingStream(
                            inputStream,
                            ResamplingQuality.VeryHigh,
                            factor
                        );
                        MixerStream sampleRateResetStream = new MixerStream(
                            resamplingStream.Properties.Channels,
                            inputStream.Properties.SampleRate
                        );
                        sampleRateResetStream.Add(resamplingStream);

                        IAudioStream outputStream = sampleRateResetStream;

                        AudioStreamFactory.WriteToFile(outputStream, outputFileInfo.FullName);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error processing " + fileInfo.Name + ": " + e.Message);
                    }
                }
            );
        }

        /// <summary>
        /// Reads a file containing a mapping of regexes for filenames and their resampling speed change factor
        /// </summary>
        /// <param name="fi"></param>
        private static Dictionary<string, double> ReadConfig(string filename)
        {
            Dictionary<string, double> mapping = new Dictionary<string, double>();
            using (StreamReader reader = File.OpenText(filename))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(';');
                    mapping.Add(
                        parts[0],
                        Double.Parse(parts[1], CultureInfo.InvariantCulture.NumberFormat)
                    );
                }
            }

            return mapping;
        }
    }
}
