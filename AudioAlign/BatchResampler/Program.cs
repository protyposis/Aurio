using AudioAlign.Audio;
using AudioAlign.Audio.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchResampler
{
    class Program
    {
        static void Main(string[] args)
        {
            // read config file
            Dictionary<string, double> mapping = ReadConfig(args[0]);

            // read input dir
            DirectoryInfo indir = new DirectoryInfo(args[1]);

            // read output dir
            DirectoryInfo outdir = new DirectoryInfo(args[2]);
            if (!outdir.Exists)
                outdir.Create();

            Dictionary<FileInfo, double> fileMapping = new Dictionary<FileInfo, double>();

            foreach (string fileNamePattern in mapping.Keys)
            {
                double factor = mapping[fileNamePattern];
                foreach (FileInfo fileInfo in indir.EnumerateFiles(fileNamePattern))
                {
                    fileMapping.Add(fileInfo, factor);
                }
            }

            Parallel.ForEach<FileInfo>(fileMapping.Keys, (fileInfo) => {
                double factor = fileMapping[fileInfo];
                FileInfo outputFileInfo = new FileInfo(Path.Combine(outdir.FullName, fileInfo.Name));

                if (outputFileInfo.Exists)
                {
                    Console.WriteLine(fileInfo.Name + " SKIP");
                    return;
                }

                Console.WriteLine(fileInfo.Name);
                IAudioStream inputStream = AudioStreamFactory.FromFileInfoIeee32(fileInfo);
                IAudioStream resamplingStream = new ResamplingStream(inputStream, ResamplingQuality.SincBest, factor);
                MixerStream sampleRateResetStream = new MixerStream(resamplingStream.Properties.Channels, inputStream.Properties.SampleRate);
                sampleRateResetStream.Add(resamplingStream);

                IAudioStream outputStream = sampleRateResetStream;

                AudioStreamFactory.WriteToFile(outputStream, outputFileInfo.FullName);
            });
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
                while((line = reader.ReadLine()) != null) {
                    string[] parts = line.Split(';');
                    mapping.Add(parts[0], Double.Parse(parts[1]));
                }
            }

            return mapping;
        }
    }
}
