﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;

namespace AudioAlign.WaveControls {
    public class VirtualViewBase: Control {

        public static readonly DependencyProperty ViewportOffsetProperty = DependencyProperty.Register(
            "ViewportOffset", typeof(long), typeof(VirtualViewBase),
                new FrameworkPropertyMetadata { AffectsRender = true });

        public static readonly DependencyProperty ViewportWidthProperty = DependencyProperty.Register(
            "ViewportWidth", typeof(long), typeof(VirtualViewBase),
                new FrameworkPropertyMetadata { AffectsRender = true, 
                    PropertyChangedCallback = OnViewportWidthChanged, 
                    CoerceValueCallback = CoerceViewportWidth, DefaultValue = (long)100 });

        private static object CoerceViewportWidth(DependencyObject d, object value) {
            long viewportWidth = (long)value;
            // avoid negative length
            return viewportWidth >= 0 ? viewportWidth : 0;
        }

        private static void OnViewportWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            VirtualViewBase ctrl = (VirtualViewBase)d;
            ctrl.OnViewportWidthChanged((long)e.OldValue, (long)e.NewValue);
        }

        public long ViewportOffset {
            get { return (long)GetValue(ViewportOffsetProperty); }
            set { SetValue(ViewportOffsetProperty, value); }
        }

        public long ViewportWidth {
            get { return (long)GetValue(ViewportWidthProperty); }
            set { SetValue(ViewportWidthProperty, value); }
        }

        protected virtual void OnViewportWidthChanged(long oldValue, long newValue) {}
    }
}