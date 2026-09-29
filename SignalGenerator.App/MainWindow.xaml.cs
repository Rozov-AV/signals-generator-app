using Microsoft.Win32;
using ScottPlot;
using ScottPlot.Plottables;
using SignalProcessing.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SignalGenerator.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Поля и свойства

        private List<SingleChannelSignal> _signalsList;

        #endregion

        #region Конструкторы

        public MainWindow()
        {
            InitializeComponent();
            Init();
        }

        #endregion

        #region Открытые методы

        #endregion

        #region Внутренние методы

        private void Init()
        {
            _signalsList = new List<SingleChannelSignal>();
        }

        private void ResetPlot()
        {
            _signalsList.Clear();
            WpfPlot1.Plot.Clear();
            WpfPlot1.Refresh();
        }

        private void AddSignal(SingleChannelSignal signal)
        {
            _signalsList.Add(signal);
            WpfPlot1.Plot.Add.ScatterLine(signal.TimeTicks, signal.Data);
            WpfPlot1.Plot.Axes.AutoScale();
            WpfPlot1.Refresh();
        }

        private void SinButton_Click(object sender, RoutedEventArgs e)
        {
            float length = float.Parse(SignalLengthTextBox.Text, CultureInfo.InvariantCulture);
            float freq = float.Parse(SignalFrequencyTextBox.Text, CultureInfo.InvariantCulture);
            float period = float.Parse(SinPeriodTextBox.Text, CultureInfo.InvariantCulture);
            float phase = float.Parse(SinPhaseTextBox.Text, CultureInfo.InvariantCulture);
            SingleChannelSignal signal = Tools.GetSinSignal(length, freq, period, phase);

            AddSignal(signal);
        }

        private void LineButton_Click(object sender, RoutedEventArgs e)
        {
            float length = float.Parse(SignalLengthTextBox.Text, CultureInfo.InvariantCulture);
            float freq = float.Parse(SignalFrequencyTextBox.Text, CultureInfo.InvariantCulture);
            float k = float.Parse(KTextBox.Text, CultureInfo.InvariantCulture);
            float b = float.Parse(BTextBox.Text, CultureInfo.InvariantCulture);
            SingleChannelSignal signal = Tools.GetLineSignal(length, freq, k, b);

            AddSignal(signal);
        }

        private void NoiseButton_Click(object sender, RoutedEventArgs e)
        {
            float length = float.Parse(SignalLengthTextBox.Text, CultureInfo.InvariantCulture);
            float freq = float.Parse(SignalFrequencyTextBox.Text, CultureInfo.InvariantCulture);
            float amplitude = float.Parse(NoiseAmplitudeTextBox.Text, CultureInfo.InvariantCulture);
            SingleChannelSignal signal = Tools.GetNoiseSignal(length, freq, amplitude);

            AddSignal(signal);
        }

        private void SumButton_Click(object sender, RoutedEventArgs e)
        {
            SingleChannelSignal sumSignal = Tools.SumSignals(_signalsList);
            if (sumSignal == null)
            {
                MessageBox.Show("Суммирование невозможно!", "Ошибка");
                return;
            }

            AddSignal(sumSignal);
        }

        private void ConcatButton_Click(object sender, RoutedEventArgs e)
        {
            SingleChannelSignal resSignal = Tools.ConcatSignals(_signalsList);
            if (resSignal == null)
            {
                MessageBox.Show("Склейка невозможна!", "Ошибка");
                return;
            }

            AddSignal(resSignal);
        }

        private void DerivativeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_signalsList.Count < 1)
            {
                MessageBox.Show("Нет сигнала для вычисления производной!", "Ошибка");
                return;
            }

            AddSignal(Tools.GetDerivativeSignal(_signalsList.Last()));
        }

        private void FilterOfflineButton_Click(object sender, RoutedEventArgs e)
        {
            if (_signalsList.Count < 1)
            {
                MessageBox.Show("Нет сигнала для фильтрации!", "Ошибка");
                return;
            }

            float windowSize = float.Parse(FilterWindowSizeTextBox.Text, CultureInfo.InvariantCulture);
            AddSignal(Tools.GetFilteredSignal(_signalsList.Last(), windowSize));
        }

        private void FilterOnlineButton_Click(object sender, RoutedEventArgs e)
        {
            if (_signalsList.Count < 1)
            {
                MessageBox.Show("Нет сигнала для фильтрации!", "Ошибка");
                return;
            }

            float windowSize = float.Parse(FilterWindowSizeTextBox.Text, CultureInfo.InvariantCulture);
            SingleChannelSignal srcSignal = _signalsList.Last();
            float[] srcData = srcSignal.Data;

            OnlineSingleChannelFilter filter = new OnlineSingleChannelFilter(
                (int)(windowSize * srcSignal.Frequency + 0.5));
            int signalLength = srcData.Length;
            int portionLength = 10;
            int index = 0;
            IEnumerable<float> resData = new float[0];
            while (index < signalLength)
            {
                int cnt = Math.Min(portionLength, signalLength - index);
                float[] fData = filter.Filter(srcData.Skip(index).Take(cnt).ToArray());
                resData = resData.Concat(fData);
                index += cnt;
            }

            SingleChannelSignal filtSignal = new SingleChannelSignal(resData.ToArray(), srcSignal.Frequency);
            AddSignal(filtSignal);
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ResetPlot();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_signalsList.Count > 0)
            {
                _signalsList.RemoveAt(_signalsList.Count - 1);
                WpfPlot1.Plot.Remove(WpfPlot1.Plot.PlottableList.Last());
                WpfPlot1.Plot.Axes.AutoScale();
                WpfPlot1.Refresh();
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog myDialog = new OpenFileDialog();
            myDialog.Filter = "csv-файл (*.csv)|*.csv";
            myDialog.CheckFileExists = true;
            myDialog.Multiselect = false;
            if (myDialog.ShowDialog() == true)
            {
                string filePath = myDialog.FileName;
                SingleChannelSignal signal = SingleChannelSignalReader.Read(filePath);
                AddSignal(signal);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog myDialog = new SaveFileDialog();
            myDialog.Filter = "csv-файл (*.csv)|*.csv";
            myDialog.FileName = "MySignal";
            myDialog.DefaultExt = "csv";
            if (myDialog.ShowDialog() == true)
            {
                if (_signalsList.Count < 1)
                {
                    MessageBox.Show("Нет сигнала для сохранения!", "Ошибка");
                    return;
                }

                string filePath = myDialog.FileName;
                SingleChannelSignalWriter.Write(_signalsList.Last(), filePath);
            }
        }


        #endregion

        
    }
}
