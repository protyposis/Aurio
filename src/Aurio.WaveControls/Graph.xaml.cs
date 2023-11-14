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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Aurio.WaveControls
{
    /// <summary>
    /// Interaction logic for Graph.xaml
    /// </summary>
    public partial class Graph : UserControl
    {
        public float[] Values
        {
            get { return (float[])GetValue(ValuesProperty); }
            set { SetValue(ValuesProperty, value); }
        }

        public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
            "Values",
            typeof(float[]),
            typeof(Graph),
            new UIPropertyMetadata(new float[0], new PropertyChangedCallback(ValuesChanged))
        );

        public GraphMode Mode
        {
            get { return (GraphMode)GetValue(ModeProperty); }
            set { SetValue(ModeProperty, value); }
        }

        public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
            "Mode",
            typeof(GraphMode),
            typeof(Graph),
            new UIPropertyMetadata(GraphMode.Default)
        );

        public float Minimum
        {
            get { return (float)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
            "Minimum",
            typeof(float),
            typeof(Graph),
            new UIPropertyMetadata(0f)
        );

        public float Maximum
        {
            get { return (float)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
            "Maximum",
            typeof(float),
            typeof(Graph),
            new UIPropertyMetadata(1f)
        );

        public Brush LineBrush
        {
            get { return (Brush)GetValue(LineBrushProperty); }
            set { SetValue(LineBrushProperty, value); }
        }

        public static readonly DependencyProperty LineBrushProperty = DependencyProperty.Register(
            "LineBrush",
            typeof(Brush),
            typeof(Graph),
            new UIPropertyMetadata(Brushes.Green)
        );

        public double LineThickness
        {
            get { return (double)GetValue(LineThicknessProperty); }
            set { SetValue(LineThicknessProperty, value); }
        }

        public static readonly DependencyProperty LineThicknessProperty =
            DependencyProperty.Register(
                "LineThickness",
                typeof(double),
                typeof(Graph),
                new UIPropertyMetadata(1.0d)
            );

        private long lastUpdateTime = 0;
        private long updateTimeDelta = new TimeSpan(0, 0, 1).Ticks / 25; // 25 FPS

        private static void ValuesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Graph graph = (Graph)d;

            long currentTime = DateTime.Now.Ticks;
            if (currentTime - graph.lastUpdateTime >= graph.updateTimeDelta)
            {
                graph.lastUpdateTime = currentTime;
            }
            else
            {
                return;
            }

            graph.GraphLine.Points.Clear();
            float[] dc = (float[])e.NewValue;

            if (graph.Mode == GraphMode.Default)
            {
                double min = graph.Minimum;
                double max = graph.Maximum;
                double range = max - min;
                double height = graph.ActualHeight;
                double factor = height / range;
                int count = 0;
                foreach (double value in dc)
                {
                    graph
                        .GraphLine
                        .Points
                        .Add(
                            new Point(
                                graph.ActualWidth / (dc.Length - 1) * count,
                                height - factor * (value - min)
                            )
                        );
                    count++;
                }
            }
            else if (graph.Mode == GraphMode.Fit)
            {
                double max = double.MinValue;
                double min = double.MaxValue;
                foreach (double value in dc)
                {
                    if (value > max)
                        max = value;
                    if (value < min)
                        min = value;
                }

                int count = 0;
                double height = graph.ActualHeight;
                double heightScale = height / (max - min);
                foreach (double value in dc)
                {
                    graph
                        .GraphLine
                        .Points
                        .Add(
                            new Point(
                                graph.ActualWidth / (dc.Length - 1) * count,
                                height - heightScale * (value - min)
                            )
                        );
                    count++;
                }
            }
            else if (graph.Mode == GraphMode.Decibel)
            {
                int count = 0;
                double range = graph.Maximum - graph.Minimum;
                double height = graph.ActualHeight;
                double dbFactor = height / range;
                foreach (double value in dc)
                {
                    graph
                        .GraphLine
                        .Points
                        .Add(
                            new Point(
                                graph.ActualWidth / (dc.Length - 1) * count,
                                value * -1 * dbFactor
                            )
                        );
                    count++;
                }
            }
        }

        public Graph()
        {
            InitializeComponent();
        }

        public void Reset()
        {
            GraphLine.Points.Clear();
        }
    }
}
