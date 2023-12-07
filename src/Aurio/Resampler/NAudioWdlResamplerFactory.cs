namespace Aurio.Resampler
{
    public class NAudioWdlResamplerFactory : IResamplerFactory
    {
        public IResampler CreateInstance(
            ResamplingQuality quality,
            int channels,
            double sampleRateRatio
        )
        {
            return new NAudioWdlResampler(quality, channels, sampleRateRatio);
        }

        public bool CheckRatio(double sampleRateRatio)
        {
            return true;
        }
    }
}
