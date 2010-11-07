using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

namespace AudioAlign.Audio {
    public static class AudioStreamFactory {
        public static IAudioStream16 FromFilename(string filename) {
            return new NAudio16BitWaveFileReaderWrapperStream(new WaveFileReader(filename));
        }

        public static VisualizingAudioStream16 FromFilenameForGUI(string filename) {
            int SAMPLES_PER_PEAK = 1024;
            string PEAKFILE_EXTENSION = ".aapeaks";

            NAudio16BitWaveFileReaderWrapperStream audioInputStream = new NAudio16BitWaveFileReaderWrapperStream(
                new WaveFileReader(File.OpenRead(filename)));

            PeakStore peakStore = new PeakStore(audioInputStream.Properties.Channels,
                (int)Math.Ceiling((float)audioInputStream.SampleCount / audioInputStream.Properties.Channels / SAMPLES_PER_PEAK * 2));
            
            // search for existing peakfile
            if (File.Exists(filename + PEAKFILE_EXTENSION)) {
                // load peakfile from disk
                peakStore.ReadFrom(File.OpenRead(filename + PEAKFILE_EXTENSION));
            }
                // generate peakfile
            else {
                int channels = peakStore.Channels;
                int bufferSize = 512;
                float[][] buffer = AudioUtil.CreateArray<float>(channels, bufferSize);
                List<float>[] minMax = AudioUtil.CreateList<float>(channels, SAMPLES_PER_PEAK);
                IAudioStream16 audioInputStream2 = new NAudio16BitWaveFileReaderWrapperStream(
                    new WaveFileReader(File.OpenRead(filename)));
                BinaryWriter[] peakWriters = peakStore.CreateMemoryStreams().WrapWithBinaryWriters();

                Task.Factory.StartNew(() => {
                    DateTime startTime = DateTime.Now;
                    int sampleCount = 0;
                    int peakCount = 0;
                    int samplesRead;
                    long totalSamplesRead = 0;
                    while ((samplesRead = audioInputStream2.Read(buffer, bufferSize)) > 0) {
                        for (int x = 0; x < samplesRead; x++) {
                            totalSamplesRead++;
                            for (int channel = 0; channel < channels; channel++) {
                                minMax[channel].Add(buffer[channel][x]);
                            }
                            if (++sampleCount == SAMPLES_PER_PEAK || totalSamplesRead == audioInputStream2.SampleCount) {
                                // write peak
                                peakCount++;
                                for (int channel = 0; channel < channels; channel++) {
                                    peakWriters[channel].Write(new Peak(minMax[channel].Min(), minMax[channel].Max()));
                                    minMax[channel].Clear();
                                }
                                sampleCount = 0;
                            }
                        }

                        Debug.WriteLine((100.0f / audioInputStream2.SampleCount * audioInputStream2.SamplePosition) + "% of peaks generated...");
                    }
                    Debug.WriteLine("peak generation finished - " + (DateTime.Now - startTime) + ", " + (peakWriters[0].BaseStream.Length * channels) + " bytes");

                    // write peakfile to disk
                    FileStream peakOutputFile = File.OpenWrite(filename + PEAKFILE_EXTENSION);
                    peakStore.StoreTo(peakOutputFile);
                    peakOutputFile.Close();
                });
            }

            return new VisualizingAudioStream16(audioInputStream, peakStore);
        }
    }
}
