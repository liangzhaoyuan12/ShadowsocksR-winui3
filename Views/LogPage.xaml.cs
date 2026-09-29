using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Shadowsocks.Controller;
using Shadowsocks.View;
using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace ShadowsocksR_winui3.Views
{
    public sealed partial class LogPage : Page, IPageController
    {
        private const int MaxReadSize = 65536;

        private string _currentLogFile;
        private string _currentLogFileName;
        private long _currentOffset;
        private DispatcherQueueTimer refreshTimer;

        public LogPage()
        {
            this.InitializeComponent();

            foreach (string font in new string[]
            {
                "Consolas", "Courier New", "Lucida Console", "SimSun-ExtB", "SimSun",
                "NSimSun", "KaiTi", "FangSong", "Microsoft YaHei UI", "Microsoft YaHei"
            })
            {
                FontFamilyCombo.Items.Add(font);
            }
            foreach (string size in new string[] { "9", "10", "11", "12", "14", "16", "18", "20", "24" })
            {
                FontSizeCombo.Items.Add(size);
            }
            FontFamilyCombo.SelectedItem = "Courier New";
            FontSizeCombo.SelectedItem = "14";

            UpdateTexts();

            refreshTimer = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().CreateTimer();
            refreshTimer.Interval = TimeSpan.FromMilliseconds(100);
            refreshTimer.Tick += refreshTimer_Tick;

            this.Loaded += LogPage_Loaded;
            this.Unloaded += LogPage_Unloaded;
        }

        private void LogPage_Loaded(object sender, RoutedEventArgs e)
        {
            ReadLog();
            if (refreshTimer != null)
                refreshTimer.Start();
        }

        private void LogPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (refreshTimer != null)
                refreshTimer.Stop();
        }

        public void OnShow(int arg)
        {
            UpdateTexts();
            ReadLog();
            ScrollLogToEnd();
            refreshTimer.Start();
        }

        private static string MenuText(string key)
        {
            string text = I18N.GetString(key).Replace("&", "");
            if (text.EndsWith("..."))
                text = text.Substring(0, text.Length - 3);
            return text;
        }

        private void UpdateTexts()
        {
            fileToolStripMenuItem.Label = MenuText("&File");
            clearLogToolStripMenuItem.Text = MenuText("Clear &log");
            showInExplorerToolStripMenuItem.Text = MenuText("Show in &Explorer");
            closeToolStripMenuItem.Text = MenuText("&Close");
            wrapTextToolStripMenuItem.Label = MenuText("&Wrap Text");
            alwaysOnTopToolStripMenuItem.Label = MenuText("&Always on top");
            fontToolStripMenuItem.Text = MenuText("&Font...");
            TitleText.Text = I18N.GetString("Log Viewer");
        }

        private void ReadLog()
        {
            var newLogFile = Logging.LogFile;
            if (newLogFile != _currentLogFile)
            {
                _currentOffset = 0;
                _currentLogFile = newLogFile;
                _currentLogFileName = Logging.LogFileName;
            }

            if (string.IsNullOrEmpty(newLogFile))
            {
                TitleText.Text = I18N.GetString("Log Viewer") + " " + _currentLogFileName;
                return;
            }

            try
            {
                using (
                    var reader =
                        new StreamReader(new FileStream(newLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                )
                {
                    if (_currentOffset == 0)
                    {
                        var maxSize = reader.BaseStream.Length;
                        if (maxSize > MaxReadSize)
                        {
                            reader.BaseStream.Seek(-MaxReadSize, SeekOrigin.End);
                            reader.ReadLine();
                        }
                    }
                    else
                    {
                        reader.BaseStream.Seek(_currentOffset, SeekOrigin.Begin);
                    }

                    var txt = reader.ReadToEnd();
                    if (!string.IsNullOrEmpty(txt))
                    {
                        logTextBox.Text += txt;
                        ScrollLogToEnd();
                    }

                    _currentOffset = reader.BaseStream.Position;
                }
            }
            catch (FileNotFoundException)
            {
            }

            TitleText.Text = I18N.GetString("Log Viewer") + " " + _currentLogFileName;
        }

        private void ScrollLogToEnd()
        {
            try
            {
                logTextBox.SelectionStart = logTextBox.Text.Length;
                logTextBox.SelectionLength = 0;
                ScrollViewer scrollViewer = FindScrollViewer(logTextBox);
                if (scrollViewer != null)
                    scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null, true);
            }
            catch
            {
            }
        }

        private static ScrollViewer FindScrollViewer(DependencyObject root)
        {
            if (root == null)
                return null;
            ScrollViewer scrollViewer = root as ScrollViewer;
            if (scrollViewer != null)
                return scrollViewer;
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; ++i)
            {
                ScrollViewer found = FindScrollViewer(VisualTreeHelper.GetChild(root, i));
                if (found != null)
                    return found;
            }
            return null;
        }

        private void refreshTimer_Tick(DispatcherQueueTimer sender, object args)
        {
            ReadLog();
        }

        private void clearLogToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Logging.Clear();
            _currentOffset = 0;
            logTextBox.Text = "";
        }

        private void showInExplorerToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NativeUi.OpenFileInExplorer(Logging.LogFile);
            }
            catch (Exception ex)
            {
                Logging.LogUsefulException(ex);
            }
        }

        private void closeToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShadowsocksR_winui3.MainWindow.Instance.AppWindow.Hide();
            }
            catch
            {
            }
        }

        private void wrapTextToolStripMenuItem_Changed(object sender, RoutedEventArgs e)
        {
            logTextBox.TextWrapping = wrapTextToolStripMenuItem.IsChecked == true ? TextWrapping.Wrap : TextWrapping.NoWrap;
            ScrollLogToEnd();
        }

        private void alwaysOnTopToolStripMenuItem_Changed(object sender, RoutedEventArgs e)
        {
            bool topmost = alwaysOnTopToolStripMenuItem.IsChecked == true;
            SetWindowPos(GetWindowHandle(), topmost ? new IntPtr(-1) : new IntPtr(-2), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0004);
        }

        private void FontFamilyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string name = FontFamilyCombo.SelectedItem as string;
            if (!string.IsNullOrEmpty(name))
            {
                logTextBox.FontFamily = new FontFamily(name);
            }
        }

        private void FontSizeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string value = FontSizeCombo.SelectedItem as string;
            double size;
            if (!string.IsNullOrEmpty(value)
                && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out size)
                && size > 0)
            {
                logTextBox.FontSize = size;
            }
        }

        private IntPtr GetWindowHandle()
        {
            try
            {
                return WinRT.Interop.WindowNative.GetWindowHandle(ShadowsocksR_winui3.MainWindow.Instance);
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    }
}
