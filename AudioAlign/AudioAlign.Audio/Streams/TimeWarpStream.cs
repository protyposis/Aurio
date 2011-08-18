using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace AudioAlign.Audio.Streams {
    public class TimeWarpStream : AbstractAudioStreamWrapper {

        private Mapping mappingAlpha;
        private Mapping mappingOmega;
        private List<Mapping> mappings;
        private long length;
        private long position;
        private CropStream cropStream;
        private ResamplingStream resamplingStream;
        private ResamplingQuality resamplingQuality;

        public TimeWarpStream(IAudioStream sourceStream, ResamplingQuality quality)
            : base(sourceStream) {
                mappingAlpha = new Mapping { From = 0, To = 0 };
                mappingOmega = new Mapping { From = sourceStream.Length, To = sourceStream.Length };
                mappings = new List<Mapping>();
                length = sourceStream.Length;
                position = sourceStream.Position;
                resamplingQuality = quality;

                ResetStream();
        }

        public void ClearMappings() {
            mappings.Clear();
            ResetStream();
        }

        public void AddMapping(Mapping mapping) {
            mappings.Add(mapping);
            SortAndValidateMappings();
            if (mappings.Last().From < length) {
                mappingOmega = new Mapping { From = length, To = length };
            }
            else {
                mappingOmega = mappings.Last();
            }
            ResetStream();
        }

        private void SortAndValidateMappings() {
            mappings = new List<Mapping>(mappings.OrderBy(mapping => mapping.From));
            // validate that no mapping is overlapping with another one
            for (int x = 0; x < mappings.Count - 1; x++) {
                for (int y = x + 1; y < mappings.Count; y++) {
                    if (mappings[x].To > mappings[y].To) {
                        throw new Exception(mappings[x] + " is overlapping " + mappings[y]);
                    }
                    else if (!resamplingStream.CheckSampleRateRatio(CalculateSampleRateRatio(mappings[x], mappings[y]))) {
                        throw new Exception("invalid sample ratio");
                    }
                }
            }
        }

        private void GetBoundingMappingsForWarpedPosition(long warpedPosition,
                out Mapping lowerMapping, out Mapping upperMapping) {
            lowerMapping = mappingAlpha;
            upperMapping = mappingOmega;
            for (int x = 0; x < mappings.Count; x++) {
                if (warpedPosition < mappings[x].To) {
                    lowerMapping = x == 0 ? mappingAlpha : mappings[x - 1];
                    upperMapping = mappings[x];
                    break;
                }
                else if (x == mappings.Count - 1) {
                    lowerMapping = mappings[x];
                    upperMapping = mappingOmega;
                }
            }
        }

        private void ResetStream() {
            if (position > mappingOmega.To) {
                throw new Exception("position beyond length");
            }

            length = mappingOmega.To;

            Mapping mL, mH;
            GetBoundingMappingsForWarpedPosition(position, out mL, out mH);

            if (cropStream == null || cropStream.Begin != mL.From || cropStream.End != mH.From || resamplingStream.Length != mappingOmega.To) {
                // mapping has changed, stream subsection must be renewed
                cropStream = new CropStream(sourceStream, mL.From, mH.From);
                resamplingStream = new ResamplingStream(cropStream, resamplingQuality, CalculateSampleRateRatio(mL, mH));
                resamplingStream.Position = position - mL.To;
            }
        }

        private double CalculateSampleRateRatio(Mapping mL, Mapping mH) {
            return (mH.To - mL.To) / (double)(mH.From - mL.From);
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

                Mapping mL, mH;
                GetBoundingMappingsForWarpedPosition(value, out mL, out mH);

                if (position < mL.To || position >= mH.To) {
                    // new stream subsection required
                    cropStream = new CropStream(sourceStream, mL.From, mH.From);
                    resamplingStream = new ResamplingStream(cropStream, resamplingQuality, CalculateSampleRateRatio(mL, mH));
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

    /// <summary>
    /// Maps a position of a linear source stream to a position in a time warped target stream.
    /// </summary>
    public class Mapping {

        /// <summary>
        /// The position in the unwarped source stream.
        /// </summary>
        public long From { get; set; }

        /// <summary>
        /// The warped position in the target stream.
        /// </summary>
        public long To { get; set; }

        /// <summary>
        /// The position difference between the source and target stream.
        /// </summary>
        public long Offset {
            get { return To - From; }
        }

        public override string ToString() {
            return String.Format("Mapping({0} -> {1})", From, To);
        }
    }
}
