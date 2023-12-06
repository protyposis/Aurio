//
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2017  Mario Guggenberger <mg@protyposis.net>
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.IO;
using System.Linq;
using Aurio.Project;
using Aurio.Resampler;
using Aurio.Streams;

namespace Aurio.Features.ContinuousFrequencyActivation
{
    /// <summary>
    /// Continuous Frequency Activation
    ///
    /// "Automatic Music Detection in Television Productions"
    /// DAFx-07, Seyerlehner, Widmer
    /// http://www.cp.jku.at/people/seyerlehner/md.html
    /// </summary>
    public class CFA
    {
        public enum Label
        {
            NO_MUSIC,
            MUSIC
        }

        public const float DEFAULT_THRESHOLD = 1.24f;

        private AudioTrack audioTrack;
        private readonly float threshold;
        private readonly bool smoothing;
        private readonly bool writeLog;

        public CFA(AudioTrack audioTrack, float threshold, bool smoothing, bool writeLog)
        {
            this.audioTrack = audioTrack;
            this.threshold = threshold;
            this.smoothing = smoothing;
            this.writeLog = writeLog;
        }

        public CFA(AudioTrack audioTrack)
            : this(audioTrack, DEFAULT_THRESHOLD, true, false)
        {
            // nothing to do here
        }

        public float Run()
        {
            IAudioStream audioStream = new ResamplingStream(
                new MonoStream(AudioStreamFactory.FromFileInfoIeee32(audioTrack.FileInfo)),
                ResamplingQuality.Medium,
                11000
            );

            ContinuousFrequencyActivationQuantifier cfaq =
                new ContinuousFrequencyActivationQuantifier(audioStream);
            float[] cfaValue = new float[1];
            float[] cfaValues = new float[cfaq.WindowCount];
            Label[] cfaLabels = new Label[cfaq.WindowCount];
            int count = 0;
            int musicCount = 0;
            while (cfaq.HasNext())
            {
                cfaq.ReadFrame(cfaValue);
                cfaValues[count] = cfaValue[0];
                if (cfaValue[0] > threshold)
                {
                    musicCount++;
                    cfaLabels[count] = Label.MUSIC;
                }
                Console.WriteLine(
                    "cfa {0,3}% {3} {1,5:0.00} {2}",
                    (int)(Math.Round((float)count++ / cfaq.WindowCount * 100)),
                    cfaValue[0],
                    cfaValue[0] > threshold ? "MUSIC" : "",
                    TimeUtil.BytesToTimeSpan(audioStream.Position, audioStream.Properties)
                );
            }

            audioStream.Close();

            if (smoothing)
            {
                // 3.3 Smoothing

                /* majority filtering with sliding window ~5 secs
                 * 1 frame = ~2,4 secs, at least 3 frames are needed for majority filtering -> 3 * ~2,4 secs = ~7,2 secs */

                // filter out single NO_MUSIC frames
                for (int i = 2; i < cfaLabels.Length; i++)
                {
                    if (
                        cfaLabels[i - 2] == Label.MUSIC
                        && cfaLabels[i - 1] == Label.NO_MUSIC
                        && cfaLabels[i] == Label.MUSIC
                    )
                    {
                        cfaLabels[i - 1] = Label.MUSIC;
                    }
                }

                // filter out single MUSIC frames
                for (int i = 2; i < cfaLabels.Length; i++)
                {
                    if (
                        cfaLabels[i - 2] == Label.NO_MUSIC
                        && cfaLabels[i - 1] == Label.MUSIC
                        && cfaLabels[i] == Label.NO_MUSIC
                    )
                    {
                        cfaLabels[i - 1] = Label.NO_MUSIC;
                    }
                }

                // swap ~5 secs NO_MUSIC segments to MUSIC
                for (int i = 3; i < cfaLabels.Length; i++)
                {
                    if (
                        cfaLabels[i - 3] == Label.MUSIC
                        && cfaLabels[i - 2] == Label.NO_MUSIC
                        && cfaLabels[i - 1] == Label.NO_MUSIC
                        && cfaLabels[i] == Label.MUSIC
                    )
                    {
                        cfaLabels[i - 1] = Label.MUSIC;
                        cfaLabels[i - 2] = Label.MUSIC;
                    }
                }

                // swap ~5 secs NMUSIC segments to NO_MUSIC
                for (int i = 3; i < cfaLabels.Length; i++)
                {
                    if (
                        cfaLabels[i - 3] == Label.NO_MUSIC
                        && cfaLabels[i - 2] == Label.MUSIC
                        && cfaLabels[i - 1] == Label.MUSIC
                        && cfaLabels[i] == Label.NO_MUSIC
                    )
                    {
                        cfaLabels[i - 1] = Label.NO_MUSIC;
                        cfaLabels[i - 2] = Label.NO_MUSIC;
                    }
                }
            }

            float musicRatio = (float)musicCount / count;
            float musicRatioSmoothed = -1f;
            Console.WriteLine(
                "'"
                    + audioTrack.FileInfo.FullName
                    + "' contains "
                    + ((int)(Math.Round(musicRatio * 100)))
                    + "% music"
            );

            if (smoothing)
            {
                musicCount = cfaLabels.Count<Label>(l => l == Label.MUSIC);
                musicRatioSmoothed = (float)musicCount / count;
                Console.WriteLine(
                    "smoothed: " + ((int)(Math.Round(musicRatioSmoothed * 100))) + "% music"
                );
            }

            if (writeLog)
            {
                FileInfo logFile = new FileInfo(audioTrack.FileInfo.FullName + ".music");
                StreamWriter writer = logFile.CreateText();

                writer.WriteLine(musicRatio + "; " + musicRatioSmoothed);
                writer.WriteLine(threshold);

                for (int i = 0; i < cfaValues.Length; i++)
                {
                    writer.WriteLine(
                        "{0:0.00000}; {1}; \t{2}",
                        cfaValues[i],
                        cfaValues[i] > threshold ? Label.MUSIC : Label.NO_MUSIC,
                        cfaLabels[i]
                    );
                }

                writer.Flush();
                writer.Close();
            }

            return 0;
        }
    }
}
