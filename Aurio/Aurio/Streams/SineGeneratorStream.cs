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

namespace Aurio.Streams
{
    public class SineGeneratorStream : IAudioStream
    {

        private AudioProperties properties;
        private float frequency;
        private long length;
        private long position;

        public SineGeneratorStream(int sampleRate, float frequency, TimeSpan length)
        {
            this.properties = new AudioProperties(1, sampleRate, 32, AudioFormat.IEEE);
            this.frequency = frequency;
            this.length = TimeUtil.TimeSpanToBytes(length, properties);
        }

        public AudioProperties Properties
        {
            get { return properties; }
        }

        public long Length
        {
            get { return length; }
        }

        public long Position
        {
            get { return position; }
            set { position = value; }
        }

        public int SampleBlockSize
        {
            get { return properties.SampleBlockByteSize; }
        }

        public float Frequency
        {
            get { return frequency; }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            count = Math.Min((int)(Length - Position) / 4, count);
            float frequencyFactor = Properties.SampleRate / frequency;
            long samplePosition = position / 4;
            for (int x = 0; x < count; x++)
            {
                buffer[offset + x] = (float)Math.Sin((samplePosition + x) / frequencyFactor * Math.PI * 2);
            }
            position += count * 4;
            return count;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            if (count % SampleBlockSize != 0)
            {
                throw new Exception("count is not aligned to the sample block size");
            }
            AudioBuffer audioBuffer = new AudioBuffer(buffer);
            return Read(audioBuffer.FloatData, offset / 4, count / 4) * 4;
        }

        public void Close()
        {
            // nothing to release
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Close();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
