using System.Diagnostics;
using Aurio.FFT;

var factories = new IFFTFactory[]
{
    new Aurio.Exocortex.FFTFactory(),
    new Aurio.FftSharp.FFTFactory(),
    new Aurio.PFFFT.FFTFactory(),
    new Aurio.FFTW.FFTFactory(),
};
var sizes = new int[] { 256, 1024, 2048, 4096 };
int samples = 10000;

foreach (var factory in factories)
{
    FFTFactory.Factory = factory;

    foreach (var size in sizes)
    {
        var fft = FFTFactory.CreateInstance(size);
        var input = new float[size];
        var output = new float[size];
        void logStat(string type, Stopwatch sw)
        {
            var msPerCall = (double)sw.ElapsedTicks / samples / TimeSpan.TicksPerMillisecond;
            Console.WriteLine(
                $"{factory.GetType().FullName} {size} {type}: {sw.ElapsedMilliseconds} ms total {msPerCall:0.#####} ms/call"
            );
        }

        // warmup
        for (var i = 0; i < 10; i++)
        {
            fft.Forward(input);
            fft.Forward(input, output);
        }

        // measure
        Stopwatch sw = Stopwatch.StartNew();

        for (var i = 0; i <= samples; i++)
        {
            fft.Forward(input);
        }

        sw.Stop();
        logStat("inplace", sw);

        sw.Restart();

        for (var i = 0; i <= samples; i++)
        {
            fft.Forward(input, output);
        }

        sw.Stop();
        logStat("", sw);
    }
}
