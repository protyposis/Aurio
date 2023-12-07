using Aurio.FFT;

namespace Aurio.FftSharp
{
    public class FFTFactory : IFFTFactory
    {
        public IFFT CreateInstance(int size)
        {
            return new FFT(size);
        }
    }
}
