using SignalProcessing.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalGenerator.App
{
    public static class Tools
    {
        /// <summary>
        /// Генерирует сигнал с синусоидой.
        /// </summary>
        /// <param name="length">Длина фрагмента генерируемого сигнала (с).</param>
        /// <param name="frequency">Частота генерируемого сигнала (Гц).</param>
        /// <param name="period">Период синусоиды (с).</param>
        /// <param name="phase">Фаза синусоиды (с).</param>
        /// <returns>Сигнал с синусоидой.</returns>
        public static SingleChannelSignal GetSinSignal(float length, float frequency, float period, float phase)
        {
            int cnt = (int)(length * frequency + 0.5);
            float delta = 1 / frequency;
            float[] y = new float[cnt];
            for (int i = 0; i < cnt; i++)
            {
                y[i] = (float)Math.Sin((i * delta - phase) / period * 2 * Math.PI);
            }

            return new SingleChannelSignal(y, frequency);
        }

        /// <summary>
        /// Генерирует сигнал с прямой линией, задаваемой уравнением y = kx + b.
        /// </summary>
        /// <param name="length">Длина фрагмента генерируемого сигнала (с).</param>
        /// <param name="frequency">Частота генерируемого сигнала (Гц).</param>
        /// <param name="k">Коэффициент k в уравнении прямой.</param>
        /// <param name="b">Коэффициент b в уравнении прямой.</param>
        /// <returns>Сигнал с прямой линией.</returns>
        public static SingleChannelSignal GetLineSignal(float length, float frequency, float k, float b)
        {
            int cnt = (int)(length * frequency + 0.5);
            float delta = 1 / frequency;
            float[] y = new float[cnt];
            for (int i = 0; i < cnt; i++)
            {
                y[i] = k * i * delta + b;
            }

            return new SingleChannelSignal(y, frequency);
        }

        /// <summary>
        /// Генерирует сигнал с белым шумом.
        /// </summary>
        /// <param name="length">Длина фрагмента генерируемого сигнала (с).</param>
        /// <param name="frequency">Частота генерируемого сигнала (Гц).</param>
        /// <param name="amplitude">Амплитуда генерируемого шума.</param>
        /// <returns>Сигнал с белым шумом.</returns>
        public static SingleChannelSignal GetNoiseSignal(float length, float frequency, float amplitude)
        {
            int cnt = (int)(length * frequency + 0.5);
            Random rand = new Random();
            float[] y = Enumerable.Range(0, cnt)
                .Select(_ => (float)((rand.NextDouble() - 0.5) * amplitude))
                .ToArray();
            
            return new SingleChannelSignal(y, frequency);
        }

        /// <summary>
        /// Вычисляет сигнал с производной.
        /// </summary>
        /// <param name="signal">Исходный сигнал.</param>
        /// <returns>Сигнал с производной.</returns>
        public static SingleChannelSignal GetDerivativeSignal(SingleChannelSignal signal)
        {
            int length = signal.Data.Length;
            float[] srcData = signal.Data;
            float frequency = signal.Frequency;
            float[] derivative = new float[length];
            for (int i = 0; i < length - 1; i++)
            {
                derivative[i] = (srcData[i + 1] - srcData[i]) * frequency;
            }
            derivative[length - 1] = derivative[length - 2];

            return new SingleChannelSignal(derivative, signal.Frequency);
        }

        /// <summary>
        /// Фильтрует сигнал усреднением окном.
        /// </summary>
        /// <param name="signal">Исходный сигнал.</param>
        /// <param name="windowSize">Размер окна для усреднения (с).</param>
        /// <returns>Фильтрованный сигнал.</returns>
        public static SingleChannelSignal GetFilteredSignal(SingleChannelSignal signal, float windowSize)
        {
            int length = signal.Data.Length;
            float[] srcData = signal.Data;
            float frequency = signal.Frequency;
            float[] filteredData = OfflineSingleChannelFilter.Filter(srcData,
                (int)(windowSize * frequency + 0.5));

            return new SingleChannelSignal(filteredData, signal.Frequency);
        }

        /// <summary>
        /// Вычисляет сумму сигналов. Все суммируемые сигналы должны иметь одинаковую частоту.
        /// </summary>
        /// <param name="signals">Суммируемые сигналы.</param>
        /// <returns>Сумма сигналов.</returns>
        public static SingleChannelSignal SumSignals(List<SingleChannelSignal> signals)
        {
            int count = signals.Count;
            if (count <= 0)
                return null;

            float frequency = signals.First().Frequency;
            for (int i = 1; i < count; i++)
            {
                if (Math.Abs(signals[i].Frequency - frequency) > 1e-5)
                {
                    return null;
                }
            }

            int length = signals.Select(s => s.Data.Length).Max();
            float[] resData = new float[length];
            for (int signalInd = 0; signalInd < count; signalInd++)
            {
                float[] data = signals[signalInd].Data;
                for (int sampleInd = 0; sampleInd < signals[signalInd].Data.Length; sampleInd++)
                {
                    resData[sampleInd] += data[sampleInd];
                }
            }

            return new SingleChannelSignal(resData, frequency);
        }

        /// <summary>
        /// Склеивает набор сигналов в один.
        /// </summary>
        /// <param name="signals">Набор склеиваемых сигналов.</param>
        /// <returns>Склеенный сигнал.</returns>
        public static SingleChannelSignal ConcatSignals(List<SingleChannelSignal> signals)
        {
            int count = signals.Count;
            if (count <= 0)
                return null;

            float frequency = signals.First().Frequency;
            for (int i = 1; i < count; i++)
            {
                if (Math.Abs(signals[i].Frequency - frequency) > 1e-5)
                {
                    return null;
                }
            }

            IEnumerable<float> resData = new float[0];
            for (int signalInd = 0; signalInd < count; signalInd++)
            {
                resData = resData.Concat(signals[signalInd].Data);
            }

            return new SingleChannelSignal(resData.ToArray(), frequency);
        }

        public static SingleChannelSignal ConcatSignals(List<SingleChannelSignal> signals,
            float windowTime)
        {
            int count = signals.Count;
            if (count <= 0)
                return null;

            float frequency = signals.First().Frequency;
            for (int i = 1; i < count; i++)
            {
                if (Math.Abs(signals[i].Frequency - frequency) > 1e-5)
                {
                    return null;
                }
            }

            int windowLength = (int)(frequency * windowTime + 0.5);
            float[] cosWindow = Enumerable.Range(0, windowLength)
                .Select(i => (float)Math.Cos(i / Math.PI * 2))
                .ToArray();

            IEnumerable<float> resData = new float[0];
            for (int signalInd = 0; signalInd < count; signalInd++)
            {
                resData = resData.Concat(signals[signalInd].Data);
            }

            return new SingleChannelSignal(resData.ToArray(), frequency);
        }

    }
}
