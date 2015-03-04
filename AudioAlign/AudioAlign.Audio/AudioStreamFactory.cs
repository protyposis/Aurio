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
using AudioAlign.Audio.Streams;
using System.Collections.Concurrent;

namespace AudioAlign.Audio {
    public static class AudioStreamFactory {

        private const int SAMPLES_PER_PEAK = 256;

        private static BlockingCollection<Action> peakStoreQueue = new BlockingCollection<Action>();
        private static volatile int peakStoreQueueThreads = 0;

        private static WaveStream OpenFile(FileInfo fileInfo) {
            if (fileInfo.Extension.Equals(".wav")) {
                return new WaveFileReader(fileInfo.FullName);
            }
            else if (fileInfo.Extension.Equals(".mp3")) {
                return new Mp3FileReader(fileInfo.FullName);
            }
            return null;
        }

        private static IAudioStream TryOpenSourceStream(FileInfo fileInfo) {
            WaveStream waveStream;
            if ((waveStream = OpenFile(fileInfo)) != null) {
                return new NAudioSourceStream(waveStream);
            }
            else {
                try {
                    return new FFmpegSourceStream(fileInfo);
                }
                catch {
                    // file probably unsupported
                }
            }
            return null;
        }

        public static IAudioStream FromFileInfo(FileInfo fileInfo) {
            return TryOpenSourceStream(fileInfo);
        }

        public static IAudioStream FromFileInfoIeee32(FileInfo fileInfo) {
            return new IeeeStream(TryOpenSourceStream(fileInfo));
        }

        public static VisualizingStream FromAudioTrackForGUI(AudioTrack audioTrack) {
            VisualizingStream visualizingStream = 
                new VisualizingStream(audioTrack.CreateAudioStream(),
                    CreatePeakStore(audioTrack, audioTrack.TimeWarps.Count == 0));

            // TODO if timewarps are added but total length stays the same, the peakstore still has to be refreshed
            audioTrack.LengthChanged += delegate(object sender, ValueEventArgs<TimeSpan> e) {
                visualizingStream.PeakStore = CreatePeakStore(audioTrack, false);
            };

            return visualizingStream;
        }

        /// <summary>
        /// Checks if a file has a supported format.
        /// </summary>
        /// <param name="fileName">the filename to check</param>
        /// <returns>true if the file is supported, else false</returns>
        public static bool IsSupportedFile(string fileName) {
            return TryOpenSourceStream(new FileInfo(fileName)) != null;
        }

        public static void WriteToFile(IAudioStream stream, string targetFileName) {
            WaveFileWriter.CreateWaveFile(targetFileName, new NAudioSinkStream(stream));
        }

        private static PeakStore CreatePeakStore(AudioTrack audioTrack, bool fileSupport) {
            IAudioStream audioInputStream = audioTrack.CreateAudioStream();

            PeakStore peakStore = new PeakStore(SAMPLES_PER_PEAK, audioInputStream.Properties.Channels,
                (int)Math.Ceiling((float)audioInputStream.Length / audioInputStream.SampleBlockSize / SAMPLES_PER_PEAK));

            Action peakStoreFillAction = delegate {
                FillPeakStore(audioTrack, fileSupport, audioInputStream, peakStore);
            };

            // add task
            peakStoreQueue.Add(peakStoreFillAction);

            // create consumer/worker threads
            for (; peakStoreQueueThreads < Math.Min(2, Environment.ProcessorCount); peakStoreQueueThreads++) {
                Task.Factory.StartNew(() => {
                    // process peakstore actions as long as the queue is not empty
                    Debug.WriteLine("PeakStoreQueue thread started");
                    while (peakStoreQueue.Count > 0) {
                        peakStoreQueue.Take().Invoke();
                    }
                    peakStoreQueueThreads--;
                    Debug.WriteLine("PeakStoreQueue thread stopped");
                });
            }

            return peakStore;
        }

        private static void FillPeakStore(AudioTrack audioTrack, bool fileSupport, IAudioStream audioInputStream, PeakStore peakStore) {
            // search for existing peakfile
            if (audioTrack.HasPeakFile && fileSupport) {
                // load peakfile from disk
                peakStore.ReadFrom(File.OpenRead(audioTrack.PeakFile.FullName));
                peakStore.CalculateScaledData(8, 6);
            }
            // generate peakfile
            else {
                int channels = peakStore.Channels;
                byte[] buffer = new byte[65536 * audioInputStream.SampleBlockSize];
                float[] min = new float[channels];
                float[] max = new float[channels];
                BinaryWriter[] peakWriters = peakStore.CreateMemoryStreams().WrapWithBinaryWriters();

                IProgressReporter progressReporter = ProgressMonitor.GlobalInstance.BeginTask("Generating peaks for " + audioTrack.FileInfo.Name, true);
                DateTime startTime = DateTime.Now;
                int sampleBlockCount = 0;
                int peakCount = 0;
                int bytesRead;
                long totalSampleBlocks = audioInputStream.Length / audioInputStream.SampleBlockSize;
                long totalSamplesRead = 0;
                int progress = 0;

                for (int i = 0; i < channels; i++) {
                    min[i] = float.MaxValue;
                    max[i] = float.MinValue;
                }

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
                                    if (min[channel] > bufferF[samplesProcessed]) {
                                        min[channel] = bufferF[samplesProcessed];
                                    }
                                    if (max[channel] < bufferF[samplesProcessed]) {
                                        max[channel] = bufferF[samplesProcessed];
                                    }
                                    samplesProcessed++;
                                    totalSamplesRead++;
                                }

                                if (++sampleBlockCount % SAMPLES_PER_PEAK == 0 || sampleBlockCount == totalSampleBlocks) {
                                    // write peak
                                    peakCount++;
                                    for (int channel = 0; channel < channels; channel++) {
                                        peakWriters[channel].Write(new Peak(min[channel], max[channel]));
                                        // add last sample of previous peak as first sample of current peak to make consecutive peaks overlap
                                        // this gives the impression of a continuous waveform
                                        min[channel] = max[channel] = bufferF[samplesProcessed - channels];
                                    }
                                    //sampleBlockCount = 0;
                                }
                            }
                            while (samplesProcessed < samplesRead);

                            progressReporter.ReportProgress(100.0f / audioInputStream.Length * audioInputStream.Position);
                            if ((int)(100.0f / audioInputStream.Length * audioInputStream.Position) > progress) {
                                progress = (int)(100.0f / audioInputStream.Length * audioInputStream.Position);
                                peakStore.OnPeaksChanged();
                            }
                        }
                    }
                }

                Debug.WriteLine("generating downscaled peaks...");
                peakStore.CalculateScaledData(8, 6);

                Debug.WriteLine("peak generation finished - " + (DateTime.Now - startTime) + ", " + (peakWriters[0].BaseStream.Length * channels) + " bytes");
                progressReporter.Finish();

                if (fileSupport) {
                    // write peakfile to disk
                    try {
                        FileStream peakOutputFile = File.OpenWrite(audioTrack.PeakFile.FullName);
                        peakStore.StoreTo(peakOutputFile);
                        peakOutputFile.Close();
                    }
                    catch (UnauthorizedAccessException e) {
                        Debug.WriteLine("peak file writing failed: " + e.Message);
                    }
                }
            }
            peakStore.OnPeaksChanged();
        }
    }
}
