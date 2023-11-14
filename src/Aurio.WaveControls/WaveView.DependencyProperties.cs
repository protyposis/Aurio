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
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Aurio;
using Aurio.Project;

namespace Aurio.WaveControls
{
    public partial class WaveView
    {
        public static readonly DependencyProperty AudioTrackProperty;
        public static readonly DependencyProperty RenderModeProperty;
        public static readonly DependencyProperty DrawTrackNameProperty;

        public static readonly DependencyProperty WaveformBackgroundProperty;
        public static readonly DependencyProperty WaveformLineProperty;
        public static readonly DependencyProperty WaveformFillProperty;
        public static readonly DependencyProperty WaveformSamplePointProperty;
        public static readonly DependencyProperty TrackLengthProperty;
        public static readonly DependencyProperty TrackOffsetProperty;

        private static readonly DependencyPropertyKey TrackScrollLengthPropertyKey; // TrackLength - ViewportWidth
        public static readonly DependencyProperty TrackScrollLengthProperty;

        private static readonly DependencyPropertyKey ViewportZoomPropertyKey; // ActualWidth / ViewportWidth
        public static readonly DependencyProperty ViewportZoomProperty;

        public static readonly RoutedEvent TrackOffsetChangedEvent;

        static WaveView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(WaveView),
                new FrameworkPropertyMetadata(typeof(WaveView))
            );

            AudioTrackProperty = DependencyProperty.Register(
                "AudioTrack",
                typeof(AudioTrack),
                typeof(WaveView),
                new FrameworkPropertyMetadata
                {
                    DefaultValue = null,
                    AffectsRender = true,
                    PropertyChangedCallback = OnAudioTrackChanged
                }
            );

            RenderModeProperty = DependencyProperty.Register(
                "RenderMode",
                typeof(WaveViewRenderMode),
                typeof(WaveView),
                new FrameworkPropertyMetadata
                {
                    DefaultValue = WaveViewRenderMode.Bitmap,
                    AffectsRender = true
                }
            );

            DrawTrackNameProperty = DependencyProperty.Register(
                "DrawTrackName",
                typeof(bool),
                typeof(WaveView),
                new FrameworkPropertyMetadata { DefaultValue = false, AffectsRender = true }
            );

            WaveformBackgroundProperty = DependencyProperty.Register(
                "WaveformBackground",
                typeof(SolidColorBrush),
                typeof(WaveView),
                new FrameworkPropertyMetadata
                {
                    DefaultValue = Brushes.White,
                    AffectsRender = true,
                    PropertyChangedCallback = OnWaveformBackgroundChanged
                }
            );

            WaveformLineProperty = DependencyProperty.Register(
                "WaveformLine",
                typeof(SolidColorBrush),
                typeof(WaveView),
                new FrameworkPropertyMetadata
                {
                    DefaultValue = Brushes.CornflowerBlue,
                    AffectsRender = true,
                    PropertyChangedCallback = OnWaveformLineChanged
                }
            );

            WaveformFillProperty = DependencyProperty.Register(
                "WaveformFill",
                typeof(SolidColorBrush),
                typeof(WaveView),
                new FrameworkPropertyMetadata
                {
                    DefaultValue = Brushes.LightBlue,
                    AffectsRender = true
                }
            );

            WaveformSamplePointProperty = DependencyProperty.Register(
                "WaveformSamplePoint",
                typeof(SolidColorBrush),
                typeof(WaveView),
                new FrameworkPropertyMetadata
                {
                    DefaultValue = Brushes.RoyalBlue,
                    AffectsRender = true
                }
            );

            TrackLengthProperty = DependencyProperty.Register(
                "TrackLength",
                typeof(long),
                typeof(WaveView),
                new FrameworkPropertyMetadata
                {
                    AffectsRender = true,
                    PropertyChangedCallback = OnTrackLengthChanged
                }
            );

            TrackOffsetProperty = DependencyProperty.Register(
                "TrackOffset",
                typeof(long),
                typeof(WaveView),
                new FrameworkPropertyMetadata
                {
                    AffectsRender = true,
                    PropertyChangedCallback = OnTrackOffsetChanged
                }
            );

            TrackScrollLengthPropertyKey = DependencyProperty.RegisterReadOnly(
                "TrackScrollLength",
                typeof(long),
                typeof(WaveView),
                new FrameworkPropertyMetadata()
            );
            TrackScrollLengthProperty = TrackScrollLengthPropertyKey.DependencyProperty;

            ViewportZoomPropertyKey = DependencyProperty.RegisterReadOnly(
                "ViewportZoom",
                typeof(float),
                typeof(WaveView),
                new FrameworkPropertyMetadata()
            );
            ViewportZoomProperty = ViewportZoomPropertyKey.DependencyProperty;

            TrackOffsetChangedEvent = EventManager.RegisterRoutedEvent(
                "TrackOffsetChanged",
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(WaveView)
            );
        }

        private static void OnAudioTrackChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            WaveView waveView = d as WaveView;
            AudioTrack audioTrack = e.NewValue as AudioTrack;
            if (waveView != null && audioTrack != null)
            {
                waveView.audioTrack = audioTrack;

                if (waveView.IsLoaded)
                {
                    // If element is already loaded this must be explicitly triggered,
                    // else it will be done by the Loaded event
                    waveView.LoadAudioTrack();
                }
            }
        }

        private static void audioStream_WaveformChanged(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void OnWaveformLineChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            WaveView waveView = (WaveView)d;
            waveView._lineBrush = e.NewValue as SolidColorBrush;
            waveView.ApplyLineBrushToRenderers();
        }

        private static void OnWaveformBackgroundChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            WaveView waveView = (WaveView)d;
            waveView._backgroundBrush = e.NewValue as SolidColorBrush;
        }

        private static void OnTrackLengthChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            UpdateTrackScrollLength(d);
        }

        private static void OnTrackOffsetChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            WaveView waveView = d as WaveView;
            waveView.RaiseEvent(new RoutedEventArgs(TrackOffsetChangedEvent, waveView));
        }

        private static object CoerceTrackLength(DependencyObject d, object value)
        {
            long trackLength = (long)value;
            // avoid negative length
            return trackLength >= 0 ? trackLength : 0;
        }

        private static void UpdateTrackScrollLength(DependencyObject d)
        {
            long trackLength = (long)d.GetValue(TrackLengthProperty);
            long viewportWidth = (long)d.GetValue(VirtualViewportWidthProperty);
            d.SetValue(TrackScrollLengthPropertyKey, trackLength - viewportWidth);
        }

        private static void UpdateViewportZoom(DependencyObject d)
        {
            long viewportWidth = (long)d.GetValue(VirtualViewportWidthProperty);
            double actualWidth = (double)d.GetValue(ActualWidthProperty);
            d.SetValue(ViewportZoomPropertyKey, (float)(actualWidth / viewportWidth));
        }

        public AudioTrack AudioTrack
        {
            get { return (AudioTrack)GetValue(AudioTrackProperty); }
            set { SetValue(AudioTrackProperty, value); }
        }

        public WaveViewRenderMode RenderMode
        {
            get { return (WaveViewRenderMode)GetValue(RenderModeProperty); }
            set { SetValue(RenderModeProperty, value); }
        }

        public bool DrawTrackName
        {
            get { return (bool)GetValue(DrawTrackNameProperty); }
            set { SetValue(DrawTrackNameProperty, value); }
        }

        [Bindable(true), Category("Brushes")]
        public SolidColorBrush WaveformBackground
        {
            get { return (SolidColorBrush)GetValue(WaveformBackgroundProperty); }
            set { SetValue(WaveformBackgroundProperty, value); }
        }

        [Bindable(true), Category("Brushes")]
        public SolidColorBrush WaveformLine
        {
            get { return (SolidColorBrush)GetValue(WaveformLineProperty); }
            set { SetValue(WaveformLineProperty, value); }
        }

        [Bindable(true), Category("Brushes")]
        public SolidColorBrush WaveformFill
        {
            get { return (SolidColorBrush)GetValue(WaveformFillProperty); }
            set { SetValue(WaveformFillProperty, value); }
        }

        [Bindable(true), Category("Brushes")]
        public SolidColorBrush WaveformSamplePoint
        {
            get { return (SolidColorBrush)GetValue(WaveformSamplePointProperty); }
            set { SetValue(WaveformSamplePointProperty, value); }
        }

        public long TrackLength
        {
            get { return (long)GetValue(TrackLengthProperty); }
            set { SetValue(TrackLengthProperty, value); }
        }

        public long TrackOffset
        {
            get { return (long)GetValue(TrackOffsetProperty); }
            set { SetValue(TrackOffsetProperty, value); }
        }

        public long TrackScrollLength
        {
            get { return (long)GetValue(TrackScrollLengthProperty); }
        }

        public float ViewportZoom
        {
            get { return (float)GetValue(ViewportZoomProperty); }
        }

        private void WaveView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged && e.PreviousSize.Width > 0)
            {
                //ViewportWidth = (long)(ViewportWidth * e.NewSize.Width / e.PreviousSize.Width);
            }
            UpdateViewportZoom(this);
            //InvalidateVisual();
        }

        protected override void OnViewportWidthChanged(long oldValue, long newValue)
        {
            UpdateTrackScrollLength(this);
            UpdateViewportZoom(this);
        }
    }
}
