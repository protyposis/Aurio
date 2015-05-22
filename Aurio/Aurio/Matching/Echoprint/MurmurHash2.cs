// 
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2015  Mario Guggenberger <mg@protyposis.net>
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

namespace Aurio.Matching.Echoprint {
    /// <summary>
    /// MurmurHash2, by Austin Appleby
    /// http://sites.google.com/site/murmurhash
    /// 
    /// Note - This code makes a few assumptions about how your machine behaves -
    /// 
    /// 1. We can read a 4-byte value from any address without crashing
    /// 2. sizeof(int) == 4
    ///
    /// And it has a few limitations -
    ///
    /// 1. It will not work incrementally.
    /// 2. It will not produce the same results on little-endian and big-endian
    ///    machines.
    /// </summary>
    public class MurmurHash2 {

        // 'm' and 'r' are mixing constants generated offline.
        // They're not really 'magic', they just happen to work well.
        private const uint m = 0x5bd1e995;
        private const int r = 24;

        public static unsafe uint Hash(byte* key, int len, uint seed) {

            // Initialize the hash to a 'random' value

            uint h = seed ^ (uint)len;

            // Mix 4 bytes at a time into the hash

            byte* data = key;

            while (len >= 4) {
                uint k = *(uint*)data;

                k *= m;
                k ^= k >> r;
                k *= m;

                h *= m;
                h ^= k;

                data += 4;
                len -= 4;
            }

            // Handle the last few bytes of the input array

            while (len > 0) { // C# does not support falling through cases, so we need to loop
                switch (len) {
                    case 3:
                        h ^= (uint)(data[2] << 16);
                        break;
                    case 2:
                        h ^= (uint)(data[1] << 8);
                        break;
                    case 1:
                        h ^= (uint)data[0]; h *= m;
                        break;
                }
                len--;
            }

            // Do a few final mixes of the hash to ensure the last few
            // bytes are well-incorporated.

            h ^= h >> 13;
            h *= m;
            h ^= h >> 15;

            return h;
        }

        public static uint Hash(byte[] key, uint seed) {
            unsafe {
                fixed (byte* keyPtr = key) {
                    return Hash(keyPtr, key.Length, seed);
                }
            }
        }

        public static uint Hash(int[] key, uint seed) {
            unsafe {
                fixed (int* keyPtr = key) {
                    return Hash((byte*)keyPtr, key.Length * 4, seed);
                }
            }
        }
    }
}
