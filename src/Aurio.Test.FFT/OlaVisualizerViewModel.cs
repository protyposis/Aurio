using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Aurio.FFT;
using Aurio.Features;
using Aurio.Streams;

namespace Aurio.Test.FFT
{
    class OlaVisualizerViewModel : INotifyPropertyChanged
    {
        private int windowSize;
        private int overlap;
        private int maxOverlap;
        private WindowType windowType;
        private bool windowSqrt;
        private VisualizingStream inputStream;
        private VisualizingStream outputOlaStream;
        private VisualizingStream outputWolaStream;
        private WindowFunctionViewModel windowFunctionViewModel;
        private long initialViewportWidth;
        private TimeSpan length;
        private float inputFrequency;

        public OlaVisualizerViewModel()
        {
            windowSize = 64;
            overlap = 32;
            maxOverlap = windowSize - 1;
            windowType = WindowType.HannPeriodic;
            windowFunctionViewModel = new WindowFunctionViewModel(
                WindowUtil.GetFunction(windowType, windowSize)
            );
            length = new TimeSpan(0, 0, 0, 1);
            inputFrequency = 1;

            FFTFactory.Factory = new PFFFT.FFTFactory();
            InitialViewportWidth = length.Ticks;

            Update();
            OnPropertyChanged();
        }

        public List<WindowType> WindowTypeValues
        {
            get => Enum.GetValues(typeof(WindowType)).Cast<WindowType>().ToList();
        }

        public WindowType WindowType
        {
            get => windowType;
            set
            {
                bool valueChanged = value != windowType;
                windowType = value;

                if (valueChanged)
                {
                    OnPropertyChanged();
                    Update();
                }
            }
        }

        public WindowFunctionViewModel WindowFunctionViewModel
        {
            get => windowFunctionViewModel;
            set
            {
                windowFunctionViewModel = value;
                OnPropertyChanged();
            }
        }

        public int WindowSize
        {
            get => windowSize;
            set
            {
                bool valueChanged = windowSize != value;
                windowSize = value;
                MaxOverlap = windowSize - 1;
                if (Overlap > MaxOverlap)
                {
                    Overlap = MaxOverlap;
                }

                if (valueChanged)
                {
                    OnPropertyChanged();
                    Update();
                }
            }
        }

        public float InputFrequency
        {
            get => inputFrequency;
            set
            {
                bool valueChanged = inputFrequency != value;
                inputFrequency = value;

                if (valueChanged)
                {
                    OnPropertyChanged();
                    Update();
                }
            }
        }

        public bool WindowSqrt
        {
            get => windowSqrt;
            set
            {
                bool valueChanged = windowSqrt != value;
                windowSqrt = value;

                if (valueChanged)
                {
                    OnPropertyChanged();
                    Update();
                }
            }
        }

        public int Overlap
        {
            get => overlap;
            set
            {
                bool valueChanged = overlap != value;
                overlap = value;

                if (valueChanged)
                {
                    OnPropertyChanged();
                    Update();
                }
            }
        }

        public int MaxOverlap
        {
            get => maxOverlap;
            set
            {
                maxOverlap = value;
                OnPropertyChanged();
            }
        }

        public VisualizingStream InputStream
        {
            get => inputStream;
            set
            {
                inputStream = value;
                OnPropertyChanged();
            }
        }

        public VisualizingStream OutputOlaStream
        {
            get => outputOlaStream;
            set
            {
                outputOlaStream = value;
                OnPropertyChanged();
            }
        }

        public VisualizingStream OutputWolaStream
        {
            get => outputWolaStream;
            set
            {
                outputWolaStream = value;
                OnPropertyChanged();
            }
        }

        public long InitialViewportWidth
        {
            get => initialViewportWidth;
            set
            {
                initialViewportWidth = value;
                OnPropertyChanged();
            }
        }

        private async void Update()
        {
            var inputStream = new VolumeControlStream(
                new SineGeneratorStream(1000, inputFrequency, length)
            )
            {
                Volume = 0.5f
            };
            var olaStream = new MemoryWriterStream(inputStream.Properties);
            var windowConfig = new WindowConfig(windowType, windowSize, SquareRoot: windowSqrt);
            var windowFunction = WindowUtil.GetFunction(windowConfig);
            var hopSize = windowSize - overlap;
            var sw = new StreamWindower(inputStream, windowFunction, hopSize);
            var ola = new OLA(olaStream, windowSize, hopSize);

            var frameBuffer = new float[windowSize];
            while (sw.HasNext())
            {
                sw.ReadFrame(frameBuffer);
                ola.WriteFrame(frameBuffer);
            }
            ola.Flush();

            inputStream.Position = 0;
            var wolaStream = new MemoryWriterStream(inputStream.Properties);
            var stft = new STFT(
                inputStream,
                windowFunction,
                hopSize,
                WindowSize,
                STFT.OutputFormat.Raw
            );
            var istft = new InverseSTFT(wolaStream, windowFunction, hopSize, WindowSize);
            float[] fftResult = new float[WindowSize];
            while (stft.HasNext())
            {
                stft.ReadFrame(fftResult);
                istft.WriteFrame(fftResult);
            }
            istft.Flush();

            inputStream.Position = 0;
            olaStream.Position = 0;
            wolaStream.Position = 0;

            InputStream = await VisualizingStream.Create(inputStream);
            OutputOlaStream = await VisualizingStream.Create(olaStream);
            OutputWolaStream = await VisualizingStream.Create(wolaStream);
            WindowFunctionViewModel = new WindowFunctionViewModel(windowFunction);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
