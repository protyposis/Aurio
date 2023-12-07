using Aurio.FFT;

namespace Aurio.FftSharp
{
    /// <summary>
    /// Access layer to the FftSharp library.
    ///
    /// This implementation has two performance penalties:
    ///
    /// - It copies and transforms the input and output data to adjust Aurio's real
    /// float data to FftSharp's required double complex numbers. This also results
    /// in losing the in-place transformation ability of FftSharp.
    /// ability of FftSharp is thus lost.
    /// - It calculates the FFT on input for size*2 to simulate a real-symmetric transform
    /// and get the expected output resolution. FftSharp also exposes `ForwardReal`/`InverseReal`,
    /// however, without in-place support.
    /// </summary>
    public class FFT : IFFT
    {
        private readonly System.Numerics.Complex[] buffer;

        public FFT(int size)
        {
            Size = size;
            buffer = new System.Numerics.Complex[size * 2];
        }

        public int Size { get; private set; }

        public bool InPlace => false;

        public void Backward(float[] inPlaceBuffer)
        {
            ToBuffer(inPlaceBuffer);
            global::FftSharp.FFT.Inverse(buffer);
            FromBuffer(inPlaceBuffer);
        }

        public void Backward(float[] input, float[] output)
        {
            ToBuffer(input);
            global::FftSharp.FFT.Inverse(buffer);
            FromBuffer(output);
        }

        public void Dispose() { }

        public void Forward(float[] inPlaceBuffer)
        {
            ToBuffer(inPlaceBuffer);
            global::FftSharp.FFT.Forward(buffer);
            FromBuffer(inPlaceBuffer);
        }

        public void Forward(float[] input, float[] output)
        {
            ToBuffer(input);
            global::FftSharp.FFT.Forward(buffer);
            FromBuffer(output);
        }

        private void ToBuffer(float[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                buffer[i] = new(data[i], 0);
            }
        }

        private void FromBuffer(float[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (float)buffer[i].Real;
            }
        }
    }
}
