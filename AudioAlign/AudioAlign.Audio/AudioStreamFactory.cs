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

        public static AudioAlign.Audio.Streams.IAudioStream FromFileInfoIeee32(FileInfo fileInfo) {
            return new AudioAlign.Audio.Streams.IeeeStream(new AudioAlign.Audio.Streams.NAudioSourceStream(
                new WaveFileReader(fileInfo.FullName)));
        }

        public static IAudioStream16 FromFileInfo(FileInfo fileInfo) {
            return new NAudio16BitWaveFileReaderWrapperStream(new WaveFileReader(fileInfo.FullName));
        }

        public static VisualizingAudioStream16 FromAudioTrackForGUI(AudioTrack audioTrack) {
            int SAMPLES_PER_PEAK = 1024;

            AudioAlign.Audio.Streams.IAudioStream audioInputStream = FromFileInfoIeee32(audioTrack.FileInfo);

            PeakStore peakStore = new PeakStore(audioInputStream.Properties.Channels,
                (int)Math.Ceiling((float)audioInputStream.Length / audioInputStream.SampleBlockSize / SAMPLES_PER_PEAK));
            
            // search for existing peakfile
            if (audioTrack.HasPeakFile) {
                // load peakfile from disk
                peakStore.ReadFrom(File.OpenRead(audioTrack.PeakFile.FullName));
            }
                // generate peakfile
            else {
                int channels = peakStore.Channels;
                int bufferSize = 65536;
                //float[][] buffer = AudioUtil.CreateArray<float>(channels, bufferSize);
                byte[] buffer = new byte[bufferSize * audioInputStream.SampleBlockSize];
                List<float>[] minMax = AudioUtil.CreateList<float>(channels, SAMPLES_PER_PEAK);
                BinaryWriter[] peakWriters = peakStore.CreateMemoryStreams().WrapWithBinaryWriters();

                Task.Factory.StartNew(() => {
                    ProgressReporter progress = ProgressMonitor.Instance.BeginTask("Generating peaks for " + audioTrack.FileInfo.Name, true);
                    DateTime startTime = DateTime.Now;
                    int sampleBlockCount = 0;
                    int peakCount = 0;
                    int bytesRead;
                    long totalSampleBlocks = audioInputStream.Length / audioInputStream.SampleBlockSize;
                    long totalSamplesRead = 0;

                    unsafe {
                        fixed (byte* bufferB = &buffer[0]) {
                            float* bufferF = (float*)bufferB;
                            int samplesRead;
                            int samplesProcessed;

                            while ((bytesRead = StreamUtil.ForceRead(audioInputStream, buffer, 0, buffer.Length)) > 0) {
                                samplesRead = bytesRead / audioInputStream.Properties.SampleByteSize;
                                samplesProcessed = 0;

                                do {
                                    for (int channel = 0; channel < channels; channel++) {
                                        minMax[channel].Add(bufferF[samplesProcessed]);
                                        samplesProcessed++;
                                        totalSamplesRead++;
                                    }

                                    if (++sampleBlockCount == SAMPLES_PER_PEAK || sampleBlockCount == totalSampleBlocks) {
                                        // write peak
                                        peakCount++;
                                        for (int channel = 0; channel < channels; channel++) {
                                            peakWriters[channel].Write(new Peak(minMax[channel].Min(), minMax[channel].Max()));
                                            float last = minMax[channel].Last();
                                            minMax[channel].Clear();
                                            // add last sample of previous peak as first sample of current peak to make consecutive peaks overlap
                                            // this gives the impression of a continuous waveform
                                            minMax[channel].Add(last);
                                        }
                                        sampleBlockCount = 0;
                                    }
                                }
                                while (samplesProcessed < samplesRead);

                                progress.ReportProgress(100.0f / audioInputStream.Length * audioInputStream.Position);
                            }
                        }
                    }

                    
                    Debug.WriteLine("peak generation finished - " + (DateTime.Now - startTime) + ", " + (peakWriters[0].BaseStream.Length * channels) + " bytes");
                    ProgressMonitor.Instance.EndTask(progress);

                    // write peakfile to disk
                    FileStream peakOutputFile = File.OpenWrite(audioTrack.PeakFile.FullName);
                    peakStore.StoreTo(peakOutputFile);
                    peakOutputFile.Close();
                });
            }

            return new VisualizingAudioStream16(audioTrack.CreateAudioStream(), peakStore);
        }

        /// <summary>
        /// Checks if a file has a supported format.
        /// </summary>
        /// <param name="fileName">the filename to check</param>
        /// <returns>true if the file is supported, else false</returns>
        public static bool IsSupportedFile(string fileName) {
            return fileName.EndsWith(".wav");
        }

        ///// <summary>
        ///// Creates an NAudio audio stream from a file if the file format is supported.
        ///// Source: NAudio AudioPlaybackForm.cs / CreateInputStream
        ///// </summary>
        ///// <param name="fileName">name of the file to open</param>
        ///// <returns>an audio stream or null if the format is unsupported</returns>
        //public static WaveStream CreateInputStream(string fileName) {
        //    if (fileName.EndsWith(".wav")) {
        //        WaveStream readerStream = new WaveFileReader(fileName);
        //        if (readerStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm) {
        //            readerStream = WaveFormatConversionStream.CreatePcmStream(readerStream);
        //            readerStream = new BlockAlignReductionStream(readerStream);
        //        }
        //        if (readerStream.WaveFormat.BitsPerSample != 16) {
        //            var format = new WaveFormat(readerStream.WaveFormat.SampleRate, 16, readerStream.WaveFormat.Channels);
        //            readerStream = new WaveFormatConversionStream(format, readerStream);
        //        }
        //        return readerStream;
        //    }
        //    else if (fileName.EndsWith(".mp3")) {
        //        WaveStream mp3Reader = new Mp3FileReader(fileName);
        //        WaveStream pcmStream = WaveFormatConversionStream.CreatePcmStream(mp3Reader);
        //        WaveStream blockAlignedStream = new BlockAlignReductionStream(pcmStream);
        //        return blockAlignedStream;
        //    }

        //    return null;
        //}
    }
}
