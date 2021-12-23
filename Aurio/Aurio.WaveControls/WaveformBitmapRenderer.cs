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
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Media;
using System.Diagnostics;
using Aurio;

namespace Aurio.WaveControls
{
    class WaveformBitmapRenderer : IWaveformRenderer
    {

        private WriteableBitmap wb;
        private int[] pixels;
        private int pixelWidth;
        private int pixelHeight;
        private int pixelStride;

        public WaveformBitmapRenderer()
        {
            WaveformFill = Brushes.LightBlue;
            WaveformLine = Brushes.CornflowerBlue;
            WaveformSamplePoint = Brushes.RoyalBlue;
        }

        public SolidColorBrush WaveformFill { get; set; }
        public SolidColorBrush WaveformLine { get; set; }
        public SolidColorBrush WaveformSamplePoint { get; set; }

        #region IWaveformRenderer Members

        public Drawing Render(float[] sampleData, int sampleCount, int width, int height, float volume)
        {
            if (width > pixelWidth || height > pixelHeight)
            {
                AllocateBitmap(width, height);
            }

            bool peaks = sampleCount >= width;
            if (!peaks)
            {
                BitmapSource waveform = DrawWaveform(sampleData, sampleCount, width, height, volume);
                return new ImageDrawing(waveform, new Rect(0, 0, pixelWidth, pixelHeight));
            }
            else
            {
                BitmapSource waveform = DrawPeakform(sampleData, sampleCount, width, height, volume);
                return new ImageDrawing(waveform, new Rect(0, 0, pixelWidth, pixelHeight));
            }
        }

        #endregion

        private void AllocateBitmap(int width, int height)
        {
            wb = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            pixels = new int[width * height];
            pixelWidth = width;
            pixelHeight = height;
            pixelStride = width;
        }

        private WriteableBitmap DrawPeakform(float[] peakData, int peakCount, int width, int height, float volume)
        {
            Array.Clear(pixels, 0, pixels.Length);

            int borderColor = BrushToColorValue(WaveformLine);
            int fillColor = BrushToColorValue(WaveformFill);

            int halfheight = height / 2;
            int peaks = peakCount;
            int x, y, top, bottom, prevX = 0, prevY = 0, prevTop = 0, prevBottom = height;
            for (int peak = 0; peak < peaks * 2; peak += 2)
            {
                float p1 = peakData[peak] * volume;
                float p2 = peakData[peak + 1] * volume;

                if (p1 > 1.0f)
                {
                    p1 = 1.0f;
                }
                else if (p1 < -1.0f)
                {
                    p1 = -1.0f;
                }
                if (p2 > 1.0f)
                {
                    p2 = 1.0f;
                }
                else if (p2 < -1.0f)
                {
                    p2 = -1.0f;
                }

                int pp1 = (int)(halfheight * p1);
                int pp2 = (int)(halfheight * p2);

                // NOTE:
                // The peaks are distributed among the available width. If more peaks than pixel columns are
                // given, columns can contain multiple peaks, which could lead to drawing errors:
                // If the two peaks 10->20 and 20->30 are merged, the resulting column has a hole 
                // between 20->30. Solution would be to combine them to a single column 10->30 (if it is
                // ever getting noticeable).
                // TODO resolve drawing issues of combined peaks if noticeable
                x = (int)Math.Round((float)peak / 2 / (peaks - 1) * (width - 1));

                top = halfheight - pp2;
                bottom = halfheight - pp1;

                if (bottom == height)
                {
                    bottom--; // for even heights the last line needs to be stripped
                }

                for (y = top; y <= bottom; y++)
                {
                    //bool useBorderColor = 
                    //    y == top // topmost peak pixel
                    //    || y == bottom // bottommost peak pixel
                    //    || (x > 0 && top < prevTop && y > top && y < prevTop) // upper rising lines
                    //    || (x > 0 && bottom > prevBottom && y < bottom && y > prevBottom); // lower falling lines
                    bool useBorderColor = true;
                    int pixelOffset = (y * pixelWidth + x);
                    pixels[pixelOffset] = useBorderColor ? borderColor : fillColor;
                }

                prevTop = top;
                prevBottom = bottom;
                prevX = x;
                prevY = y;
            }

            int stride = (wb.PixelWidth * wb.Format.BitsPerPixel) / 8;
            wb.WritePixels(new Int32Rect(0, 0, pixelWidth, pixelHeight), pixels, stride, 0);

            return wb;
        }

        private WriteableBitmap DrawWaveform(float[] sampleData, int sampleCount, int width, int height, float volume)
        {
            Array.Clear(pixels, 0, pixels.Length);

            int borderColor = BrushToColorValue(WaveformLine);
            int sampleColor = BrushToColorValue(WaveformSamplePoint);

            int halfheight = height / 2;
            int samples = sampleCount;
            int x, y, prevX = 0, prevY = 0;
            for (int sample = 0; sample < samples; sample++)
            {
                float v = sampleData[sample] * volume;

                if (v > 1.0f)
                {
                    v = 1.0f;
                }
                else if (v < -1.0f)
                {
                    v = -1.0f;
                }

                x = (int)Math.Round((float)sample / (samples - 1) * (width - 1));
                y = halfheight - (int)(halfheight * v);
                if (y == height)
                {
                    y--; // for even heights the last line needs to be stripped
                }

                if (sample > 0)
                {
                    BitmapUtils.DrawLine(prevX, prevY, x, y, pixels, pixelWidth, pixelHeight, borderColor);
                }

                if (width / samples > 4)
                {
                    BitmapUtils.DrawPointMarker(x, y, pixels, pixelWidth, pixelHeight, sampleColor);
                }

                prevX = x;
                prevY = y;
            }

            int stride = (wb.PixelWidth * wb.Format.BitsPerPixel) / 8;
            wb.WritePixels(new Int32Rect(0, 0, pixelWidth, pixelHeight), pixels, stride, 0);

            return wb;
        }

        private int BrushToColorValue(SolidColorBrush brush)
        {
            Color c = brush.Color;
            return c.A << 24 | c.R << 16 | c.G << 8 | c.B;
        }
    }
}
