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
                SortAndValidateMappings();
        }

        public void ClearMappings() {
            mappings.Clear();
            UpdateLengthAndPosition();
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
            UpdateLengthAndPosition();
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
                    else if (!resamplingStream.CheckSampleRateRatio(
                        (mappings[y].To - mappings[x].To) / ((double)mappings[y].From - mappings[x].From))) {
                        throw new Exception("invalid sample ratio");
                    }
                }
            }
        }

        private void GetBoundingMappingsForSourcePosition(long sourcePosition,
                out Mapping lowerMapping, out Mapping upperMapping) {
            lowerMapping = mappingAlpha;
            upperMapping = mappingOmega;
            for (int x = 0; x < mappings.Count; x++) {
                if (sourcePosition < mappings[x].From) {
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

        public long CalculateWarpedPosition(long sourcePosition) {
            Mapping m1 = null;
            Mapping m2 = null;
            GetBoundingMappingsForSourcePosition(sourcePosition, out m1, out m2);

            if (m1.Offset != m2.Offset) {
                return (long)(m1.To + (sourcePosition - m1.From) * 
                    ((double)(m2.To - m1.To) / (m2.From - m1.From)));
            }
            else {
                // offsets are equal
                return sourcePosition - m1.From + m1.To;
            }
        }

        public long CalculateSourcePosition(long warpedPosition) {
            Mapping m1 = null;
            Mapping m2 = null;
            GetBoundingMappingsForWarpedPosition(warpedPosition, out m1, out m2);

            if (m1.Offset != m2.Offset) {
                return (long)(m1.From + (warpedPosition - m1.To) / 
                    ((double)(m2.To - m1.To) / (m2.From - m1.From)));
            }
            else {
                // offsets are equal
                return warpedPosition - m1.To + m1.From;
            }
        }

        private void UpdateLengthAndPosition() {
            length = CalculateWarpedPosition(sourceStream.Length);
        }

        private bool ResetStream() {
            return ResetStream(sourceStream.Position);
        }

        private bool ResetStream(long sourcePosition) {
            Mapping m1 = null, m2 = null;
            double sampleRateRatio = 1;

            GetBoundingMappingsForSourcePosition(sourcePosition, out m1, out m2);
            if (m1 != null) {
                if (cropStream != null && m1.From == m2.From && m2.From == cropStream.End) {
                    // end of stream has been reached
                    // return false to indicate that no new data section is following
                    return false;
                }
                cropStream = new CropStream(sourceStream, m1.From, m2.From);
                sampleRateRatio = (m2.To - m1.To) / (double)(m2.From - m1.From);
            }
            else {
                cropStream = new CropStream(sourceStream, 0, length);
            }
            resamplingStream = new ResamplingStream(cropStream, resamplingQuality, sampleRateRatio);

            return true;
        }

        public override long Length {
            get { return length; }
        }

        public override long Position {
            get { return position; }
            set {
                PrintDebugStatus();
                position = value;
                //sourceStream.Position = Math.Min(
                //    CalculateSourcePosition(value) + resamplingStream.BufferedBytes, 
                //    sourceStream.Length);
                //PrintDebugStatus();
                ResetStream(Math.Min(
                    CalculateSourcePosition(value) + resamplingStream.BufferedBytes, 
                    sourceStream.Length));
                PrintDebugStatus();
                Mapping m1, m2;
                GetBoundingMappingsForWarpedPosition(value, out m1, out m2);
                resamplingStream.Position = value - m1.To;
                PrintDebugStatus();
            }
        }

        //public IAudioStream SourceStream { get { return sourceStream; } }
        //public int BufferedBytes { get { return resamplingStream.BufferedBytes; } }

        public override int Read(byte[] buffer, int offset, int count) {
            int bytesRead = resamplingStream.Read(buffer, offset, count);
            if (bytesRead == 0) {
                Debug.WriteLine("SRC buffer: " + resamplingStream.BufferedBytes);
                if (ResetStream()) {
                    return this.Read(buffer, offset, count);
                }
                PrintDebugStatus();
            }
            position += bytesRead;
            return bytesRead;
        }

        private void PrintDebugStatus() {
            Debug.WriteLine("TimeWarpStream len {0,10}, pos {1,10}", length, position);
            Debug.WriteLine("     resampler len {0,10}, pos {1,10} src buffer {2,4}", resamplingStream.Length, resamplingStream.Position, resamplingStream.BufferedBytes);
            Debug.WriteLine("          crop len {0,10}, pos {1,10}", cropStream.Length, cropStream.Position);
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
