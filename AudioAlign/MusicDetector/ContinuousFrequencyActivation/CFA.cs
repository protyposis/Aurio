using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Project;
using AudioAlign.Audio.Streams;
using AudioAlign.Audio;
using System.IO;

namespace MusicDetector.ContinuousFrequencyActivation {
    /// <summary>
    /// Continuous Frequency Activation
    /// 
    /// "Automatic Music Detection in Television Productions"
    /// DAFx-07, Seyerlehner, Widmer
    /// http://www.cp.jku.at/people/seyerlehner/md.html
    /// </summary>
    class CFA {

        public enum Label {
            NO_MUSIC,
            MUSIC
        }

        public const float DEFAULT_THRESHOLD = 1.24f;

        private AudioTrack audioTrack;
        private readonly float threshold;
        private readonly bool smoothing;
        private readonly bool writeLog;

        public CFA(AudioTrack audioTrack, float threshold, bool smoothing, bool writeLog) {
            this.audioTrack = audioTrack;
            this.threshold = threshold;
            this.smoothing = smoothing;
            this.writeLog = writeLog;
        }

        public CFA(AudioTrack audioTrack)
            : this(audioTrack, DEFAULT_THRESHOLD, true, false) {
            // nothing to do here
        }

        public float Run() {
            IAudioStream audioStream = new ResamplingStream(
                new MonoStream(AudioStreamFactory.FromFileInfoIeee32(audioTrack.FileInfo)),
                ResamplingQuality.Medium, 11000);

            ContinuousFrequencyActivationQuantifier cfaq = new ContinuousFrequencyActivationQuantifier(audioStream);
            float[] cfaValue = new float[1];
            float[] cfaValues = new float[cfaq.WindowCount];
            Label[] cfaLabels = new Label[cfaq.WindowCount];
            int count = 0;
            int musicCount = 0;
            while (cfaq.HasNext()) {
                cfaq.ReadFrame(cfaValue);
                cfaValues[count] = cfaValue[0];
                if (cfaValue[0] > threshold) {
                    musicCount++;
                    cfaLabels[count] = Label.MUSIC;
                }
                Console.WriteLine("cfa {0,3}% {3} {1,5:0.00} {2}", (int)(Math.Round((float)count++ / cfaq.WindowCount * 100)), cfaValue[0], cfaValue[0] > threshold ? "MUSIC" : "", TimeUtil.BytesToTimeSpan(audioStream.Position, audioStream.Properties));
            }

            if (smoothing) {
                // 3.3 Smoothing

                /* majority filtering with sliding window ~5 secs
                 * 1 frame = ~2,4 secs, at least 3 frames are needed for majority filtering -> 3 * ~2,4 secs = ~7,2 secs */

                // filter out single NO_MUSIC frames
                for (int i = 2; i < cfaLabels.Length; i++) {
                    if (cfaLabels[i - 2] == Label.MUSIC && cfaLabels[i - 1] == Label.NO_MUSIC && cfaLabels[i] == Label.MUSIC) {
                        cfaLabels[i - 1] = Label.MUSIC;
                    }
                }

                // filter out single MUSIC frames
                for (int i = 2; i < cfaLabels.Length; i++) {
                    if (cfaLabels[i - 2] == Label.NO_MUSIC && cfaLabels[i - 1] == Label.MUSIC && cfaLabels[i] == Label.NO_MUSIC) {
                        cfaLabels[i - 1] = Label.NO_MUSIC;
                    }
                }

                // swap ~5 secs NO_MUSIC segments to MUSIC
                for(int i = 3; i < cfaLabels.Length; i++) {
                    if (cfaLabels[i - 3] == Label.MUSIC && cfaLabels[i - 2] == Label.NO_MUSIC && cfaLabels[i - 1] == Label.NO_MUSIC && cfaLabels[i] == Label.MUSIC) {
                        cfaLabels[i - 1] = Label.MUSIC;
                        cfaLabels[i - 2] = Label.MUSIC;
                    }
                }

                // swap ~5 secs NMUSIC segments to NO_MUSIC
                for (int i = 3; i < cfaLabels.Length; i++) {
                    if (cfaLabels[i - 3] == Label.NO_MUSIC && cfaLabels[i - 2] == Label.MUSIC && cfaLabels[i - 1] == Label.MUSIC && cfaLabels[i] == Label.NO_MUSIC) {
                        cfaLabels[i - 1] = Label.NO_MUSIC;
                        cfaLabels[i - 2] = Label.NO_MUSIC;
                    }
                }
            }

            float musicRatio = (float)musicCount / count;
            float musicRatioSmoothed = -1f;
            Console.WriteLine("'" + audioTrack.FileInfo.FullName + "' contains " + ((int)(Math.Round(musicRatio * 100))) + "% music");

            if (smoothing) {
                musicCount = cfaLabels.Count<Label>(l => l == Label.MUSIC);
                musicRatioSmoothed = (float)musicCount / count;
                Console.WriteLine("smoothed: " + ((int)(Math.Round(musicRatioSmoothed * 100))) + "% music");
            }

            if (writeLog) {
                FileInfo logFile = new FileInfo(audioTrack.FileInfo.FullName + ".music");
                StreamWriter writer = logFile.CreateText();

                writer.WriteLine(musicRatio + "; " + musicRatioSmoothed);
                writer.WriteLine(threshold);

                for (int i = 0; i < cfaValues.Length; i++) {
                    writer.WriteLine("{0:0.00000}; {1}; \t{2}", cfaValues[i], cfaValues[i] > threshold ? Label.MUSIC : Label.NO_MUSIC, cfaLabels[i]);
                }

                writer.Flush();
                writer.Close();
            }

            return 0;
        }
    }
}
