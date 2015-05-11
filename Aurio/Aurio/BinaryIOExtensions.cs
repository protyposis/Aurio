using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Aurio {
    public static class BinaryIOExtensions {

        public static void Write(this BinaryWriter bw, Peak peak) {
            bw.Write(peak.Min);
            bw.Write(peak.Max);
        }

        public static Peak ReadPeak(this BinaryReader br) {
            return new Peak(br.ReadSingle(), br.ReadSingle());
        }

        public static BinaryReader[] WrapWithBinaryReaders(this MemoryStream[] memoryStreams) {
            BinaryReader[] readers = new BinaryReader[memoryStreams.Length];
            for (int x = 0; x < memoryStreams.Length; x++) {
                readers[x] = new BinaryReader(memoryStreams[x]);
            }
            return readers;
        }

        public static BinaryWriter[] WrapWithBinaryWriters(this MemoryStream[] memoryStreams) {
            BinaryWriter[] writers = new BinaryWriter[memoryStreams.Length];
            for (int x = 0; x < memoryStreams.Length; x++) {
                writers[x] = new BinaryWriter(memoryStreams[x]);
            }
            return writers;
        }
    }
}
