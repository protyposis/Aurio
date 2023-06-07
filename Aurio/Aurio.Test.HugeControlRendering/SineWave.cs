using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Globalization;
using System.Windows.Media.Imaging;

namespace Aurio.Test.HugeControlRendering
{
    public partial class SineWave : Control
    {
        public static readonly DependencyProperty LineProperty = DependencyProperty.Register(
            "Line",
            typeof(Brush),
            typeof(SineWave),
            new FrameworkPropertyMetadata
            {
                DefaultValue = Brushes.CornflowerBlue,
                AffectsRender = true
            }
        );

        public static readonly DependencyProperty ViewportHorizontalOffsetProperty =
            DependencyProperty.Register(
                "ViewportHorizontalOffset",
                typeof(double),
                typeof(SineWave),
                new FrameworkPropertyMetadata() { AffectsRender = true }
            );

        public static readonly DependencyProperty ViewportWidthProperty =
            DependencyProperty.Register(
                "ViewportWidth",
                typeof(double),
                typeof(SineWave),
                new FrameworkPropertyMetadata() { AffectsRender = true }
            );

        public static readonly DependencyProperty SineRenderModeProperty =
            DependencyProperty.Register(
                "SineRenderMode",
                typeof(SineRenderMode),
                typeof(SineWave),
                new FrameworkPropertyMetadata(SineRenderMode.DirectTransformless)
                {
                    AffectsRender = true
                }
            );

        public SineWave() { }

        [Bindable(true), Category("Brushes")]
        public Brush Line
        {
            get { return (Brush)GetValue(LineProperty); }
            set { SetValue(LineProperty, value); }
        }

        public double ViewportHorizontalOffset
        {
            get { return (double)GetValue(ViewportHorizontalOffsetProperty); }
            set { SetValue(ViewportHorizontalOffsetProperty, value); }
        }

        public double ViewportWidth
        {
            get { return (double)GetValue(ViewportWidthProperty); }
            set { SetValue(ViewportWidthProperty, value); }
        }

        public SineRenderMode SineRenderMode
        {
            get { return (SineRenderMode)GetValue(SineRenderModeProperty); }
            set { SetValue(SineRenderModeProperty, value); }
        }

        public bool Antialiased
        {
            get { return ((EdgeMode)GetValue(RenderOptions.EdgeModeProperty)) != EdgeMode.Aliased; }
            set
            {
                SetValue(
                    RenderOptions.EdgeModeProperty,
                    !value ? EdgeMode.Aliased : EdgeMode.Unspecified
                );
            }
        }

        //public bool SoftwareRendering {
        //    get { return ((System.Windows.Interop.RenderMode)GetValue(RenderOptions.ProcessRenderMode)) == System.Windows.Interop.RenderMode.SoftwareOnly; }
        //    set { SetValue(RenderOptions.ProcessRenderMode, value ? System.Windows.Interop.RenderMode.SoftwareOnly : System.Windows.Interop.RenderMode.Default); }
        //}

        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            SineRenderMode renderMode = SineRenderMode;

            drawingContext.DrawText(
                new FormattedText(
                    "ViewportHOffset: "
                        + ViewportHorizontalOffset
                        + " ViewportWidth: "
                        + ViewportWidth
                        + ", RenderMode: "
                        + renderMode,
                    CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Tahoma"),
                    8,
                    Brushes.Black,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip
                ),
                new Point(ViewportHorizontalOffset, ActualHeight) + new Vector(0, -10)
            );

            // generate samples
            double scale = 0.1;
            List<double> samples = new List<double>();
            for (
                double x = ViewportHorizontalOffset;
                x < ViewportHorizontalOffset + ViewportWidth;
                x++
            )
            {
                samples.Add(Math.Sin(x * scale));
            }
            Debug.WriteLine("Samples: " + samples.Count);

            if (!samples.Any())
            {
                return;
            }

            switch (renderMode)
            {
                case SineRenderMode.DirectTransformless:
                    drawingContext.DrawGeometry(
                        null,
                        new Pen(Brushes.BlueViolet, 1),
                        CreateGeometryTransformless(samples)
                    );
                    break;

                case SineRenderMode.Direct:
                    drawingContext.DrawGeometry(
                        null,
                        new Pen(Brushes.BlueViolet, 1),
                        CreateGeometry(samples)
                    );
                    break;

                case SineRenderMode.BitmapTransformless:
                    BitmapSource bmp = RenderBitmapTransformless(samples);
                    Rect rect = new Rect(
                        ViewportHorizontalOffset,
                        0,
                        bmp.PixelWidth,
                        bmp.PixelHeight
                    );
                    drawingContext.DrawImage(bmp, rect);
                    break;
            }
        }

        private Geometry CreateGeometry(List<double> samples)
        {
            List<Point> points = new List<Point>();

            for (int x = 0; x < samples.Count; x++)
            {
                double transformedSample = samples[x];
                points.Add(new Point(x, transformedSample));
            }

            PathGeometry geometry = new PathGeometry();
            PathFigure pathFigure = new PathFigure();
            pathFigure.IsClosed = false;
            pathFigure.IsFilled = false;
            pathFigure.StartPoint = points[0];
            pathFigure.Segments.Add(new PolyLineSegment(points, true));
            geometry.Figures.Add(pathFigure);

            TransformGroup transformGroup = new TransformGroup();
            transformGroup.Children.Add(new ScaleTransform(1, ActualHeight / 2));
            transformGroup.Children.Add(
                new TranslateTransform(ViewportHorizontalOffset, ActualHeight / 2)
            );
            geometry.Transform = transformGroup;

            return geometry;
        }

        private Geometry CreateGeometryTransformless(List<double> samples)
        {
            List<Point> points = new List<Point>();

            for (int x = 0; x < samples.Count; x++)
            {
                double transformedSample = samples[x] * ActualHeight / 2 + ActualHeight / 2;
                points.Add(new Point(x + ViewportHorizontalOffset, transformedSample));
            }

            PathGeometry geometry = new PathGeometry();
            PathFigure pathFigure = new PathFigure();
            pathFigure.IsClosed = false;
            pathFigure.IsFilled = false;
            pathFigure.StartPoint = points[0];
            pathFigure.Segments.Add(new PolyLineSegment(points, true));
            geometry.Figures.Add(pathFigure);

            return geometry;
        }

        private BitmapSource RenderBitmapTransformless(List<double> samples)
        {
            List<Point> points = new List<Point>();

            for (int x = 0; x < samples.Count; x++)
            {
                double transformedSample = samples[x] * ActualHeight / 2 + ActualHeight / 2;
                points.Add(new Point(x, transformedSample));
            }

            PathGeometry geometry = new PathGeometry();
            PathFigure pathFigure = new PathFigure();
            pathFigure.IsClosed = false;
            pathFigure.IsFilled = false;
            pathFigure.StartPoint = points[0];
            pathFigure.Segments.Add(new PolyLineSegment(points, true));
            geometry.Figures.Add(pathFigure);

            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawGeometry(null, new Pen(Brushes.BlueViolet, 1), geometry);
            }

            RenderTargetBitmap bmp = new RenderTargetBitmap(
                samples.Count,
                (int)ActualHeight,
                96,
                96,
                PixelFormats.Pbgra32
            );
            bmp.Render(drawingVisual);

            return bmp;
        }
    }
}
