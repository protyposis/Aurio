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
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Aurio.WaveControls
{
    public class Spectrogram : Control
    {
        /// <summary>
        /// Specified how much times the scroll mode bitmap should be larger than the actual control's width.
        /// The bigger it is, the more memory is consumed, but the less bitmap copy operations need to be executed.
        /// </summary>
        private const int SCROLL_WIDTH_FACTOR = 3;

        private WriteableBitmap writeableBitmap;
        private int position;
        private int[] pixelColumn;
        private SpectrogramMode mode;

        private int[] colorPalette;
        private bool paletteDemo = false;
        private int paletteDemoIndex = 0;

        public SpectrogramMode Mode
        {
            get { return (SpectrogramMode)GetValue(ModeProperty); }
            set { SetValue(ModeProperty, value); }
        }

        public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
            "Mode",
            typeof(SpectrogramMode),
            typeof(Spectrogram),
            new UIPropertyMetadata(SpectrogramMode.Scroll, OnModeChanged)
        );

        private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Spectrogram spectrogram = d as Spectrogram;
            spectrogram.mode = (SpectrogramMode)e.NewValue;
        }

        public int SpectrogramSize
        {
            get { return (int)GetValue(SpectrogramSizeProperty); }
            set { SetValue(SpectrogramSizeProperty, value); }
        }

        public static readonly DependencyProperty SpectrogramSizeProperty =
            DependencyProperty.Register(
                "SpectrogramSize",
                typeof(int),
                typeof(Spectrogram),
                new UIPropertyMetadata(1024)
                {
                    CoerceValueCallback = CoerceSpectrogramSize,
                    PropertyChangedCallback = OnSpectrogramSizeChanged
                }
            );

        private static object CoerceSpectrogramSize(DependencyObject d, object value)
        {
            int i = (int)value;
            return i < 1 ? 1 : i;
        }

        private static void OnSpectrogramSizeChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            Spectrogram spectrogram = d as Spectrogram;
            spectrogram.InitializeSpectrogramBitmap(true);
        }

        public float Minimum
        {
            get { return (float)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
            "Minimum",
            typeof(float),
            typeof(Spectrogram),
            new UIPropertyMetadata(-100f)
        );

        public float Maximum
        {
            get { return (float)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
            "Maximum",
            typeof(float),
            typeof(Spectrogram),
            new UIPropertyMetadata(0f)
        );

        public Spectrogram()
        {
            ColorGradient gradient = new ColorGradient(0, 1);
            gradient.AddStop(Colors.Black, 0);
            gradient.AddStop(Colors.DarkBlue, 0.25f);
            gradient.AddStop(Colors.DarkOrange, 0.7f);
            gradient.AddStop(Colors.Yellow, 0.9f);
            gradient.AddStop(Colors.White, 1);
            colorPalette = gradient.GetGradientArgbArray(1024);

            ClipToBounds = true;
            mode = Mode;
        }

        public int[] ColorPalette
        {
            get { return colorPalette; }
            set { colorPalette = value; }
        }

        public long ColumnCount { get; private set; }

        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            drawingContext.DrawRectangle(
                Background,
                null,
                new Rect(0, 0, ActualWidth, ActualHeight)
            );

            if (mode == SpectrogramMode.Scroll)
            {
                drawingContext.DrawDrawing(
                    new ImageDrawing(
                        writeableBitmap,
                        new Rect(ActualWidth - position, 0, ActualWidth * 3, ActualHeight)
                    )
                );
            }
            else
            {
                drawingContext.DrawDrawing(
                    new ImageDrawing(writeableBitmap, new Rect(0, 0, ActualWidth, ActualHeight))
                );
            }
        }

        protected override void OnRenderSizeChanged(System.Windows.SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            // recreate bitmap for current control size
            InitializeSpectrogramBitmap(true);
        }

        public void AddSpectrogramColumn(float[] values)
        {
            if (values.Length != pixelColumn.Length)
            {
                throw new Exception(
                    "illegal values array - length should be "
                        + pixelColumn.Length
                        + " but is "
                        + values.Length
                );
            }

            if (paletteDemo)
            {
                for (int x = 0; x < pixelColumn.Length; x++)
                {
                    pixelColumn[pixelColumn.Length - 1 - x] = colorPalette[paletteDemoIndex];
                }
                if (++paletteDemoIndex % colorPalette.Length == 0)
                {
                    paletteDemoIndex = 0;
                }
            }
            else
            {
                float factor = (colorPalette.Length - 1) / (Maximum - Minimum);
                for (int x = 0; x < pixelColumn.Length; x++)
                {
                    float value = values[x];
                    int color;
                    if (value > Maximum)
                    {
                        color = colorPalette[colorPalette.Length - 1];
                    }
                    else if (value < Minimum)
                    {
                        color = colorPalette[0];
                    }
                    else
                    {
                        color = colorPalette[colorPalette.Length - 1 + (int)(factor * value)];
                    }
                    pixelColumn[pixelColumn.Length - 1 - x] = color;
                }
            }

            writeableBitmap.WritePixels(
                new Int32Rect(position, 0, 1, values.Length),
                pixelColumn,
                4,
                0
            );

            if (++position >= writeableBitmap.PixelWidth)
            {
                if (mode == SpectrogramMode.Scroll)
                {
                    position = (int)ActualWidth;
                    InitializeSpectrogramBitmap(false);
                }
                else
                {
                    position = 0;
                }
            }

            InvalidateVisual();
            ColumnCount++;
        }

        public void AddPointMarker(long columnIndex, int rowIndex, Color color)
        {
            if (columnIndex >= ColumnCount)
            {
                throw new Exception(
                    $"Column index is larger than the number of drawn columns ({columnIndex} > ´{ColumnCount})"
                );
            }

            var columnOffset = ColumnCount - columnIndex;

            if (position - columnOffset < 0)
            {
                Debug.WriteLine(
                    $"Ignoring point marker at column {columnIndex} because it is too far behind "
                        + $"and outside the drawing range ({ColumnCount - columnOffset} - {ColumnCount})"
                );
            }

            int pixelColor = ColorGradient.ColorToArgb(color);
            int pixelColumnPosition = (int)(position - columnOffset);

            // alternatively use GetBitmapContext() + SetPixel() but WritePixel() may just do that internally
            writeableBitmap.WritePixels(
                new Int32Rect(pixelColumnPosition, pixelColumn.Length - 1 - rowIndex, 1, 1),
                new[] { pixelColor },
                4,
                0
            );
        }

        public void AddLineMarker(
            long columnIndexFrom,
            int rowIndexFrom,
            long columnIndexTo,
            int rowIndexTo,
            Color color
        )
        {
            var maxColumnIndex = Math.Max(columnIndexFrom, columnIndexTo);

            if (maxColumnIndex >= ColumnCount)
            {
                throw new Exception(
                    $"Column index is larger than the number of drawn columns ({maxColumnIndex} > ´{ColumnCount})"
                );
            }

            var columnOffset = ColumnCount - position;

            if (columnIndexFrom - columnOffset < 0)
            {
                Debug.WriteLine(
                    $"Ignoring line marker at column {columnIndexFrom} because it is too far behind "
                        + $"and outside the drawing range ({ColumnCount - columnOffset} - {ColumnCount})"
                );
            }

            var bitmapColumnPositionFrom = (int)(columnIndexFrom - columnOffset);
            var bitmapColumnPositionTo = (int)(columnIndexTo - columnOffset);
            var pixelColor = ColorGradient.ColorToArgb(color);

            try
            {
                writeableBitmap.Lock();
                unsafe
                {
                    var pBackBuffer = writeableBitmap.BackBuffer;
                    BitmapUtils.DrawLine(
                        bitmapColumnPositionFrom,
                        pixelColumn.Length - 1 - rowIndexFrom,
                        bitmapColumnPositionTo,
                        pixelColumn.Length - 1 - rowIndexTo,
                        (int*)pBackBuffer,
                        writeableBitmap.BackBufferStride / 4,
                        writeableBitmap.PixelHeight,
                        pixelColor
                    );
                }

                var rect = new Int32Rect(
                    Math.Min(bitmapColumnPositionFrom, bitmapColumnPositionTo),
                    Math.Min(rowIndexFrom, rowIndexTo),
                    Math.Abs(bitmapColumnPositionTo - bitmapColumnPositionFrom),
                    Math.Abs(rowIndexTo - rowIndexFrom)
                );

                writeableBitmap.AddDirtyRect(rect);
            }
            finally
            {
                writeableBitmap.Unlock();
            }
        }

        public void Reset()
        {
            InitializeSpectrogramBitmap(true);
        }

        private void InitializeSpectrogramBitmap(bool sizeChanged)
        {
            if (writeableBitmap == null)
            { // first time initialization
                writeableBitmap = new WriteableBitmap(
                    mode == SpectrogramMode.Scroll
                        ? (int)ActualWidth * SCROLL_WIDTH_FACTOR
                        : (int)ActualWidth,
                    SpectrogramSize,
                    96,
                    96,
                    PixelFormats.Bgra32,
                    null
                );
                pixelColumn = new int[SpectrogramSize];
            }
            else
            {
                if (pixelColumn.Length != SpectrogramSize)
                {
                    pixelColumn = new int[SpectrogramSize];
                }
                if (mode == SpectrogramMode.Scroll)
                {
                    if (sizeChanged)
                    {
                        writeableBitmap = new WriteableBitmap(
                            (int)ActualWidth * SCROLL_WIDTH_FACTOR,
                            SpectrogramSize,
                            96,
                            96,
                            PixelFormats.Bgra32,
                            null
                        );
                    }
                    else
                    {
                        CopyPixels(writeableBitmap, writeableBitmap);
                    }
                    position = (int)ActualWidth;
                }
                else if (mode == SpectrogramMode.Static)
                {
                    if (sizeChanged)
                    {
                        writeableBitmap = new WriteableBitmap(
                            (int)ActualWidth,
                            SpectrogramSize,
                            96,
                            96,
                            PixelFormats.Bgra32,
                            null
                        );
                    }
                    position = 0;
                }
            }
            InvalidateVisual();
        }

        private static void CopyPixels(WriteableBitmap src, WriteableBitmap dest)
        {
            int twoThirds = src.PixelWidth / 3 * 2;
            int third = src.PixelWidth / 3;
            int height = src.PixelHeight;

            Int32Rect srcRect = new Int32Rect(twoThirds, 0, third, height);
            Int32Rect destRect = new Int32Rect(0, 0, third, height);

            //// pixel copy with intermediate buffer
            //int[] buffer = new int[twoThirds * height];
            //src.CopyPixels(srcRect, buffer, third * 4, 0);
            //dest.WritePixels(destRect, buffer, third * 4, 0);

            // direct pixel copy
            dest.WritePixels(
                srcRect,
                src.BackBuffer,
                src.BackBufferStride * src.PixelHeight,
                src.BackBufferStride,
                destRect.X,
                destRect.Y
            );
        }
    }
}
