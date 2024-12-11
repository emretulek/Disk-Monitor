using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot;
using System.ComponentModel;
using System.Timers;
using System.Diagnostics;
using System.Windows;
using Widgets.Common;

namespace Disk_Monitor
{
    public class DiskViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly Schedule schedule = new();
        private string scheduleID = "";
        private readonly CancellationTokenSource cancellationTokenSource = new();

        public struct SettingsStruct
        {
            public float TimeLine { get; set; }
            public string GraphicColor { get; set; }
            public string DiskLetter { get; set; }
        }

        public static SettingsStruct Default => new()
        {
            DiskLetter = "Total",
            TimeLine = 200,
            GraphicColor = "#9e269a",
        };

        public required SettingsStruct Settings = Default;

        private PerformanceCounter? diskCounter;
        private AreaSeries? AreaSeries;
        private int timeCounter;

        private PlotModel? _plotModel;
        public PlotModel? DiskPlotModel
        {
            get { return _plotModel; }
            set
            {
                _plotModel = value;
                OnPropertyChanged(nameof(DiskPlotModel));
            }
        }

        private string _diskUsageText = "0";
        public string DiskUsageText
        {
            get { return _diskUsageText; }
            set
            {
                _diskUsageText = value;
                OnPropertyChanged(nameof(DiskUsageText));
            }
        }

        public async Task Start()
        {
            await Task.Run(() =>
            {
                diskCounter = GetDiskCounter(Settings.DiskLetter);
                UpdateDiskUsage();
            }, cancellationTokenSource.Token);
            CreatePlot();
            scheduleID = schedule.Secondly(UpdateDiskUsage, 1);
        }

        private void CreatePlot()
        {
            // Plot modelini ve alan serisini ayarlama
            DiskPlotModel = new PlotModel
            {
                PlotAreaBorderThickness = new OxyThickness(0),
                PlotAreaBorderColor = OxyColors.Transparent,
                Padding = new OxyThickness(0)
            };

            var xAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                IsAxisVisible = false,
                MajorGridlineStyle = LineStyle.None,
                MinorGridlineStyle = LineStyle.None,
                MaximumPadding = 0,
                MinimumPadding = 0
            };

            var yAxis = new LinearAxis
            {
                Minimum = 0,
                Maximum = 100,
                Position = AxisPosition.Left,
                IsAxisVisible = false,
                MajorGridlineStyle = LineStyle.None,
                MinorGridlineStyle = LineStyle.None,
                MaximumPadding = 0,
                MinimumPadding = 0
            };

            DiskPlotModel.Axes.Add(xAxis);
            DiskPlotModel.Axes.Add(yAxis);

            AreaSeries = new AreaSeries
            {
                LineStyle = LineStyle.Solid,
                StrokeThickness = 1,
                Color = OxyColor.Parse(Settings.GraphicColor)
            };

            DiskPlotModel.Series.Add(AreaSeries);
        }

        private void UpdateDiskUsage()
        {
            if (DiskPlotModel is null || diskCounter is null || AreaSeries is null) return;

            double diskUsage = diskCounter.NextValue();

            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var disk_text = string.Equals(Settings.DiskLetter, "Total") ? "Total" : $"({Settings.DiskLetter}:)";
                    DiskUsageText = $"Disk {disk_text} {diskUsage:F2}%";

                    AreaSeries.Points.Add(new DataPoint(timeCounter, diskUsage));
                    AreaSeries.Points2.Add(new DataPoint(timeCounter, 0));
                    timeCounter++;

                    if (AreaSeries.Points.Count > Settings.TimeLine)
                    {
                        AreaSeries.Points.RemoveAt(0);
                        AreaSeries.Points2.RemoveAt(0);
                    }

                    DiskPlotModel.InvalidatePlot(true);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message);
                }
            });
        }

        /// <summary>
        /// Kullanılabilir diskler
        /// </summary>
        /// <param name="diskLetter"></param>
        /// <returns></returns>
        private static PerformanceCounter? GetDiskCounter(string diskLetter)
        {
            // Total disk kullanımı için Total sözcüğüne bak 
            if(string.Equals(diskLetter, "Total", StringComparison.OrdinalIgnoreCase))
            {
                return new PerformanceCounter("LogicalDisk", "% Disk Time", "_Total");
            }
            // PhysicalDisk kategorisindeki tüm örnek adlarını al
            var diskCategory = new PerformanceCounterCategory("LogicalDisk");
            var instanceNames = diskCategory.GetInstanceNames();

            // Kullanıcının belirttiği sürücü harfine göre örneği bulmaya çalış
            var matchedInstance = instanceNames.FirstOrDefault(name => name.Equals($"{diskLetter}:"));

            if (matchedInstance != null)
            {
                // Eşleşen örneği kullanarak PerformanceCounter oluştur
                return new PerformanceCounter("LogicalDisk", "% Disk Time", matchedInstance);
            }

            return null;
        }

        public void Dispose()
        {
            schedule.Stop(scheduleID);
            cancellationTokenSource.Cancel();
            GC.SuppressFinalize(this);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
