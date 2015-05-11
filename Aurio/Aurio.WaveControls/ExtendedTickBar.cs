using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Globalization;
using System.Windows.Controls;

namespace Aurio.WaveControls {
    public class ExtendedTickBar : FrameworkElement {

        public static readonly DependencyProperty MinimumProperty;
        public static readonly DependencyProperty MaximumProperty;
        public static readonly DependencyProperty TicksProperty;
        public static readonly DependencyProperty TickRenderModeProperty;
        public static readonly DependencyProperty TextAlignmentProperty;
        public static readonly DependencyProperty FillProperty;

        static ExtendedTickBar() {
            MinimumProperty = DependencyProperty.Register("Minimum", typeof(double), typeof(ExtendedTickBar),
                new FrameworkPropertyMetadata(-60.0d) { AffectsRender = true });

            MaximumProperty = DependencyProperty.Register("Maximum", typeof(double), typeof(ExtendedTickBar),
                new FrameworkPropertyMetadata(0.0d) { AffectsRender = true });

            TicksProperty = DependencyProperty.Register("Ticks", typeof(DoubleCollection), typeof(ExtendedTickBar),
                new FrameworkPropertyMetadata(new DoubleCollection()) { AffectsRender = true });

            TickRenderModeProperty = DependencyProperty.Register("TickRenderMode", typeof(TickRenderMode), typeof(ExtendedTickBar),
                new FrameworkPropertyMetadata(TickRenderMode.Both) { AffectsRender = true });

            TextAlignmentProperty = DependencyProperty.Register("TextAlignment", typeof(TextAlignment), typeof(ExtendedTickBar), 
                new FrameworkPropertyMetadata(TextAlignment.Right) { AffectsRender = true });

            FillProperty = DependencyProperty.Register("Fill", typeof(Brush), typeof(ExtendedTickBar),
                new FrameworkPropertyMetadata((Brush)null) {  AffectsRender = true });
        }

        public ExtendedTickBar() {
        }

        public double Minimum {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public double Maximum {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public DoubleCollection Ticks {
            get { return (DoubleCollection)GetValue(TicksProperty); }
            set { SetValue(TicksProperty, value); }
        }

        public TickRenderMode TickRenderMode {
            get { return (TickRenderMode)GetValue(TickRenderModeProperty); }
            set { SetValue(TickRenderModeProperty, value); }
        }

        public TextAlignment TextAlignment {
            get { return (TextAlignment)GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }

        public Brush Fill {
            get { return (Brush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext) {
            double textMaxWidth = 0;
            double textX = ActualWidth;

            switch (TextAlignment) {
                case TextAlignment.Left:
                    textX = 0;
                    break;
                case TextAlignment.Center:
                    textX = ActualWidth / 2;
                    break;
                case TextAlignment.Right:
                    textX = ActualWidth;
                    break;
            }

            if (TickRenderMode == TickRenderMode.Text || TickRenderMode == TickRenderMode.Both) {
                foreach (double value in Ticks) {
                    double y = CalculateY(value);

                    FormattedText text = new FormattedText(value.ToString(), CultureInfo.CurrentUICulture,
                        FlowDirection.LeftToRight, new Typeface("Tahoma"), 8.0d, Fill) {
                            TextAlignment = TextAlignment
                        };
                    drawingContext.DrawText(text, new Point(textX, y - text.Height / 2));
                    textMaxWidth = Math.Max(textMaxWidth, text.Width);
                }

                textMaxWidth += 3;
            }

            if (TickRenderMode == TickRenderMode.Tick || TickRenderMode == TickRenderMode.Both) {
                Pen pen = new Pen(Fill, 1.0d);

                GuidelineSet guidelineSet = new GuidelineSet();
                drawingContext.PushGuidelineSet(guidelineSet);

                foreach (double value in Ticks) {
                    double y = CalculateY(value) + 1;
                    drawingContext.DrawLine(pen, new Point(0, y), new Point(ActualWidth - textMaxWidth, y));
                    guidelineSet.GuidelinesY.Add(y - 0.5);
                }

                drawingContext.Pop();
            }
        }

        private double CalculateY(double value) {
            double actualHeight = ActualHeight - 2;
            return actualHeight - actualHeight * ((value - Minimum) / (Maximum - Minimum));
        }
    }

    public enum TickRenderMode {
        Tick,
        Text,
        Both
    }
}
