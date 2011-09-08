using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace AudioAlign.Audio.Streams {
    public class TimeWarpStream : AbstractAudioStreamWrapper {

        private TimeWarp mappingAlpha;
        private TimeWarp mappingOmega;
        private TimeWarpCollection mappings;
        private long length;
        private long position;
        private CropStream cropStream;
        private ResamplingStream resamplingStream;
        private ResamplingQuality resamplingQuality;

        public TimeWarpStream(IAudioStream sourceStream, ResamplingQuality quality)
            : base(sourceStream) {
                mappingAlpha = new TimeWarp { From = 0, To = 0 };
                mappingOmega = new TimeWarp { From = sourceStream.Length, To = sourceStream.Length };
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
                TimeWarp last = mappings.Last();
                if (last.From < length) {
                    mappingOmega = new TimeWarp { From = length, To = mappings.TranslateSourceToWarpedPosition(length) };
                }
                else {
                    mappingOmega = mappings.Last();
                }
            }
            ResetStream();
        }

        private void GetBoundingMappingsForWarpedPosition(long warpedPosition,
                out TimeWarp lowerMapping, out TimeWarp upperMapping) {
            mappings.GetBoundingMappingsForWarpedPosition(warpedPosition, out lowerMapping, out upperMapping);
            lowerMapping = lowerMapping ?? mappingAlpha;
            upperMapping = upperMapping ?? mappingOmega;
        }

        private void ResetStream() {
            if (position > mappingOmega.To) {
                throw new Exception("position beyond length");
            }

            length = mappingOmega.To;

            TimeWarp mL, mH;
            GetBoundingMappingsForWarpedPosition(position, out mL, out mH);

            if (cropStream == null || cropStream.Begin != mL.From || cropStream.End != mH.From || resamplingStream.Length != mappingOmega.To) {
                // mapping has changed, stream subsection must be renewed
                cropStream = new CropStream(sourceStream, mL.From, mH.From);
                resamplingStream = new ResamplingStream(cropStream, resamplingQuality, TimeWarp.CalculateSampleRateRatio(mL, mH));
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

                TimeWarp mL, mH;
                GetBoundingMappingsForWarpedPosition(value, out mL, out mH);

                if (position < mL.To || position >= mH.To) {
                    // new stream subsection required
                    cropStream = new CropStream(sourceStream, mL.From, mH.From);
                    resamplingStream = new ResamplingStream(cropStream, resamplingQuality, TimeWarp.CalculateSampleRateRatio(mL, mH));
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
    }
}
