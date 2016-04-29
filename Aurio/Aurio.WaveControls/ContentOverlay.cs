using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Aurio.WaveControls {
    /// <summary>
    /// Wraps an element and adds an overlay to draw above the wrapped element. 
    /// 
    /// Directly drawing in a ContentControl does not work because the content is rendered afterwards and
    /// overpaints what was drawn, i.e. the content drawing is one layer above the ContentControl drawing.
    /// 
    /// This control adds a layer above the content and makes it possible to draw over the content. This
    /// is similar to an adorner, except the adorner cannot be configures in XAML and must be added in
    /// code behind. This control can just be added IN XAML as a wrapper of a content element to draw above.
    /// </summary>
    public abstract class ContentOverlay : VirtualContentViewBase {

        static ContentOverlay() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ContentOverlay),
                new FrameworkPropertyMetadata(typeof(ContentOverlay)));
        }

        internal ContentOverlayDrawSurface DrawSurface { get; set; }

        internal abstract void OnRenderOverlay(DrawingContext drawingContext);

        protected override void OnViewportOffsetChanged(long oldValue, long newValue) {
            DrawSurface?.InvalidateVisual();
        }

        protected override void OnViewportWidthChanged(long oldValue, long newValue) {
            DrawSurface?.InvalidateVisual();
        }
    }
}
