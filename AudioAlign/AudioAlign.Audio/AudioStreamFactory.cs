using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using AudioAlign.Audio.Project;
using AudioAlign.Audio.TaskMonitor;

namespace AudioAlign.Audio {
    public static class AudioStreamFactory {
        public const string PEAKFILE_EXTENSION = ".aapeaks";

        public static IAudioStream16 FromFilename(string filename) {
            return new NAudio16BitWaveFileReaderWrapperStream(new WaveFileReader(filename));
        }

        public static IAudioStream16 FromFileInfo(FileInfo fileInfo) {
            return new NAudio16BitWaveFileReaderWrapperStream(new WaveFileReader(fileInfo.FullName));
        }

        public static IAudioStream16 FromStream(Stream stream) {
            return new NAudio16BitWaveFileReaderWrapperStream(new WaveFileReader(stream));
        }

        public static VisualizingAudioStream16 FromAudioTrackForGUI(AudioTrack audioTrack) {
            int SAMPLES_PER_PEAK = 1024;

            IAudioStream16 audioInputStream = audioTrack.CreateAudioStream();

            PeakStore peakStore = new PeakStore(audioInputStream.Properties.Channels,
                (int)Math.Ceiling((float)audioInputStream.SampleCount / SAMPLES_PER_PEAK));
            
            // search for existing peakfile
            if (audioTrack.HasPeakFile) {
                // load peakfile from disk
                peakStore.ReadFrom(File.OpenRead(audioTrack.PeakFile.FullName));
            }
                // generate peakfile
            else {
                int channels = peakStore.Channels;
                int bufferSize = 65536;
                float[][] buffer = AudioUtil.CreateArray<float>(channels, bufferSize);
                List<float>[] minMax = AudioUtil.CreateList<float>(channels, SAMPLES_PER_PEAK);
                IAudioStream16 audioInputStream2 = audioTrack.CreateAudioStream();
                BinaryWriter[] peakWriters = peakStore.CreateMemoryStreams().WrapWithBinaryWriters();

                Task.Factory.StartNew(() => {
                    ProgressReporter progress = ProgressMonitor.Instance.BeginTask("Generating peaks for " + audioTrack.FileInfo.Name, true);
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
                                    // add last sample of previous peak as first sample of current peak to make consecutive peaks overlap
                                    // this gives the impression of a continuous waveform
                                    minMax[channel].Add(buffer[channel][x]);
                                }
                                sampleCount = 0;
                            }
                        }

                        progress.ReportProgress(100.0f / audioInputStream2.SampleCount * audioInputStream2.SamplePosition);
                    }
                    Debug.WriteLine("peak generation finished - " + (DateTime.Now - startTime) + ", " + (peakWriters[0].BaseStream.Length * channels) + " bytes");
                    ProgressMonitor.Instance.EndTask(progress);

                    // write peakfile to disk
                    FileStream peakOutputFile = File.OpenWrite(audioTrack.PeakFile.FullName);
                    peakStore.StoreTo(peakOutputFile);
                    peakOutputFile.Close();
                });
            }

            return new VisualizingAudioStream16(audioInputStream, peakStore);
        }

        public static VisualizingAudioStream16 FromFilenameForGUI(string fileName) {
            return FromAudioTrackForGUI(new AudioTrack(new FileInfo(fileName)));
        }
    }
}
