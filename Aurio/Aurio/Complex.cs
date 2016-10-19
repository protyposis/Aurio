using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurio {
    /// <summary>
    /// Represents a complex number made of real and imaginary parts.
    /// </summary>
    public class Complex {

        private double re;
        private double im;

        public Complex(double re, double im) {
            this.re = re;
            this.im = im;
        }

        /// <summary>
        /// Gets or sets the real part.
        /// </summary>
        public double Real {
            get { return re; }
            set { re = value; }
        }

        /// <summary>
        /// Gets or sets the imaginary part.
        /// </summary>
        public double Imaginary {
            get { return im; }
            set { im = value; }
        }

        /// <summary>
        /// Gets the calculated magnitude of this complex number.
        /// </summary>
        public double Magnitude {
            get { return Math.Sqrt(re * re + im * im); }
        }

        /// <summary>
        /// Gets the calculated phase of this complex number.
        /// </summary>
        public double Phase {
            get { return Math.Atan2(im, re); }
        }

        /// <summary>
        /// Returns a complex number whose real and imaginary parts are calculated from the supplied magnitude and phase.
        /// </summary>
        /// <param name="magnitude">the magnitude of a complex number</param>
        /// <param name="phase">the phase of a complex number</param>
        /// <returns>a complex number</returns>
        public static Complex FromMagnitudeAndPhase(double magnitude, double phase) {
            // http://math.stackexchange.com/a/42781
            double re = magnitude * Math.Cos(phase);
            double im = magnitude * Math.Sin(phase);
            return new Complex(re, im);
        }
    }
}
