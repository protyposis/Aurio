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

namespace AudioAlign.Test.HugeControlRendering {
    public partial class SineWave : Control {

        public static readonly DependencyProperty LineProperty = DependencyProperty.Register(
            "Line", typeof(Brush), typeof(SineWave),
            new FrameworkPropertyMetadata { 
                DefaultValue = Brushes.CornflowerBlue, AffectsRender = true });

        public static readonly DependencyProperty ViewportHorizontalOffsetProperty = DependencyProperty.Register(
            "ViewportHorizontalOffset", typeof(double), typeof(SineWave),
            new FrameworkPropertyMetadata() {
                AffectsRender = true
            });

        public static readonly DependencyProperty ViewportWidthProperty = DependencyProperty.Register(
            "ViewportWidth", typeof(double), typeof(SineWave),
            new FrameworkPropertyMetadata() {
                AffectsRender = true
            });

        public SineWave() {

        }

        [Bindable(true), Category("Brushes")]
        public Brush Line {
            get { return (Brush)GetValue(LineProperty); }
            set { SetValue(LineProperty, value); }
        }

        public double ViewportHorizontalOffset {
            get { return (double)GetValue(ViewportHorizontalOffsetProperty); }
            set { SetValue(ViewportHorizontalOffsetProperty, value); }
        }

        public double ViewportWidth {
            get { return (double)GetValue(ViewportWidthProperty); }
            set { SetValue(ViewportWidthProperty, value); }
        }

        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext) {
            base.OnRender(drawingContext);

            drawingContext.DrawText(
                new FormattedText(
                    "ViewportHOffset: " + ViewportHorizontalOffset + " ViewportWidth: " + ViewportWidth,
                    CultureInfo.CurrentUICulture,
                    System.Windows.FlowDirection.LeftToRight,
                    new Typeface("Tahoma"),
                    8,
                    Brushes.Black),
                new Point(ViewportHorizontalOffset, ActualHeight) + new Vector(0, -10));

            double scale = 0.1;

            List<Point> points = new List<Point>();
            //for (double x = ViewportHorizontalOffset; x < ActualWidth * scale; x += scale) {
            //    points.Add(new Point(x / scale, Math.Sin(x) * ActualHeight / 2));
            //}
            for (double x = ViewportHorizontalOffset; x < ViewportHorizontalOffset + ViewportWidth; x++) {
                points.Add(new Point(x, Math.Sin(x * scale) * ActualHeight / 2 + ActualHeight / 2));
            }

            Debug.WriteLine("Points: " + points.Count);

            if (!points.Any()) {
                return;
            }

            PathGeometry geometry = new PathGeometry();
            PathFigure pathFigure = new PathFigure();
            pathFigure.IsClosed = false;
            pathFigure.IsFilled = false;
            pathFigure.StartPoint = points[0];
            pathFigure.Segments.Add(new PolyLineSegment(points, true));
            geometry.Figures.Add(pathFigure);
            //geometry.Transform = new TranslateTransform(0, ActualHeight / 2);

            drawingContext.DrawGeometry(null, new Pen(Brushes.BlueViolet, 1), geometry);
        }
    }
}
