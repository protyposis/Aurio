using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;
using System.Windows.Media;
using System.Windows;

namespace AudioAlign.WaveControls {
    public class Spectrogram : Control {

        private WriteableBitmap writableBitmap;
        private int position;
        private int[] pixelColumn;

        private int[] colorPalette;

        public int SpectrogramSize {
            get { return (int)GetValue(SpectrogramSizeProperty); }
            set { SetValue(SpectrogramSizeProperty, value); }
        }

        public static readonly DependencyProperty SpectrogramSizeProperty =
            DependencyProperty.Register("SpectrogramSize", typeof(int), typeof(Spectrogram),
            new UIPropertyMetadata(1024) {
                CoerceValueCallback = CoerceSpectrogramSize,
                PropertyChangedCallback = OnSpectrogramSizeChanged
            });

        private static object CoerceSpectrogramSize(DependencyObject d, object value) {
            int i = (int)value;
            return i < 1 ? 1 : i;
        }

        private static void OnSpectrogramSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            Spectrogram spectrogram = d as Spectrogram;
            spectrogram.InitializeSpectrogramBitmap();
        }

        public float Minimum {
            get { return (float)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(float), typeof(Spectrogram), new UIPropertyMetadata(-100f));

        public float Maximum {
            get { return (float)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(float), typeof(Spectrogram), new UIPropertyMetadata(0f));

        public Spectrogram() {
            ColorGradient gradient = new ColorGradient(0, 1);
            gradient.AddStop(Colors.Black, 0);
            gradient.AddStop(Colors.White, 1);
            colorPalette = gradient.GetGradient(256).Select(c => GetColorValue(c)).ToArray();
        }

        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext) {
            base.OnRender(drawingContext);

            drawingContext.DrawRectangle(Background, null, new Rect(0, 0, ActualWidth, ActualHeight));

            ImageDrawing drawing = new ImageDrawing(writableBitmap, new Rect(0, 0, ActualWidth, ActualHeight));
            drawingContext.DrawDrawing(drawing);
        }

        protected override void OnRenderSizeChanged(System.Windows.SizeChangedInfo sizeInfo) {
            base.OnRenderSizeChanged(sizeInfo);

            // recreate bitmap for current control size
            InitializeSpectrogramBitmap();
        }

        public void AddSpectrogramColumn(float[] values) {
            if (values.Length != pixelColumn.Length) {
                throw new Exception("illegal values array - length should be " + pixelColumn.Length 
                    + " but is " + values.Length);
            }

            float factor = 255f / (Maximum - Minimum);
            for (int x = 0; x < pixelColumn.Length; x++) {
                float value = values[x];
                int color;
                if (value > Maximum) {
                    color = colorPalette[255];
                }
                else if (value < Minimum) {
                    color = colorPalette[0];
                }
                else {
                    color = colorPalette[255 + (int)(factor * values[x])];
                }
                pixelColumn[pixelColumn.Length - 1 - x] = color;
            }

            writableBitmap.WritePixels(new Int32Rect(position, 0, 1, values.Length), pixelColumn, 4, 0);

            if (++position >= writableBitmap.PixelWidth) {
                position = 0;
            }

            InvalidateVisual();
        }

        private void InitializeSpectrogramBitmap() {
            writableBitmap = new WriteableBitmap(
                (int)ActualWidth, SpectrogramSize, 96, 96, PixelFormats.Bgra32, null);
            pixelColumn = new int[SpectrogramSize];
            position = 0;
            InvalidateVisual();
        }

        private static int GetColorValue(Color c) {
            return c.A << 24 | c.R << 16 | c.G << 8 | c.B;
        }
    }
}
