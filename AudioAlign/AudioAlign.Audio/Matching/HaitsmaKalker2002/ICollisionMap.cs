using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.HaitsmaKalker2002 {
    public interface ICollisionMap<K, V> {
        void Add(K key, V value);
        List<K> GetCollidingKeys();
        List<V> GetValues(K key);
    }

    public interface IFingerprintCollisionMap : ICollisionMap<SubFingerprint, SubFingerprintLookupEntry> {
    }
}
