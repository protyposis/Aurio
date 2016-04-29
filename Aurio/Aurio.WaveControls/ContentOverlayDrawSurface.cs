using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Aurio.WaveControls {
    /// <summary>
    /// The draw surface that the ContentOverlay uses to draw above its content.
    /// <see cref="ContentOverlay"/>
    /// </summary>
    internal class ContentOverlayDrawSurface : FrameworkElement {

        public ContentOverlayDrawSurface() {
            Loaded += TrackMarkerDrawOverlay_Loaded;
        }

        private void TrackMarkerDrawOverlay_Loaded(object sender, RoutedEventArgs e) {
            // Search the ContentOverlay that owns this DrawSurface to wire them together
            // This must be done in the Loaded event because the visual tree is not initialized at construction time
            DependencyObject element = this;
            while (element != null) {
                if (element is ContentOverlay) {
                    // We have found our owner
                    var owner = (ContentOverlay)element;

                    // Wire the owner to this instance to pass the render calls from here to the owner
                    Owner = owner;

                    // Wire this instance to the owner to give him the possibility to signal events (e.g. invalidate visual)
                    owner.DrawSurface = this; 

                    break;
                }
                element = VisualTreeHelper.GetParent(element);
            }

            if(element == null) {
                throw new Exception("the overlay could not find its owner in the visual tree");
            }
        }

        internal ContentOverlay Owner {
            get; set;
        }

        protected override void OnRender(DrawingContext drawingContext) {
            if (Owner != null) {
                Owner.OnRenderOverlay(drawingContext);
            }
        }
    }
}
