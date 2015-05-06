using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioAlign.Audio.Matching.Chromaprint {
    public class Fingerprint : IEnumerable<SubFingerprintHash> {

        private List<SubFingerprintHash> subFingerprintList;
        private int offset;
        private int length;

        public Fingerprint(List<SubFingerprintHash> subFingerprintList, int index, int length) {
            if (index < 0 || index >= subFingerprintList.Count || index + length < 0 || index + length > subFingerprintList.Count) {
                throw new ArgumentException();
            }

            this.subFingerprintList = subFingerprintList;
            this.offset = index;
            this.length = length;
        }

        public SubFingerprintHash this[int index] {
            get {
                if (index < 0 || index >= this.length) {
                    throw new IndexOutOfRangeException();
                }
                return subFingerprintList[this.offset + index];
            }
        }

        public int Length {
            get { return this.length; }
        }

        public Fingerprint Difference(Fingerprint fp) {
            List<SubFingerprintHash> sfpDiffs = new List<SubFingerprintHash>();
            for (int x = 0; x < Length; x++) {
                sfpDiffs.Add(this[x].Difference(fp[x]));
            }
            return new Fingerprint(sfpDiffs, 0, Length);;
        }

        public IEnumerator<SubFingerprintHash> GetEnumerator() {
            for (int i = this.offset; i < this.offset + this.length; i++) {
                yield return subFingerprintList[i];
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public static float CalculateBER(Fingerprint fp1, Fingerprint fp2) {
            if (fp1.Length != fp2.Length) {
                throw new ArgumentException("cannot compare fingerprints of different lengths");
            }

            uint bitErrors = 0;

            // sum up the bit errors
            for (int s = 0; s < fp1.Length; s++) {
                bitErrors += fp1[s].HammingDistance(fp2[s]);
            }

            return bitErrors / (float)(fp1.Length * 32); // sub-fingerprints * 32 bits
        }
    }
}
