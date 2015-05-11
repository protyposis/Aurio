using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Media;
using System.Diagnostics;
using Aurio;

namespace Aurio.WaveControls {
    class WaveformBitmapRenderer : IWaveformRenderer {

        private WriteableBitmap wb;
        private int[] pixels;
        private int pixelWidth;
        private int pixelHeight;
        private int pixelStride;

        public WaveformBitmapRenderer() {
            WaveformFill = Brushes.LightBlue;
            WaveformLine = Brushes.CornflowerBlue;
            WaveformSamplePoint = Brushes.RoyalBlue;
        }

        public SolidColorBrush WaveformFill { get; set; }
        public SolidColorBrush WaveformLine { get; set; }
        public SolidColorBrush WaveformSamplePoint { get; set; }

        #region IWaveformRenderer Members

        public Drawing Render(float[] sampleData, int sampleCount, int width, int height, float volume) {
            if (width > pixelWidth || height > pixelHeight) {
                AllocateBitmap(width, height);
            }

            bool peaks = sampleCount >= width;
            if (!peaks) {
                BitmapSource waveform = DrawWaveform(sampleData, sampleCount, width, height, volume);
                return new ImageDrawing(waveform, new Rect(0, 0, pixelWidth, pixelHeight));
            }
            else {
                BitmapSource waveform = DrawPeakform(sampleData, sampleCount, width, height, volume);
                return new ImageDrawing(waveform, new Rect(0, 0, pixelWidth, pixelHeight));
            }
        }

        #endregion

        private void AllocateBitmap(int width, int height) {
            wb = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            pixels = new int[width * height];
            pixelWidth = width;
            pixelHeight = height;
            pixelStride = width;
        }

        private WriteableBitmap DrawPeakform(float[] peakData, int peakCount, int width, int height, float volume) {
            Array.Clear(pixels, 0, pixels.Length);

            int borderColor = BrushToColorValue(WaveformLine);
            int fillColor = BrushToColorValue(WaveformFill);

            int halfheight = height / 2;
            int peaks = peakCount;
            int x, y, top, bottom, prevX = 0, prevY = 0, prevTop = 0, prevBottom = height;
            for (int peak = 0; peak < peaks * 2; peak += 2) {
                float p1 = peakData[peak] * volume;
                float p2 = peakData[peak + 1] * volume;

                if (p1 > 1.0f) {
                    p1 = 1.0f;
                }
                else if (p1 < -1.0f) {
                    p1 = -1.0f;
                }
                if (p2 > 1.0f) {
                    p2 = 1.0f;
                }
                else if (p2 < -1.0f) {
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

                if (bottom == height) {
                    bottom--; // for even heights the last line needs to be stripped
                }

                for (y = top; y <= bottom; y++) {
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

        private WriteableBitmap DrawWaveform(float[] sampleData, int sampleCount, int width, int height, float volume) {
            Array.Clear(pixels, 0, pixels.Length);

            int borderColor = BrushToColorValue(WaveformLine);
            int sampleColor = BrushToColorValue(WaveformSamplePoint);

            int halfheight = height / 2;
            int samples = sampleCount;
            int x, y, prevX = 0, prevY = 0;
            for (int sample = 0; sample < samples; sample++) {
                float v = sampleData[sample] * volume;

                if (v > 1.0f) {
                    v = 1.0f;
                }
                else if (v < -1.0f) {
                    v = -1.0f;
                }

                x = (int)Math.Round((float)sample / (samples - 1) * (width - 1));
                y = halfheight - (int)(halfheight * v);
                if (y == height) {
                    y--; // for even heights the last line needs to be stripped
                }

                if (sample > 0) {
                    DrawLine(prevX, prevY, x, y, pixels, pixelWidth, pixelHeight, borderColor);
                }

                if (width / samples > 4) {
                    DrawPointMarker(x, y, pixels, pixelWidth, pixelHeight, sampleColor);
                }

                prevX = x;
                prevY = y;
            }

            int stride = (wb.PixelWidth * wb.Format.BitsPerPixel) / 8;
            wb.WritePixels(new Int32Rect(0, 0, pixelWidth, pixelHeight), pixels, stride, 0);

            return wb;
        }

        private int BrushToColorValue(SolidColorBrush brush) {
            Color c = brush.Color;
            return c.A << 24 | c.R << 16 | c.G << 8 | c.B;
        }

        /// <summary>
        /// Fast Bresenham line drawing algorithm
        /// Taken and adapted from http://www.cs.unc.edu/~mcmillan/comp136/Lecture6/Lines.html (lineFast)
        /// </summary>
        private void DrawLine(int x0, int y0, int x1, int y1, int[] pixels, int width, int height, int color) {
            int dy = y1 - y0;
            int dx = x1 - x0;
            int stepx, stepy;

            if (dy < 0) { dy = -dy; stepy = -width; } else { stepy = width; }
            if (dx < 0) { dx = -dx; stepx = -1; } else { stepx = 1; }
            dy <<= 1;
            dx <<= 1;

            y0 *= width;
            y1 *= width;
            pixels[x0 + y0] = color;
            if (dx > dy) {
                int fraction = dy - (dx >> 1);
                while (x0 != x1) {
                    if (fraction >= 0) {
                        y0 += stepy;
                        fraction -= dx;
                    }
                    x0 += stepx;
                    fraction += dy;
                    pixels[x0 + y0] = color;
                }
            }
            else {
                int fraction = dx - (dy >> 1);
                while (y0 != y1) {
                    if (fraction >= 0) {
                        x0 += stepx;
                        fraction -= dy;
                    }
                    y0 += stepy;
                    fraction += dx;
                    pixels[x0 + y0] = color;
                }
            }
        }

        /// <summary>
        /// Draw a 3x3 block at a given point (similar to SoundForge).
        /// </summary>
        private void DrawPointMarker(int x, int y, int[] pixels, int width, int height, int color) {
            for (int i = x - 1; i <= x + 1; i++) {
                for (int j = y - 1; j <= y + 1; j++) {
                    if (i < 0 || j < 0 || i >= width || j >= height) {
                        continue; // skip pixels that don't fit the bitmap
                    }
                    int pixelOffset = (j * width + i);
                    pixels[pixelOffset] = color;
                }
            }


        }
    }
}
