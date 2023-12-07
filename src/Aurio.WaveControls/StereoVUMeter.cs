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

using System.Windows;
using System.Windows.Controls;

namespace Aurio.WaveControls
{
    public class StereoVUMeter : Control
    {
        public static readonly DependencyProperty AmplitudeLeftProperty;
        public static readonly DependencyProperty AmplitudeRightProperty;

        static StereoVUMeter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(StereoVUMeter),
                new FrameworkPropertyMetadata(typeof(StereoVUMeter))
            );

            AmplitudeLeftProperty = DependencyProperty.Register(
                "AmplitudeLeft",
                typeof(double),
                typeof(StereoVUMeter),
                new FrameworkPropertyMetadata(0.0d) { AffectsRender = true }
            );

            AmplitudeRightProperty = DependencyProperty.Register(
                "AmplitudeRight",
                typeof(double),
                typeof(StereoVUMeter),
                new FrameworkPropertyMetadata(0.0d) { AffectsRender = true }
            );
        }

        public StereoVUMeter() { }

        public double AmplitudeLeft
        {
            get { return (double)GetValue(AmplitudeLeftProperty); }
            set { SetValue(AmplitudeLeftProperty, value); }
        }

        public double AmplitudeRight
        {
            get { return (double)GetValue(AmplitudeRightProperty); }
            set { SetValue(AmplitudeRightProperty, value); }
        }

        public void Reset()
        {
            ClearValue(AmplitudeLeftProperty);
            ClearValue(AmplitudeRightProperty);
        }
    }
}
