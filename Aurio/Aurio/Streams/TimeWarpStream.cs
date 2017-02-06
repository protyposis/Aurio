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
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections;

namespace Aurio.Streams {
    public class TimeWarpStream : AbstractAudioStreamWrapper {

        private TimeWarpCollection mappings;
        private ByteTimeWarpCollection byteMappings;
        private long length;
        private long position;
        private CropStream cropStream;
        private ResamplingStream resamplingStream;

        public TimeWarpStream(IAudioStream sourceStream)
            : base(sourceStream) {
                byteMappings = new ByteTimeWarpCollection(0);
                Mappings = new TimeWarpCollection();
                length = sourceStream.Length;
                position = sourceStream.Position;

                ResetStream();
        }

        public TimeWarpStream(IAudioStream sourceStream, TimeWarpCollection mappings)
            : this(sourceStream) {
                Mappings = mappings;
        }

        public TimeWarpCollection Mappings {
            get { return mappings; }
            set {
                if (mappings != null) {
                    mappings.CollectionChanged -= mappings_CollectionChanged;
                }
                mappings = value;
                mappings.CollectionChanged += mappings_CollectionChanged;
                mappings_CollectionChanged(null, null);
            }
        }

        private void mappings_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            var alpha = new ByteTimeWarp { From = 0, To = 0 };
            var omega = new ByteTimeWarp { From = sourceStream.Length, To = sourceStream.Length };

            if (mappings.Count > 0) {
                // Convert time mappings to byte mappings
                // This is the place where the TimeWarps and ByteTimeWarps are kept in sync!!
                byteMappings = new ByteTimeWarpCollection(mappings.Count);
                foreach (TimeWarp mapping in mappings) {
                    byteMappings.Add(ByteTimeWarp.Convert(mapping, Properties));
                }

                var first = byteMappings.Alpha;
                if (first.From > alpha.From) {
                    // The first mapping is not at the start of the stream, insert start of stream alpha
                    byteMappings.Insert(0, alpha);
                }

                var last = byteMappings.Omega;
                if (last.From < omega.From) {
                    // The last mapping is not at the end of the stream, insert EOS omega
                    omega.To += last.Offset;
                    byteMappings.Insert(byteMappings.Count, omega);
                }
            }
            else {
                byteMappings.Add(alpha);
                byteMappings.Add(omega);
            }

            if (position > byteMappings.Omega.To) {
                Position = byteMappings.Omega.To;
            }

            ResetStream();
        }

        private void ResetStream(bool hardReset) {
            if (position > byteMappings.Omega.To) {
                throw new Exception("position beyond length");
            }

            length = byteMappings.Omega.To;

            byteMappings.SetWarpedPosition(position);
            ByteTimeWarp mL = byteMappings.Lower;
            ByteTimeWarp mH = byteMappings.Upper;


            if (cropStream == null || cropStream.Begin != mL.From || cropStream.End != mH.From || resamplingStream.Length != byteMappings.Omega.To) {
                // mapping has changed, stream subsection must be renewed
                if (hardReset) {
                    cropStream = new CropStream(sourceStream, mL.From, mH.From);
                    if(resamplingStream != null) {
                        resamplingStream.Close();
                    }
                    resamplingStream = new ResamplingStream(cropStream, ResamplingQuality.VariableRate, ByteTimeWarp.CalculateSampleRateRatio(mL, mH));
                    resamplingStream.Position = position - mL.To;
                }
                else {
                    // Reset the streams to the new conditions without creating new instances as the hard reset does
                    // NOTE always hard resetting works too, but this mode has been added to keep the sample resampler throughout playback

                    // Reset crop stream to source bounds, else the successive setting of begin and end can fail if 
                    // the new begin position is after the old end position and the validation in the crop stream fails
                    cropStream.Begin = 0;
                    cropStream.End = sourceStream.Length;

                    // Reset the crop stream to the new bounds
                    cropStream.Begin = mL.From;
                    cropStream.End = mH.From;
                    cropStream.Position = 0;

                    // Reset the resampling stream
                    resamplingStream.SampleRateRatio = ByteTimeWarp.CalculateSampleRateRatio(mL, mH);
                    resamplingStream.Position = position - mL.To;
                }
            }
            
        }
        private void ResetStream() {
            ResetStream(true);
        }

        public override long Length {
            get { return length; }
        }

        public override long Position {
            get { return position; }
            set {
                if (value < 0 || value > length) {
                    throw new ArgumentException("invalid position");
                }

                byteMappings.SetWarpedPosition(value);

                if (position < byteMappings.Lower.To || position >= byteMappings.Upper.To) {
                    // new stream subsection required
                    position = value;
                    ResetStream(false);
                }
                else {
                    position = value;
                    resamplingStream.Position = position - byteMappings.Lower.To;
                }
            }
        }

        public override int Read(byte[] buffer, int offset, int count) {
            int bytesRead = resamplingStream.Read(buffer, offset, count);

            if (bytesRead == 0 && cropStream.End != sourceStream.Length) {
                //PrintDebugStatus();
                ResetStream(false); // switch to next subsection
                return this.Read(buffer, offset, count);
            }

            position += bytesRead;
            return bytesRead;
        }

        public override void Close() {
            if(resamplingStream != null) {
                resamplingStream.Close();
            }
            base.Close();
        }

        private void PrintDebugStatus() {
            Debug.WriteLine("TimeWarpStream len {0,10}, pos {1,10}", length, position);
            Debug.WriteLine("     resampler len {0,10}, pos {1,10} src buffer {2,4}", resamplingStream.Length, resamplingStream.Position, resamplingStream.BufferedBytes);
            Debug.WriteLine("          crop len {0,10}, pos {1,10}, beg {2,10}, end {3,10}", cropStream.Length, cropStream.Position, cropStream.Begin, cropStream.End);
            Debug.WriteLine("        source len {0,10}, pos {1,10}", sourceStream.Length, sourceStream.Position);
        }

        /// <summary>
        /// This is basically a copy of the <see cref="TimeWarp"/> class for internal use,
        /// with the difference that it operates on byte positions in the stream instead of time positions.
        /// 
        /// Using time positions internally in this stream lead to problems with byte borders and rounding,
        /// e.g. converting the time of the end of a stream to bytes would not necessarily result
        /// in the end byte number, but a few before or after. Also, constantly converting times and bytes
        /// back and forth is probably not very performant.
        /// 
        /// Because this class is a copy, it needs to stay in sync with the TimeWarp class. Unfortunately
        /// it is not possible to create a common base class, because long and TimeSpan do not share a 
        /// common interface which makes generic computations impossible.
        /// </summary>
        private class ByteTimeWarp {

            public long From { get; set; }
            public long To { get; set; }

            public long Offset {
                get { return To - From; }
            }

            public static double CalculateSampleRateRatio(ByteTimeWarp mL, ByteTimeWarp mH) {
                return (mH.To - mL.To) / (double)(mH.From - mL.From);
            }

            public static ByteTimeWarp Convert(TimeWarp timeWarp, AudioProperties properties) {
                return new ByteTimeWarp {
                    From = TimeUtil.TimeSpanToBytes(timeWarp.From, properties),
                    To = TimeUtil.TimeSpanToBytes(timeWarp.To, properties)
                };
            }
        }

        /// <summary>
        /// This is a copy of the <see cref="TimeWarpCollection"/>, with the difference that it
        /// is a list and not an ObservableCollection, which isn't needed here.
        /// It mirrors important computation functions that need to stay in sync with their sources.
        /// </summary>
        private class ByteTimeWarpCollection : List<ByteTimeWarp> {

            private int currentIndex = 0;

            public ByteTimeWarpCollection(int size) : base(size) { }

            public ByteTimeWarp Alpha {
                get { return this.First(); }
            }

            public ByteTimeWarp Omega {
                get { return this.Last(); }
            }

            public int CurrentIndex {
                get { return currentIndex; }
                set { currentIndex = value; }
            }

            public ByteTimeWarp Lower {
                get { return this[currentIndex]; }
            }

            public ByteTimeWarp Upper {
                get { return this[currentIndex + 1]; }
            }

            public bool Next() {
                if (currentIndex < Count - 2) {
                    currentIndex++;
                    return true;
                }

                return false;
            }

            public void SetWarpedPosition(long warpedPosition) {
                if (warpedPosition < Alpha.To || warpedPosition > Omega.To) {
                    throw new ArgumentOutOfRangeException("invalid warped position " + warpedPosition);
                }

                // Either the warpedPosition falls into an interval
                for (currentIndex = 0; currentIndex < Count - 1; currentIndex++) {
                    if (warpedPosition >= Lower.To && warpedPosition < Upper.To) {
                        return;
                    }
                }

                // Or it is the EOS position so it isn't caught by the loop above
                // but it is still considered part of the last interval that the loop
                // stopped at.
                currentIndex--;
            }
        }
    }
}
