using System.Diagnostics;
using System.Linq;

namespace Aurio.Test.FFT
{
    public class WindowFunctionViewModel
    {
        public WindowFunctionViewModel(WindowFunction window)
        {
            WindowType = window.Type;
            SampleCount = window.Size;
            Name = window.Type.ToString();

            Debug.WriteLine($"Creating window model for {WindowType} of length {SampleCount}");

            float[] samples = new float[SampleCount];
            samples = samples.Select(sample => 1f).ToArray();
            window.Apply(samples);
            Samples = samples;

            if (samples.Length <= 8)
            {
                CriticalValues = string.Join(
                    "; ",
                    samples.Select(value => value.ToString("0.0000"))
                );
            }
            else
            {
                var startValues = samples.Take(2);
                var middleValues = samples.Skip(samples.Length / 2 - 2).Take(4);
                var endValues = samples.Skip(samples.Length - 2).Take(2);
                CriticalValues = string.Join(
                    "; ",
                    startValues
                        .Concat(middleValues)
                        .Concat(endValues)
                        .Select(value => value.ToString("0.0000"))
                );
            }
        }

        public WindowType WindowType { get; }
        public int SampleCount { get; }
        public string Name { get; }
        public float[] Samples { get; }
        public string CriticalValues { get; }
    }
}
