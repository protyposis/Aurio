using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections;

namespace AudioAlign.Audio.Streams {
    public class TimeWarpStream : AbstractAudioStreamWrapper {

        private TimeWarpCollection mappings;
        private ByteTimeWarp mappingAlpha;
        private ByteTimeWarp mappingOmega;
        private ByteTimeWarpCollection byteMappings;
        private long length;
        private long position;
        private CropStream cropStream;
        private ResamplingStream resamplingStream;
        private ResamplingQuality resamplingQuality;

        public TimeWarpStream(IAudioStream sourceStream, ResamplingQuality quality)
            : base(sourceStream) {
                mappingAlpha = new ByteTimeWarp { From = 0, To = 0 };
                mappingOmega = new ByteTimeWarp { From = sourceStream.Length, To = sourceStream.Length };
                byteMappings = new ByteTimeWarpCollection(0);
                Mappings = new TimeWarpCollection();
                length = sourceStream.Length;
                position = sourceStream.Position;
                resamplingQuality = quality;

                ResetStream();
        }

        public TimeWarpStream(IAudioStream sourceStream, ResamplingQuality quality, TimeWarpCollection mappings)
            : this(sourceStream, quality) {
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
            if (mappings.Count > 0) {
                // Convert time mappings to byte mappings
                // This is the place where the TimeWarps and ByteTimeWarps are kept in sync!!
                byteMappings = new ByteTimeWarpCollection(mappings.Count);
                foreach (TimeWarp mapping in mappings) {
                    byteMappings.Add(ByteTimeWarp.Convert(mapping, Properties));
                }

                ByteTimeWarp last = byteMappings.Last();
                if (last.From < length) {
                    mappingOmega = new ByteTimeWarp { From = length, To = byteMappings.TranslateSourceToWarpedPosition(length) };
                }
                else {
                    mappingOmega = byteMappings.Last();
                }
            }
            else if (e != null && e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset) {
                mappingOmega = new ByteTimeWarp { From = sourceStream.Length, To = sourceStream.Length };
            }
            if (position > mappingOmega.To) {
                Position = mappingOmega.To;
            }
            ResetStream();
        }

        private void GetBoundingMappingsForWarpedPosition(long warpedPosition,
                out ByteTimeWarp lowerMapping, out ByteTimeWarp upperMapping) {
            byteMappings.GetBoundingMappingsForWarpedPosition(warpedPosition, out lowerMapping, out upperMapping);
            lowerMapping = lowerMapping ?? mappingAlpha;
            upperMapping = upperMapping ?? mappingOmega;
        }

        private void ResetStream() {
            if (position > mappingOmega.To) {
                throw new Exception("position beyond length");
            }

            length = mappingOmega.To;

            ByteTimeWarp mL, mH;
            GetBoundingMappingsForWarpedPosition(position, out mL, out mH);

            if (cropStream == null || cropStream.Begin != mL.From || cropStream.End != mH.From || resamplingStream.Length != mappingOmega.To) {
                // mapping has changed, stream subsection must be renewed
                cropStream = new CropStream(sourceStream, mL.From, mH.From);
                resamplingStream = new ResamplingStream(cropStream, resamplingQuality, ByteTimeWarp.CalculateSampleRateRatio(mL, mH));
                resamplingStream.Position = position - mL.To;
            }
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

                ByteTimeWarp mL, mH;
                GetBoundingMappingsForWarpedPosition(value, out mL, out mH);

                if (position < mL.To || position >= mH.To) {
                    // new stream subsection required
                    cropStream = new CropStream(sourceStream, mL.From, mH.From);
                    resamplingStream = new ResamplingStream(cropStream, resamplingQuality, ByteTimeWarp.CalculateSampleRateRatio(mL, mH));
                }

                resamplingStream.Position = value - mL.To;
                position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count) {
            int bytesRead = resamplingStream.Read(buffer, offset, count);

            if (bytesRead == 0 && cropStream.End != sourceStream.Length) {
                //PrintDebugStatus();
                ResetStream(); // switch to next subsection
                return this.Read(buffer, offset, count);
            }

            position += bytesRead;
            return bytesRead;
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

            public ByteTimeWarpCollection(int size) : base(size) { }

            public void GetBoundingMappingsForSourcePosition(long sourcePosition,
                out ByteTimeWarp lowerMapping, out ByteTimeWarp upperMapping) {
                lowerMapping = null;
                upperMapping = null;
                for (int x = 0; x < Count; x++) {
                    if (sourcePosition < this[x].From) {
                        if (x == 0) {
                            upperMapping = this[x];
                            break;
                        }
                        else {
                            lowerMapping = this[x - 1];
                            upperMapping = this[x];
                            break;
                        }
                    }
                    else if (x == Count - 1) {
                        lowerMapping = this[x];
                        break;
                    }
                }
            }

            public void GetBoundingMappingsForWarpedPosition(long warpedPosition,
                    out ByteTimeWarp lowerMapping, out ByteTimeWarp upperMapping) {
                lowerMapping = null;
                upperMapping = null;
                for (int x = 0; x < Count; x++) {
                    if (warpedPosition < this[x].To) {
                        if (x == 0) {
                            upperMapping = this[x];
                            break;
                        }
                        else {
                            lowerMapping = this[x - 1];
                            upperMapping = this[x];
                            break;
                        }
                    }
                    else if (x == Count - 1) {
                        lowerMapping = this[x];
                        break;
                    }
                }
            }

            public long TranslateSourceToWarpedPosition(long sourcePosition) {
                ByteTimeWarp lowerMapping;
                ByteTimeWarp upperMapping;
                GetBoundingMappingsForSourcePosition(sourcePosition, out lowerMapping, out upperMapping);

                if (lowerMapping == null) {
                    // position is before the first mapping -> linear adjust
                    return sourcePosition + upperMapping.Offset;
                }
                else if (upperMapping == null) {
                    // position is after the last mapping -> linear adjust
                    return sourcePosition + lowerMapping.Offset;
                }
                else {
                    return lowerMapping.To +
                        (long)((sourcePosition - lowerMapping.From) *
                        ByteTimeWarp.CalculateSampleRateRatio(lowerMapping, upperMapping));
                }
            }
        }
    }
}
