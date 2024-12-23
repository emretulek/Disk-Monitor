﻿using System.ComponentModel;
using System.Windows;
using Widgets.Common;

namespace Disk_Monitor
{
    public partial class MainWindow : Window,IWidgetWindow
    {
        public readonly static string WidgetName = "Disk Monitor";
        public readonly static string SettingsFile = "settings.diskmonitor.json";
        private readonly Config config = new(SettingsFile);

        public DiskViewModel ViewModel { get; set; }
        private DiskViewModel.SettingsStruct Settings = DiskViewModel.Default;

        public MainWindow()
        {
            InitializeComponent();

            LoadSettings();
            ViewModel = new()
            {
                Settings = Settings
            };
            DataContext = ViewModel;
            _ = ViewModel.Start();
            Logger.Info($"{WidgetName} is started");
        }

        public void LoadSettings()
        {
            try
            {
                Settings.DiskLetter = PropertyParser.ToString(config.GetValue("disk_letter"), Settings.DiskLetter);
                Settings.GraphicColor = PropertyParser.ToString(config.GetValue("graphic_color"), Settings.GraphicColor);
                Settings.TimeLine = PropertyParser.ToFloat(config.GetValue("graphic_timeline"), Settings.TimeLine);
                UsageText.FontSize = PropertyParser.ToFloat(config.GetValue("usage_font_size"));
                UsageText.Foreground = PropertyParser.ToColorBrush(config.GetValue("usage_foreground"));
            }
            catch (Exception)
            {
                config.Add("usage_font_size", UsageText.FontSize);
                config.Add("usage_foreground", UsageText.Foreground);
                config.Add("graphic_color", Settings.GraphicColor);
                config.Add("graphic_timeline", Settings.TimeLine);
                config.Add("disk_letter", Settings.DiskLetter);
                config.Save();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            ViewModel.Dispose();
            Logger.Info($"{WidgetName} is closed");
        }

        public WidgetWindow WidgetWindow()
        {
            return new WidgetWindow(this, WidgetDefaultStruct());
        }

        public static WidgetDefaultStruct WidgetDefaultStruct()
        {
            return new WidgetDefaultStruct();
        }
    }
}
