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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio
{
    public enum WindowType
    {
        Rectangle,
        Triangle,
        /// <summary>
        /// Symmetric Hann window. COLA with hop size at (length-1)/2.
        /// </summary>
        Hann,
        /// <summary>
        /// Periodic Hann window. COLA with hop size at length/2.
        /// </summary>
        HannPeriodic,
        Hamming,
        Blackman,
        Nuttall,
        BlackmanHarris,
        BlackmanNuttall
    }

    /// <summary>
    /// Util to generate and apply window functions.
    /// See: Fensterfunktionen in der FFT (http://www.statistics4u.com/fundstat_germ/ee_fft_windowing.html) [contains wrong formulas!!!]
    /// See: Windowing Functions Improve FFT Results, Part I (http://www.tmworld.com/article/322450-Windowing_Functions_Improve_FFT_Results_Part_I.php)
    /// See: http://en.wikipedia.org/wiki/Window_function
    /// </summary>
    public static class WindowUtil
    {

        /// <summary>
        /// (No weighting)
        /// </summary>
        public static void Rectangle(float[] samples, int offset, int length)
        {
            // zero all samples before the desired window interval (as it is the same as a multiplication by 0)
            for (int x = 0; x < offset; x++)
            {
                samples[x] = 0;
            }

            // do nothing for samples in the desired rectangle interval as it is the same as multiplying all samples by 1

            // zero all samples after the desired window interval (as it is the same as a multiplication by 0)
            for (int x = offset + length; x < samples.Length; x++)
            {
                samples[x] = 0;
            }
        }

        /// <summary>
        /// (Bartlett-Window)
        /// </summary>
        public static void Triangle(float[] samples, int offset, int length)
        {
            float index = -(length - 1) / 2f;
            float N = length / 2f;
            for (int x = offset; x < offset + length; x++)
            {
                samples[x] *= 1 - Math.Abs(index) / N;
                index += 1;
            }
        }

        /// <summary>
        /// (Cosinus-Glockenfenster; (von) Hann; Hanning [sic])
        /// </summary>
        public static void Hann(float[] samples, int offset, int length)
        {
            int index = 0;
            int N = length - 1;
            for (int x = offset; x < offset + length; x++)
            {
                samples[x] *= (float)(0.5 * (1f - Math.Cos(2 * Math.PI * index / N)));
                index++;
            }
        }

        /// <summary>
        /// Hann window with COLA property when length is even and hop size is length/2, useful for FFT and overlap-add.
        /// For uneven lengths, COLA can be achieved by using the normal Hann window with a hop size of (length-1)/2.
        /// </summary>
        public static void HannPeriodic(float[] samples, int offset, int length)
        {
            float[] hann = GetArray(WindowType.Hann, length + 1);

            for (int x = offset; x < offset + length; x++)
            {
                samples[x] *= hann[x];
            }
        }

        public static void Hamming(float[] samples, int offset, int length)
        {
            int index = 0;
            int N = length - 1;
            for (int x = offset; x < offset + length; x++)
            {
                samples[x] *= (float)(0.54 - 0.46 * Math.Cos(2 * Math.PI * index / N));
                index++;
            }
        }

        public static void Blackman(float[] samples, int offset, int length)
        {
            int index = 0;
            int N = length - 1;
            for (int x = offset; x < offset + length; x++)
            {
                samples[x] *= (float)(0.42
                    - 0.50 * Math.Cos(2 * Math.PI * index / N)
                    + 0.08 * Math.Cos(4 * Math.PI * index / N));
                index++;
            }
        }

        public static void BlackmanHarris(float[] samples, int offset, int length)
        {
            int index = 0;
            int N = length - 1;
            for (int x = offset; x < offset + length; x++)
            {
                samples[x] *= (float)(0.35875
                    - 0.48829 * Math.Cos(2 * Math.PI * index / N)
                    + 0.14128 * Math.Cos(4 * Math.PI * index / N)
                    - 0.01168 * Math.Cos(6 * Math.PI * index / N));
                index++;
            }
        }

        public static void BlackmanNuttall(float[] samples, int offset, int length)
        {
            int index = 0;
            int N = length - 1;
            for (int x = offset; x < offset + length; x++)
            {
                samples[x] *= (float)(0.3635819
                    - 0.4891775 * Math.Cos(2 * Math.PI * index / N)
                    + 0.1365995 * Math.Cos(4 * Math.PI * index / N)
                    - 0.0106411 * Math.Cos(6 * Math.PI * index / N));
                index++;
            }
        }

        public static void Nuttall(float[] samples, int offset, int length)
        {
            int index = 0;
            int N = length - 1;
            for (int x = offset; x < offset + length; x++)
            {
                samples[x] *= (float)(0.355768
                    - 0.487396 * Math.Cos(2 * Math.PI * index / N)
                    + 0.144232 * Math.Cos(4 * Math.PI * index / N)
                    - 0.012604 * Math.Cos(6 * Math.PI * index / N));
                index++;
            }
        }

        public static float[] GetArray(WindowType windowType, int windowSize, float normalizationFactor)
        {
            float[] window = new float[windowSize];
            for (int x = 0; x < window.Length; x++)
            {
                window[x] = normalizationFactor;
            }

            switch (windowType)
            {
                case WindowType.Rectangle:
                    Rectangle(window, 0, window.Length);
                    break;
                case WindowType.Triangle:
                    Triangle(window, 0, window.Length);
                    break;
                case WindowType.Hann:
                    Hann(window, 0, window.Length);
                    break;
                case WindowType.HannPeriodic:
                    HannPeriodic(window, 0, window.Length);
                    break;
                case WindowType.Hamming:
                    Hamming(window, 0, window.Length);
                    break;
                case WindowType.Blackman:
                    Blackman(window, 0, window.Length);
                    break;
                case WindowType.Nuttall:
                    Nuttall(window, 0, window.Length);
                    break;
                case WindowType.BlackmanHarris:
                    BlackmanHarris(window, 0, window.Length);
                    break;
                case WindowType.BlackmanNuttall:
                    BlackmanNuttall(window, 0, window.Length);
                    break;
                default:
                    throw new ArgumentException("unsupported window type: " + windowType);
            }

            return window;
        }

        public static float[] GetArray(WindowType windowType, int windowSize)
        {
            return GetArray(windowType, windowSize, 1.0f);
        }

        public static WindowFunction GetFunction(WindowType windowType, int windowSize, float normalizationFactor)
        {
            return new WindowFunction(GetArray(windowType, windowSize, normalizationFactor), windowType);
        }

        public static WindowFunction GetFunction(WindowType windowType, int windowSize)
        {
            return new WindowFunction(GetArray(windowType, windowSize), windowType);
        }

        public static void Apply(float[] values, int valuesOffset, float[] window)
        {
            if (values.Length - valuesOffset < window.Length)
            {
                throw new ArgumentException("lengths of input arrays don't match");
            }

            for (int x = 0; x < window.Length; x++)
            {
                values[x + valuesOffset] *= window[x];
            }
        }
    }
}
