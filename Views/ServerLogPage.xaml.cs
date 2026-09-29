using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using Color = Windows.UI.Color;
using Colors = Microsoft.UI.Colors;

namespace ShadowsocksR_winui3.Views
{
    public sealed partial class ServerLogPage : Page, IPageController
    {
        private const int ColumnCount = 19;

        private const int COL_ID = 0;
        private const int COL_GROUP = 1;
        private const int COL_SERVER = 2;
        private const int COL_ENABLE = 3;
        private const int COL_TOTALCONNECT = 4;
        private const int COL_CONNECTING = 5;
        private const int COL_AVGLATENCY = 6;
        private const int COL_AVGDOWNSPEED = 7;
        private const int COL_MAXDOWNSPEED = 8;
        private const int COL_AVGUPSPEED = 9;
        private const int COL_MAXUPSPEED = 10;
        private const int COL_DOWNLOAD = 11;
        private const int COL_UPLOAD = 12;
        private const int COL_DOWNLOADRAW = 13;
        private const int COL_ERRORPERCENT = 14;
        private const int COL_CONNECTERROR = 15;
        private const int COL_CONNECTTIMEOUT = 16;
        private const int COL_CONNECTEMPTY = 17;
        private const int COL_CONTINUOUS = 18;

        private static readonly string[] ColumnNames = new string[]
        {
            "ID", "Group", "Server", "Enable", "TotalConnect", "Connecting",
            "AvgLatency", "AvgDownSpeed", "MaxDownSpeed", "AvgUpSpeed", "MaxUpSpeed",
            "Download", "Upload", "DownloadRaw", "ErrorPercent", "ConnectError",
            "ConnectTimeout", "ConnectEmpty", "Continuous"
        };

        private static readonly string[] ColumnHeaderNames = new string[]
        {
            "ID", "Group", "Server", "Enable", "Total Connect", "Connecting",
            "Latency", "Avg DSpeed", "Max DSpeed", "Avg UpSpeed", "Max UpSpeed",
            "Dload", "Upload", "DloadRaw", "Error Percent", "Error",
            "Timeout", "Empty Response", "Continuous"
        };

        private class ServerLogRow : INotifyPropertyChanged
        {
            public int Id;
            public readonly string[] Texts = new string[ColumnCount];
            public readonly Color?[] BackgroundColors = new Color?[ColumnCount];
            public readonly string[] ToolTips = new string[ColumnCount];
            public readonly int?[] Tags = new int?[ColumnCount];

            public event PropertyChangedEventHandler PropertyChanged;

            public string ID => Texts[COL_ID];
            public string Group => Texts[COL_GROUP];
            public string Server => Texts[COL_SERVER];
            public string Enable => Texts[COL_ENABLE];
            public string TotalConnect => Texts[COL_TOTALCONNECT];
            public string Connecting => Texts[COL_CONNECTING];
            public string AvgLatency => Texts[COL_AVGLATENCY];
            public string AvgDownSpeed => Texts[COL_AVGDOWNSPEED];
            public string MaxDownSpeed => Texts[COL_MAXDOWNSPEED];
            public string AvgUpSpeed => Texts[COL_AVGUPSPEED];
            public string MaxUpSpeed => Texts[COL_MAXUPSPEED];
            public string Download => Texts[COL_DOWNLOAD];
            public string Upload => Texts[COL_UPLOAD];
            public string DownloadRaw => Texts[COL_DOWNLOADRAW];
            public string ErrorPercent => Texts[COL_ERRORPERCENT];
            public string ConnectError => Texts[COL_CONNECTERROR];
            public string ConnectTimeout => Texts[COL_CONNECTTIMEOUT];
            public string ConnectEmpty => Texts[COL_CONNECTEMPTY];
            public string Continuous => Texts[COL_CONTINUOUS];

            public Brush BgID => ToBrush(BackgroundColors[COL_ID]);
            public Brush BgGroup => ToBrush(BackgroundColors[COL_GROUP]);
            public Brush BgServer => ToBrush(BackgroundColors[COL_SERVER]);
            public Brush BgEnable => ToBrush(BackgroundColors[COL_ENABLE]);
            public Brush BgTotalConnect => ToBrush(BackgroundColors[COL_TOTALCONNECT]);
            public Brush BgConnecting => ToBrush(BackgroundColors[COL_CONNECTING]);
            public Brush BgAvgLatency => ToBrush(BackgroundColors[COL_AVGLATENCY]);
            public Brush BgAvgDownSpeed => ToBrush(BackgroundColors[COL_AVGDOWNSPEED]);
            public Brush BgMaxDownSpeed => ToBrush(BackgroundColors[COL_MAXDOWNSPEED]);
            public Brush BgAvgUpSpeed => ToBrush(BackgroundColors[COL_AVGUPSPEED]);
            public Brush BgMaxUpSpeed => ToBrush(BackgroundColors[COL_MAXUPSPEED]);
            public Brush BgDownload => ToBrush(BackgroundColors[COL_DOWNLOAD]);
            public Brush BgUpload => ToBrush(BackgroundColors[COL_UPLOAD]);
            public Brush BgDownloadRaw => ToBrush(BackgroundColors[COL_DOWNLOADRAW]);
            public Brush BgErrorPercent => ToBrush(BackgroundColors[COL_ERRORPERCENT]);
            public Brush BgConnectError => ToBrush(BackgroundColors[COL_CONNECTERROR]);
            public Brush BgConnectTimeout => ToBrush(BackgroundColors[COL_CONNECTTIMEOUT]);
            public Brush BgConnectEmpty => ToBrush(BackgroundColors[COL_CONNECTEMPTY]);
            public Brush BgContinuous => ToBrush(BackgroundColors[COL_CONTINUOUS]);

            public string TipDownload => ToolTips[COL_DOWNLOAD];
            public string TipUpload => ToolTips[COL_UPLOAD];
            public string TipDownloadRaw => ToolTips[COL_DOWNLOADRAW];

            public bool SetText(int column, string value)
            {
                if (Texts[column] == value)
                    return false;
                Texts[column] = value;
                Raise(ColumnNames[column]);
                return true;
            }

            public bool SetColor(int column, Color? value)
            {
                if (BackgroundColors[column] == value)
                    return false;
                BackgroundColors[column] = value;
                Raise("Bg" + ColumnNames[column]);
                return true;
            }

            public bool SetToolTip(int column, string value)
            {
                if (ToolTips[column] == value)
                    return false;
                ToolTips[column] = value;
                Raise("Tip" + ColumnNames[column]);
                return true;
            }

            private static readonly Brush TransparentBrush = new SolidColorBrush(Colors.Transparent);

            private static Brush ToBrush(Color? color)
            {
                return color.HasValue ? new SolidColorBrush(color.Value) : TransparentBrush;
            }

            private void Raise(string name)
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                    handler(this, new PropertyChangedEventArgs(name));
            }
        }

        private ShadowsocksController controller;
        private List<int> listOrder = new List<int>();
        private int lastRefreshIndex = 0;
        private bool firstDispley = true;
        private bool rowChange = false;
        private int updatePause = 0;
        private int updateTick = 0;
        private int updateSize = 0;
        private int pendingUpdate = 0;
        private string title_perfix = "";
        private ServerSpeedLogShow[] ServerSpeedLogList;
        private Thread workerThread;
        private AutoResetEvent workerEvent = new AutoResetEvent(false);
        private DispatcherQueueTimer timer;
        private bool wasMinimized = false;
        private bool topmostItem = false;
        private IntPtr windowHandle = IntPtr.Zero;
        private readonly ObservableCollection<ServerLogRow> _rows = new ObservableCollection<ServerLogRow>();
        private readonly HashSet<ServerLogRow> visibleRows = new HashSet<ServerLogRow>();
        private DataGridColumn sortColumn;
        private bool sortDescending;
        private double lastGridWidth = -1;

        public ServerLogPage()
        {
            this.InitializeComponent();
            controller = ShadowsocksR_winui3.App.Controller;
            try
            {
                title_perfix = Shadowsocks.Util.Utils.StartupPath;
                if (title_perfix.Length > 20)
                    title_perfix = title_perfix.Substring(0, 20);
            }
            catch
            {
                title_perfix = "";
            }
            for (int i = 0; i < ServerDataGrid.Columns.Count && i < ColumnNames.Length; ++i)
                ServerDataGrid.Columns[i].Tag = ColumnNames[i];
            ServerDataGrid.ItemsSource = _rows;
            ServerDataGrid.AddHandler(UIElement.TappedEvent, new TappedEventHandler(ServerDataGrid_Tapped), true);
            ServerDataGrid.AddHandler(UIElement.DoubleTappedEvent, new DoubleTappedEventHandler(ServerDataGrid_DoubleTapped), true);
            ServerDataGrid.AddHandler(UIElement.RightTappedEvent, new RightTappedEventHandler(ServerDataGrid_RightTapped), true);
            UpdateTexts();
            if (controller != null)
                UpdateLog();
            if (controller != null)
                controller.ConfigChanged += controller_ConfigChanged;
            timer = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().CreateTimer();
            timer.Interval = TimeSpan.FromMilliseconds(250);
            timer.Tick += timer_Tick;
            this.SizeChanged += ServerLogPage_SizeChanged;
            this.Loaded += ServerLogPage_Loaded;
            this.Unloaded += ServerLogPage_Unloaded;
        }

        private void ServerLogPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (controller == null)
                return;
            controller.ConfigChanged -= controller_ConfigChanged;
            controller.ConfigChanged += controller_ConfigChanged;
            UpdateLog();
            if (timer != null)
                timer.Start();
        }

        public void OnShow(int arg)
        {
            if (controller == null)
                return;
            controller.ConfigChanged -= controller_ConfigChanged;
            controller.ConfigChanged += controller_ConfigChanged;
            UpdateTexts();
            UpdateLog();
            RefreshLog();
            timer.Start();
        }

        private void ServerLogPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (controller != null)
                controller.ConfigChanged -= controller_ConfigChanged;
            if (timer != null)
                timer.Stop();
            Thread thread = workerThread;
            workerThread = null;
            while (thread != null && thread.IsAlive)
            {
                workerEvent.Set();
                Thread.Sleep(50);
            }
        }

        private void ServerDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            ServerLogRow row = e.Row != null ? e.Row.DataContext as ServerLogRow : null;
            if (row != null)
                visibleRows.Add(row);
        }

        private void ServerDataGrid_UnloadingRow(object sender, DataGridRowEventArgs e)
        {
            ServerLogRow row = e.Row != null ? e.Row.DataContext as ServerLogRow : null;
            if (row != null)
                visibleRows.Remove(row);
        }

        private void ServerLogPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            updatePause = 2;
            double width = ServerDataGrid.ActualWidth;
            if (width <= 0 || lastGridWidth <= 0)
            {
                lastGridWidth = width;
                return;
            }
            double delta = width - lastGridWidth;
            lastGridWidth = width;
            if (delta == 0 || ServerDataGrid.Columns.Count <= COL_SERVER)
                return;
            DataGridColumn column = ServerDataGrid.Columns[COL_SERVER];
            if (column.Visibility != Visibility.Visible)
                return;
            double current = column.ActualWidth;
            if (current <= 0)
                current = 2;
            double target = current + delta;
            if (target < 2)
                target = 2;
            column.Width = new DataGridLength(target);
        }

        private static string MenuText(string key)
        {
            return I18N.GetString(key).Replace("&", "");
        }

        private void UpdateMenuTexts(MenuFlyout menu)
        {
            if (menu == null)
                return;
            foreach (MenuFlyoutItemBase item in menu.Items)
            {
                MenuFlyoutItem menuItem = item as MenuFlyoutItem;
                if (menuItem != null)
                {
                    string key = menuItem.Tag as string;
                    if (!string.IsNullOrEmpty(key))
                        menuItem.Text = MenuText(key);
                    continue;
                }
                ToggleMenuFlyoutItem toggleItem = item as ToggleMenuFlyoutItem;
                if (toggleItem != null)
                {
                    string key = toggleItem.Tag as string;
                    if (!string.IsNullOrEmpty(key))
                        toggleItem.Text = MenuText(key);
                }
            }
        }

        private void UpdateTexts()
        {
            UpdateTitle();
            for (int i = 0; i < ServerDataGrid.Columns.Count; ++i)
            {
                DataGridColumn column = ServerDataGrid.Columns[i];
                string name = column.Tag as string;
                int index = name != null ? Array.IndexOf(ColumnNames, name) : i;
                if (index < 0 || index >= ColumnHeaderNames.Length)
                    index = i;
                column.Header = I18N.GetString(ColumnHeaderNames[index]);
            }
            ControlMenuButton.Label = MenuText("&Control");
            PortOutMenuButton.Label = MenuText("Port &out");
            WindowMenuButton.Label = MenuText("&Window");
            UpdateMenuTexts(ControlMenuButton.Flyout as MenuFlyout);
            UpdateMenuTexts(PortOutMenuButton.Flyout as MenuFlyout);
            UpdateMenuTexts(WindowMenuButton.Flyout as MenuFlyout);
            UpdateMenuTexts(Resources["GridMenu"] as MenuFlyout);
        }

        private void UpdateTitle()
        {
            if (controller == null)
                return;
            Configuration config = controller.GetCurrentConfiguration();
            if (config == null)
                return;
            TitleText.Text = title_perfix + I18N.GetString("ServerLog") + "("
                + (config.shareOverLan ? "any" : "local") + ":" + config.localPort.ToString()
                + "(" + Shadowsocks.Model.Server.GetForwardServerRef().GetConnections().Count.ToString() + ")"
                + " " + I18N.GetString("Version") + UpdateChecker.FullVersion
                + ")";
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            UpdateTitle();
        }

        private string FormatBytes(long bytes)
        {
            const long K = 1024L;
            const long M = K * 1024L;
            const long G = M * 1024L;
            const long T = G * 1024L;
            const long P = T * 1024L;
            const long E = P * 1024L;

            if (bytes >= M * 990)
            {
                if (bytes >= G * 990)
                {
                    if (bytes >= P * 990)
                        return (bytes / (double)E).ToString("F3") + "E";
                    if (bytes >= T * 990)
                        return (bytes / (double)P).ToString("F3") + "P";
                    return (bytes / (double)T).ToString("F3") + "T";
                }
                else
                {
                    if (bytes >= G * 99)
                        return (bytes / (double)G).ToString("F2") + "G";
                    if (bytes >= G * 9)
                        return (bytes / (double)G).ToString("F3") + "G";
                    return (bytes / (double)G).ToString("F4") + "G";
                }
            }
            else
            {
                if (bytes >= K * 990)
                {
                    if (bytes >= M * 100)
                        return (bytes / (double)M).ToString("F1") + "M";
                    if (bytes > M * 9.9)
                        return (bytes / (double)M).ToString("F2") + "M";
                    return (bytes / (double)M).ToString("F3") + "M";
                }
                else
                {
                    if (bytes > K * 99)
                        return (bytes / (double)K).ToString("F0") + "K";
                    if (bytes > 900)
                        return (bytes / (double)K).ToString("F1") + "K";
                    return bytes.ToString();
                }
            }
        }

        private static byte ColorMix(byte a, byte b, double alpha)
        {
            return (byte)(b * alpha + a * (1 - alpha));
        }

        private static Color ColorMix(Color a, Color b, double alpha)
        {
            return Color.FromArgb(255, ColorMix(a.R, b.R, alpha),
                ColorMix(a.G, b.G, alpha),
                ColorMix(a.B, b.B, alpha));
        }

        public void UpdateLogThread()
        {
            while (workerThread != null)
            {
                if (controller != null)
                {
                    Configuration config = controller.GetCurrentConfiguration();
                    ServerSpeedLogShow[] _ServerSpeedLogList = new ServerSpeedLogShow[config.configs.Count];
                    for (int i = 0; i < config.configs.Count; ++i)
                    {
                        _ServerSpeedLogList[i] = config.configs[i].ServerSpeedLog().Translate();
                    }
                    ServerSpeedLogList = _ServerSpeedLogList;
                }

                workerEvent.WaitOne();
            }
        }

        public void UpdateLog()
        {
            if (workerThread == null)
            {
                workerThread = new Thread(this.UpdateLogThread);
                workerThread.IsBackground = true;
                workerThread.Start();
            }
            else
            {
                workerEvent.Set();
            }
        }

        public void RefreshLog()
        {
            if (controller == null)
                return;
            if (ServerSpeedLogList == null)
                return;

            int last_rowcount = _rows.Count;
            Configuration config = controller.GetCurrentConfiguration();
            if (config == null)
                return;
            if (listOrder.Count > config.configs.Count)
            {
                listOrder.RemoveRange(config.configs.Count, listOrder.Count - config.configs.Count);
            }
            while (listOrder.Count < config.configs.Count)
            {
                listOrder.Add(0);
            }
            while (_rows.Count < config.configs.Count && _rows.Count < ServerSpeedLogList.Length)
            {
                ServerLogRow newRow = new ServerLogRow();
                _rows.Add(newRow);
                int newId = _rows.Count - 1;
                newRow.Id = newId;
                newRow.SetText(COL_ID, newId.ToString());
            }
            if (_rows.Count > config.configs.Count)
            {
                for (int list_index = 0; list_index < _rows.Count; ++list_index)
                {
                    int removeId = _rows[list_index].Id;
                    if (removeId >= config.configs.Count)
                    {
                        visibleRows.Remove(_rows[list_index]);
                        _rows.RemoveAt(list_index);
                        --list_index;
                    }
                }
            }
            int sortIndex = sortColumn != null ? DataIndexOf(sortColumn) : -1;
            try
            {
                for (int list_index = (lastRefreshIndex >= _rows.Count) ? 0 : lastRefreshIndex, rowChangeCnt = 0;
                    list_index < _rows.Count && rowChangeCnt <= 100;
                    ++list_index)
                {
                    lastRefreshIndex = list_index + 1;

                    ServerLogRow row = _rows[list_index];
                    int id = row.Id;
                    if (id < 0 || id >= config.configs.Count)
                        continue;
                    if (ServerSpeedLogList == null || id >= ServerSpeedLogList.Length)
                        continue;
                    Server server = config.configs[id];
                    ServerSpeedLogShow serverSpeedLog = ServerSpeedLogList[id];
                    listOrder[id] = list_index;
                    bool rowVisible = visibleRows.Count == 0 || visibleRows.Contains(row);
                    rowChange = false;
                    for (int curcol = 0; curcol < ColumnCount; ++curcol)
                    {
                        if (!firstDispley && !rowVisible && sortIndex != curcol)
                            continue;
                        string columnName = ColumnNames[curcol];
                        if (columnName == "Server")
                        {
                            rowChange |= row.SetColor(curcol, config.index == id ? Colors.Cyan : Colors.White);
                            rowChange |= row.SetText(curcol, server.FriendlyName());
                        }
                        else if (columnName == "Group")
                        {
                            rowChange |= row.SetText(curcol, server.group);
                        }
                        else if (columnName == "Enable")
                        {
                            rowChange |= row.SetColor(curcol, server.isEnable() ? Colors.White : Colors.Red);
                        }
                        else if (columnName == "TotalConnect")
                        {
                            rowChange |= row.SetText(curcol, serverSpeedLog.totalConnectTimes.ToString());
                        }
                        else if (columnName == "Connecting")
                        {
                            long connections = serverSpeedLog.totalConnectTimes - serverSpeedLog.totalDisconnectTimes;
                            Color[] colList = new Color[5] { Colors.White, Colors.LightGreen, Colors.Yellow, Colors.Red, Colors.Red };
                            long[] bytesList = new long[5] { 0, 16, 32, 64, 65536 };
                            for (int i = 1; i < colList.Length; ++i)
                            {
                                if (connections < bytesList[i])
                                {
                                    rowChange |= row.SetColor(curcol,
                                        ColorMix(colList[i - 1],
                                            colList[i],
                                            (double)(connections - bytesList[i - 1]) / (bytesList[i] - bytesList[i - 1])
                                        )
                                        );
                                    break;
                                }
                            }
                            rowChange |= row.SetText(curcol, (serverSpeedLog.totalConnectTimes - serverSpeedLog.totalDisconnectTimes).ToString());
                        }
                        else if (columnName == "AvgLatency")
                        {
                            if (serverSpeedLog.avgConnectTime >= 0)
                                rowChange |= row.SetText(curcol, (serverSpeedLog.avgConnectTime / 1000).ToString());
                            else
                                rowChange |= row.SetText(curcol, "-");
                        }
                        else if (columnName == "AvgDownSpeed")
                        {
                            long avgBytes = serverSpeedLog.avgDownloadBytes;
                            string valStr = FormatBytes(avgBytes);
                            Color[] colList = new Color[6] { Colors.White, Colors.LightGreen, Colors.Yellow, Colors.Pink, Colors.Red, Colors.Red };
                            long[] bytesList = new long[6] { 0, 1024 * 64, 1024 * 512, 1024 * 1024 * 4, 1024 * 1024 * 16, 1024L * 1024 * 1024 * 1024 };
                            for (int i = 1; i < colList.Length; ++i)
                            {
                                if (avgBytes < bytesList[i])
                                {
                                    rowChange |= row.SetColor(curcol,
                                        ColorMix(colList[i - 1],
                                            colList[i],
                                            (double)(avgBytes - bytesList[i - 1]) / (bytesList[i] - bytesList[i - 1])
                                        )
                                        );
                                    break;
                                }
                            }
                            rowChange |= row.SetText(curcol, valStr);
                        }
                        else if (columnName == "MaxDownSpeed")
                        {
                            long maxBytes = serverSpeedLog.maxDownloadBytes;
                            string valStr = FormatBytes(maxBytes);
                            Color[] colList = new Color[6] { Colors.White, Colors.LightGreen, Colors.Yellow, Colors.Pink, Colors.Red, Colors.Red };
                            long[] bytesList = new long[6] { 0, 1024 * 64, 1024 * 512, 1024 * 1024 * 4, 1024 * 1024 * 16, 1024 * 1024 * 1024 };
                            for (int i = 1; i < colList.Length; ++i)
                            {
                                if (maxBytes < bytesList[i])
                                {
                                    rowChange |= row.SetColor(curcol,
                                        ColorMix(colList[i - 1],
                                            colList[i],
                                            (double)(maxBytes - bytesList[i - 1]) / (bytesList[i] - bytesList[i - 1])
                                        )
                                        );
                                    break;
                                }
                            }
                            rowChange |= row.SetText(curcol, valStr);
                        }
                        else if (columnName == "AvgUpSpeed")
                        {
                            long avgBytes = serverSpeedLog.avgUploadBytes;
                            string valStr = FormatBytes(avgBytes);
                            Color[] colList = new Color[6] { Colors.White, Colors.LightGreen, Colors.Yellow, Colors.Pink, Colors.Red, Colors.Red };
                            long[] bytesList = new long[6] { 0, 1024 * 64, 1024 * 512, 1024 * 1024 * 4, 1024 * 1024 * 16, 1024L * 1024 * 1024 * 1024 };
                            for (int i = 1; i < colList.Length; ++i)
                            {
                                if (avgBytes < bytesList[i])
                                {
                                    rowChange |= row.SetColor(curcol,
                                        ColorMix(colList[i - 1],
                                            colList[i],
                                            (double)(avgBytes - bytesList[i - 1]) / (bytesList[i] - bytesList[i - 1])
                                        )
                                        );
                                    break;
                                }
                            }
                            rowChange |= row.SetText(curcol, valStr);
                        }
                        else if (columnName == "MaxUpSpeed")
                        {
                            long maxBytes = serverSpeedLog.maxUploadBytes;
                            string valStr = FormatBytes(maxBytes);
                            Color[] colList = new Color[6] { Colors.White, Colors.LightGreen, Colors.Yellow, Colors.Pink, Colors.Red, Colors.Red };
                            long[] bytesList = new long[6] { 0, 1024 * 64, 1024 * 512, 1024 * 1024 * 4, 1024 * 1024 * 16, 1024 * 1024 * 1024 };
                            for (int i = 1; i < colList.Length; ++i)
                            {
                                if (maxBytes < bytesList[i])
                                {
                                    rowChange |= row.SetColor(curcol,
                                        ColorMix(colList[i - 1],
                                            colList[i],
                                            (double)(maxBytes - bytesList[i - 1]) / (bytesList[i] - bytesList[i - 1])
                                        )
                                        );
                                    break;
                                }
                            }
                            rowChange |= row.SetText(curcol, valStr);
                        }
                        else if (columnName == "Upload")
                        {
                            string valStr = FormatBytes(serverSpeedLog.totalUploadBytes);
                            string fullVal = serverSpeedLog.totalUploadBytes.ToString();
                            if (row.ToolTips[curcol] != fullVal)
                            {
                                if (fullVal == "0")
                                    rowChange |= row.SetColor(curcol, Color.FromArgb(255, 0xf4, 0xff, 0xf4));
                                else
                                {
                                    rowChange |= row.SetColor(curcol, Colors.LightGreen);
                                    row.Tags[curcol] = 8;
                                }
                            }
                            else if (row.Tags[curcol] != null)
                            {
                                row.Tags[curcol] = row.Tags[curcol] - 1;
                                if (row.Tags[curcol] == 0)
                                    rowChange |= row.SetColor(curcol, Color.FromArgb(255, 0xf4, 0xff, 0xf4));
                            }
                            rowChange |= row.SetToolTip(curcol, fullVal);
                            rowChange |= row.SetText(curcol, valStr);
                        }
                        else if (columnName == "Download")
                        {
                            string valStr = FormatBytes(serverSpeedLog.totalDownloadBytes);
                            string fullVal = serverSpeedLog.totalDownloadBytes.ToString();
                            if (row.ToolTips[curcol] != fullVal)
                            {
                                if (fullVal == "0")
                                    rowChange |= row.SetColor(curcol, Color.FromArgb(255, 0xff, 0xf0, 0xf0));
                                else
                                {
                                    rowChange |= row.SetColor(curcol, Colors.LightGreen);
                                    row.Tags[curcol] = 8;
                                }
                            }
                            else if (row.Tags[curcol] != null)
                            {
                                row.Tags[curcol] = row.Tags[curcol] - 1;
                                if (row.Tags[curcol] == 0)
                                    rowChange |= row.SetColor(curcol, Color.FromArgb(255, 0xff, 0xf0, 0xf0));
                            }
                            rowChange |= row.SetToolTip(curcol, fullVal);
                            rowChange |= row.SetText(curcol, valStr);
                        }
                        else if (columnName == "DownloadRaw")
                        {
                            string valStr = FormatBytes(serverSpeedLog.totalDownloadRawBytes);
                            string fullVal = serverSpeedLog.totalDownloadRawBytes.ToString();
                            if (row.ToolTips[curcol] != fullVal)
                            {
                                if (fullVal == "0")
                                    rowChange |= row.SetColor(curcol, Color.FromArgb(255, 0xff, 0x80, 0x80));
                                else
                                {
                                    rowChange |= row.SetColor(curcol, Colors.LightGreen);
                                    row.Tags[curcol] = 8;
                                }
                            }
                            else if (row.Tags[curcol] != null)
                            {
                                row.Tags[curcol] = row.Tags[curcol] - 1;
                                if (row.Tags[curcol] == 0)
                                {
                                    if (fullVal == "0")
                                        rowChange |= row.SetColor(curcol, Color.FromArgb(255, 0xff, 0x80, 0x80));
                                    else
                                        rowChange |= row.SetColor(curcol, Color.FromArgb(255, 0xf0, 0xf0, 0xff));
                                }
                            }
                            rowChange |= row.SetToolTip(curcol, fullVal);
                            rowChange |= row.SetText(curcol, valStr);
                        }
                        else if (columnName == "ConnectError")
                        {
                            long val = serverSpeedLog.errorConnectTimes + serverSpeedLog.errorDecodeTimes;
                            Color col = Color.FromArgb(255, 255, (byte)Math.Max(0, 255 - val * 2.5), (byte)Math.Max(0, 255 - val * 2.5));
                            rowChange |= row.SetColor(curcol, col);
                            rowChange |= row.SetText(curcol, val.ToString());
                        }
                        else if (columnName == "ConnectTimeout")
                        {
                            rowChange |= row.SetText(curcol, serverSpeedLog.errorTimeoutTimes.ToString());
                        }
                        else if (columnName == "ConnectEmpty")
                        {
                            long val = serverSpeedLog.errorEmptyTimes;
                            Color col = Color.FromArgb(255, 255, (byte)Math.Max(0, 255 - val * 8), (byte)Math.Max(0, 255 - val * 8));
                            rowChange |= row.SetColor(curcol, col);
                            rowChange |= row.SetText(curcol, val.ToString());
                        }
                        else if (columnName == "Continuous")
                        {
                            long val = serverSpeedLog.errorContinurousTimes;
                            Color col = Color.FromArgb(255, 255, (byte)Math.Max(0, 255 - val * 8), (byte)Math.Max(0, 255 - val * 8));
                            rowChange |= row.SetColor(curcol, col);
                            rowChange |= row.SetText(curcol, val.ToString());
                        }
                        else if (columnName == "ErrorPercent")
                        {
                            if (serverSpeedLog.errorLogTimes + serverSpeedLog.totalConnectTimes - serverSpeedLog.totalDisconnectTimes > 0)
                            {
                                double percent = (serverSpeedLog.errorConnectTimes
                                    + serverSpeedLog.errorTimeoutTimes
                                    + serverSpeedLog.errorDecodeTimes)
                                    * 100.00
                                    / (serverSpeedLog.errorLogTimes + serverSpeedLog.totalConnectTimes - serverSpeedLog.totalDisconnectTimes);
                                rowChange |= row.SetColor(curcol, Color.FromArgb(255, 255, (byte)(255 - percent * 2), (byte)(255 - percent * 2)));
                                rowChange |= row.SetText(curcol, percent.ToString("F0") + "%");
                            }
                            else
                            {
                                rowChange |= row.SetColor(curcol, Colors.White);
                                rowChange |= row.SetText(curcol, "-");
                            }
                        }
                    }
                    if (rowChange && rowVisible)
                        rowChangeCnt++;
                }
            }
            catch
            {

            }
            UpdateTitle();
            if (sortColumn != null)
            {
                ApplySort();
            }
            if (last_rowcount == 0 && config.index >= 0 && config.index < _rows.Count)
            {
                ServerDataGrid.SelectedIndex = config.index;
            }
            if (firstDispley)
            {
                if (config.index >= 0 && config.index < _rows.Count && ServerDataGrid.Columns.Count > 0)
                {
                    try
                    {
                        ServerDataGrid.ScrollIntoView(_rows[config.index], ServerDataGrid.Columns[0]);
                    }
                    catch
                    {
                    }
                }
                firstDispley = false;
            }
        }

        private string ColumnNameOf(DataGridColumn column)
        {
            if (column == null)
                return "";
            string name = column.Tag as string;
            if (!string.IsNullOrEmpty(name))
                return name;
            int index = ServerDataGrid.Columns.IndexOf(column);
            if (index >= 0 && index < ColumnNames.Length)
                return ColumnNames[index];
            return "";
        }

        private int DataIndexOf(DataGridColumn column)
        {
            string name = ColumnNameOf(column);
            if (string.IsNullOrEmpty(name))
                return -1;
            return Array.IndexOf(ColumnNames, name);
        }

        private long Str2Long(string str)
        {
            if (string.IsNullOrEmpty(str)) return -1;
            if (str == "-") return -1;
            if (str.LastIndexOf('K') > 0)
            {
                Double ret = Convert.ToDouble(str.Substring(0, str.LastIndexOf('K')));
                return (long)(ret * 1024);
            }
            if (str.LastIndexOf('M') > 0)
            {
                Double ret = Convert.ToDouble(str.Substring(0, str.LastIndexOf('M')));
                return (long)(ret * 1024 * 1024);
            }
            if (str.LastIndexOf('G') > 0)
            {
                Double ret = Convert.ToDouble(str.Substring(0, str.LastIndexOf('G')));
                return (long)(ret * 1024 * 1024 * 1024);
            }
            if (str.LastIndexOf('T') > 0)
            {
                Double ret = Convert.ToDouble(str.Substring(0, str.LastIndexOf('T')));
                return (long)(ret * 1024 * 1024 * 1024 * 1024);
            }
            try
            {
                Double ret = Convert.ToDouble(str);
                return (long)ret;
            }
            catch
            {
                return -1;
            }
        }

        private static int ToInt(string str)
        {
            if (string.IsNullOrEmpty(str))
                return 0;
            int value;
            if (int.TryParse(str, out value))
                return value;
            return 0;
        }

        private int CompareValues(int column, ServerLogRow a, ServerLogRow b)
        {
            try
            {
                if (column < 0 || column >= ColumnNames.Length)
                    return 0;
                string name = ColumnNames[column];
                string s1 = a.Texts[column];
                string s2 = b.Texts[column];
                if (name == "Server" || name == "Group")
                {
                    return string.Compare(s1, s2);
                }
                if (name == "ID"
                    || name == "TotalConnect"
                    || name == "Connecting"
                    || name == "ConnectError"
                    || name == "ConnectTimeout"
                    || name == "Continuous"
                    )
                {
                    int v1 = ToInt(s1);
                    int v2 = ToInt(s2);
                    return (v1 == v2 ? 0 : (v1 < v2 ? -1 : 1));
                }
                if (name == "ErrorPercent")
                {
                    string t1 = s1 ?? "";
                    string t2 = s2 ?? "";
                    int v1 = t1.Length <= 1 ? 0 : Convert.ToInt32(Convert.ToDouble(t1.Substring(0, t1.Length - 1)) * 100);
                    int v2 = t2.Length <= 1 ? 0 : Convert.ToInt32(Convert.ToDouble(t2.Substring(0, t2.Length - 1)) * 100);
                    return v1 == v2 ? 0 : v1 < v2 ? -1 : 1;
                }
                if (name == "AvgLatency"
                    || name == "AvgDownSpeed"
                    || name == "MaxDownSpeed"
                    || name == "AvgUpSpeed"
                    || name == "MaxUpSpeed"
                    || name == "Upload"
                    || name == "Download"
                    || name == "DownloadRaw"
                    )
                {
                    long v1 = Str2Long(s1);
                    long v2 = Str2Long(s2);
                    return (v1 == v2 ? 0 : (v1 < v2 ? -1 : 1));
                }
                return string.Compare(s1, s2);
            }
            catch
            {
                return 0;
            }
        }

        private int CompareRows(ServerLogRow a, ServerLogRow b)
        {
            int result = CompareValues(sortColumnIndexCache, a, b);
            if (result != 0)
                return sortDescending ? -result : result;
            int v1 = (a.Id >= 0 && a.Id < listOrder.Count) ? listOrder[a.Id] : 0;
            int v2 = (b.Id >= 0 && b.Id < listOrder.Count) ? listOrder[b.Id] : 0;
            return v1 == v2 ? 0 : (v1 < v2 ? -1 : 1);
        }

        private int sortColumnIndexCache;

        private void ApplySort()
        {
            if (sortColumn == null)
                return;
            int index = ServerDataGrid.Columns.IndexOf(sortColumn);
            if (index < 0)
                return;
            sortColumnIndexCache = DataIndexOf(sortColumn);
            object selected = ServerDataGrid.SelectedItem;
            List<ServerLogRow> sorted = new List<ServerLogRow>(_rows);
            sorted.Sort(CompareRows);
            for (int i = 0; i < sorted.Count; ++i)
            {
                int current = _rows.IndexOf(sorted[i]);
                if (current != i)
                    _rows.Move(current, i);
            }
            if (selected != null)
            {
                int newIndex = _rows.IndexOf(selected as ServerLogRow);
                if (newIndex >= 0)
                    ServerDataGrid.SelectedIndex = newIndex;
            }
        }

        private void SortByColumn(DataGridColumn column)
        {
            if (column == null)
                return;
            int index = ServerDataGrid.Columns.IndexOf(column);
            if (index < 0)
                return;
            string name = ColumnNameOf(column);
            if (name == "Enable" || name == "ErrorPercent" || name == "ConnectError"
                || name == "ConnectTimeout" || name == "ConnectEmpty")
                return;
            if (sortColumn == column)
            {
                sortDescending = !sortDescending;
            }
            else
            {
                if (sortColumn != null)
                    sortColumn.SortDirection = null;
                sortColumn = column;
                sortDescending = false;
            }
            column.SortDirection = sortDescending ? DataGridSortDirection.Descending : DataGridSortDirection.Ascending;
            ApplySort();
        }

        private DataGridRow HitRow(FrameworkElement element)
        {
            try
            {
                DataGridRow row = DataGridRow.GetRowContainingElement(element);
                if (row != null)
                    return row;
            }
            catch
            {
            }
            try
            {
                DependencyObject current = element;
                while (current != null)
                {
                    DataGridRow row = current as DataGridRow;
                    if (row != null)
                        return row;
                    current = VisualTreeHelper.GetParent(current);
                }
            }
            catch
            {
            }
            return null;
        }

        private DataGridColumn HitColumn(FrameworkElement element, DataGridRow row)
        {
            try
            {
                DataGridColumn column = DataGridColumn.GetColumnContainingElement(element);
                if (column != null)
                    return column;
            }
            catch
            {
            }
            if (row != null)
            {
                foreach (DataGridColumn column in ServerDataGrid.Columns)
                {
                    try
                    {
                        FrameworkElement content = column.GetCellContent(row);
                        if (content != null && IsAncestorOrSelf(content, element))
                            return column;
                    }
                    catch
                    {
                    }
                }
            }
            return null;
        }

        private static bool IsAncestorOrSelf(FrameworkElement ancestor, FrameworkElement element)
        {
            try
            {
                DependencyObject current = element;
                while (current != null)
                {
                    if (ReferenceEquals(current, ancestor))
                        return true;
                    current = VisualTreeHelper.GetParent(current);
                }
            }
            catch
            {
            }
            return false;
        }

        private ServerLogRow ModelOf(DataGridRow row)
        {
            if (row == null)
                return null;
            return row.DataContext as ServerLogRow;
        }

        private void SelectRow(ServerLogRow row)
        {
            int index = _rows.IndexOf(row);
            if (index >= 0)
                ServerDataGrid.SelectedIndex = index;
        }

        private void ServerDataGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FrameworkElement element = e.OriginalSource as FrameworkElement;
            if (element == null)
                return;
            DataGridRow row = HitRow(element);
            DataGridColumn column = HitColumn(element, row);
            if (column == null)
                return;
            if (row == null)
            {
                SortByColumn(column);
                return;
            }
            ServerLogRow model = ModelOf(row);
            if (model == null)
                return;
            HandleCellClick(column, model);
        }

        private void HandleCellClick(DataGridColumn column, ServerLogRow row)
        {
            int id = row.Id;
            string name = ColumnNameOf(column);
            Configuration config = controller != null ? controller.GetCurrentConfiguration() : null;
            if (config != null && id >= 0 && id < config.configs.Count)
            {
                if (name == "Server")
                {
                    controller.SelectServerIndex(id);
                }
                if (name == "Group")
                {
                    Server cur_server = config.configs[id];
                    string group = cur_server.group;
                    if (!string.IsNullOrEmpty(group))
                    {
                        bool enable = !cur_server.enable;
                        foreach (Server server in config.configs)
                        {
                            if (server.group == group)
                            {
                                if (server.enable != enable)
                                {
                                    server.setEnable(enable);
                                }
                            }
                        }
                        controller.SelectServerIndex(config.index);
                    }
                }
                if (name == "Enable")
                {
                    Server server = config.configs[id];
                    server.setEnable(!server.isEnable());
                    controller.SelectServerIndex(config.index);
                }
            }
            SelectRow(row);
        }

        private void ServerDataGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            FrameworkElement element = e.OriginalSource as FrameworkElement;
            if (element == null)
                return;
            DataGridRow gridRow = HitRow(element);
            DataGridColumn column = HitColumn(element, gridRow);
            ServerLogRow row = ModelOf(gridRow);
            if (row == null || column == null)
                return;
            int id = row.Id;
            string name = ColumnNameOf(column);
            Configuration config = controller != null ? controller.GetCurrentConfiguration() : null;
            if (config == null || id < 0 || id >= config.configs.Count)
                return;
            if (name == "ID")
            {
                controller.ShowConfigForm(id);
            }
            if (name == "Server")
            {
                controller.ShowConfigForm(id);
            }
            if (name == "Connecting")
            {
                config.configs[id].GetConnections().CloseAll();
            }
            if (name == "MaxDownSpeed" || name == "MaxUpSpeed")
            {
                config.configs[id].ServerSpeedLog().ClearMaxSpeed();
            }
            if (name == "Upload" || name == "Download")
            {
                config.configs[id].ServerSpeedLog().ClearTrans();
            }
            if (name == "DownloadRaw")
            {
                config.configs[id].ServerSpeedLog().Clear();
                config.configs[id].setEnable(true);
            }
            if (name == "ConnectError"
                || name == "ConnectTimeout"
                || name == "ConnectEmpty"
                || name == "Continuous"
                )
            {
                config.configs[id].ServerSpeedLog().ClearError();
                config.configs[id].setEnable(true);
            }
            SelectRow(row);
        }

        private void ServerDataGrid_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            MenuFlyout menu = Resources["GridMenu"] as MenuFlyout;
            if (menu == null)
                return;
            try
            {
                menu.ShowAt(ServerDataGrid, e.GetPosition(ServerDataGrid));
            }
            catch
            {
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            string key = element != null ? element.Tag as string : null;
            if (string.IsNullOrEmpty(key))
                return;
            if (controller == null)
                return;
            switch (key)
            {
                case "&Disconnect direct connections":
                    DisconnectForward_Click();
                    break;
                case "Disconnect &All":
                    Disconnect_Click();
                    break;
                case "Clear &MaxSpeed":
                    ClearMaxSpeed_Click();
                    break;
                case "&Clear":
                    ClearItem_Click();
                    break;
                case "Clear &Selected Total":
                    ClearSelectedTotal_Click();
                    break;
                case "Clear &Total":
                    ClearTotal_Click();
                    break;
                case "Copy current link":
                    CopyLinkItem_Click();
                    break;
                case "Copy current group links":
                    CopyGroupLinkItem_Click();
                    break;
                case "Copy all enable links":
                    CopyEnableLinksItem_Click();
                    break;
                case "Copy all links":
                    CopyLinksItem_Click();
                    break;
                case "Auto &size":
                    autosizeItem_Click();
                    break;
                case "Always On &Top":
                    topmostItem_Click();
                    break;
            }
        }

        private void CopyLinkItem_Click()
        {
            Configuration config = controller.GetCurrentConfiguration();
            if (config != null && config.index >= 0 && config.index < config.configs.Count)
            {
                try
                {
                    string link = config.configs[config.index].GetSSRLinkForServer();
                    NativeUi.SetClipboardText(link);
                }
                catch { }
            }
        }

        private void CopyGroupLinkItem_Click()
        {
            Configuration config = controller.GetCurrentConfiguration();
            if (config != null && config.index >= 0 && config.index < config.configs.Count)
            {
                string group = config.configs[config.index].group;
                string link = "";
                for (int index = 0; index < config.configs.Count; ++index)
                {
                    if (config.configs[index].group != group)
                        continue;
                    link += config.configs[index].GetSSRLinkForServer() + "\r\n";
                }
                try
                {
                    NativeUi.SetClipboardText(link);
                }
                catch { }
            }
        }

        private void CopyEnableLinksItem_Click()
        {
            Configuration config = controller.GetCurrentConfiguration();
            if (config == null)
                return;
            string link = "";
            for (int index = 0; index < config.configs.Count; ++index)
            {
                if (!config.configs[index].enable)
                    continue;
                link += config.configs[index].GetSSRLinkForServer() + "\r\n";
            }
            try
            {
                NativeUi.SetClipboardText(link);
            }
            catch { }
        }

        private void CopyLinksItem_Click()
        {
            Configuration config = controller.GetCurrentConfiguration();
            if (config == null)
                return;
            string link = "";
            for (int index = 0; index < config.configs.Count; ++index)
            {
                link += config.configs[index].GetSSRLinkForServer() + "\r\n";
            }
            try
            {
                NativeUi.SetClipboardText(link);
            }
            catch { }
        }

        private void topmostItem_Click()
        {
            topmostItem = !topmostItem;
            SetWindowPos(GetWindowHandle(), topmostItem ? new IntPtr(-1) : new IntPtr(-2), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0004);
            SetMenuChecked(WindowMenuButton.Flyout as MenuFlyout, topmostItem);
            SetMenuChecked(Resources["GridMenu"] as MenuFlyout, topmostItem);
        }

        private static void SetMenuChecked(MenuFlyout menu, bool isChecked)
        {
            if (menu == null)
                return;
            foreach (MenuFlyoutItemBase item in menu.Items)
            {
                ToggleMenuFlyoutItem toggleItem = item as ToggleMenuFlyoutItem;
                if (toggleItem != null)
                    toggleItem.IsChecked = isChecked;
            }
        }

        private void DisconnectForward_Click()
        {
            Shadowsocks.Model.Server.GetForwardServerRef().GetConnections().CloseAll();
        }

        private void Disconnect_Click()
        {
            Configuration config = controller.GetCurrentConfiguration();
            if (config == null)
                return;
            for (int id = 0; id < config.configs.Count; ++id)
            {
                Server server = config.configs[id];
                server.GetConnections().CloseAll();
            }
            Shadowsocks.Model.Server.GetForwardServerRef().GetConnections().CloseAll();
        }

        private void ClearMaxSpeed_Click()
        {
            Configuration config = controller.GetCurrentConfiguration();
            if (config == null)
                return;
            foreach (Server server in config.configs)
            {
                server.ServerSpeedLog().ClearMaxSpeed();
            }
        }

        private void ClearSelectedTotal_Click()
        {
            Configuration config = controller.GetCurrentConfiguration();
            if (config != null && config.index >= 0 && config.index < config.configs.Count)
            {
                try
                {
                    controller.ClearTransferTotal(config.configs[config.index].server);
                }
                catch { }
            }
        }

        private void ClearTotal_Click()
        {
            Configuration config = controller.GetCurrentConfiguration();
            if (config == null)
                return;
            foreach (Server server in config.configs)
            {
                controller.ClearTransferTotal(server.server);
            }
        }

        private void ClearItem_Click()
        {
            Configuration config = controller.GetCurrentConfiguration();
            if (config == null)
                return;
            foreach (Server server in config.configs)
            {
                server.ServerSpeedLog().Clear();
            }
        }

        private void autosizeItem_Click()
        {
            autosizeColumns();
        }

        private void autosizeColumns()
        {
            foreach (DataGridColumn column in ServerDataGrid.Columns)
            {
                string name = ColumnNameOf(column);
                if (name == "AvgLatency"
                    || name == "AvgDownSpeed"
                    || name == "MaxDownSpeed"
                    || name == "AvgUpSpeed"
                    || name == "MaxUpSpeed"
                    || name == "Upload"
                    || name == "Download"
                    || name == "DownloadRaw"
                    || name == "Group"
                    || name == "Connecting"
                    || name == "ErrorPercent"
                    || name == "ConnectError"
                    || name == "ConnectTimeout"
                    || name == "Continuous"
                    || name == "ConnectEmpty"
                    )
                {
                    if (column.ActualWidth <= 2)
                        continue;
                    column.Width = DataGridLength.Auto;
                }
            }
            Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            if (dispatcherQueue == null)
                return;
            dispatcherQueue.TryEnqueue(() =>
            {
                foreach (DataGridColumn column in ServerDataGrid.Columns)
                {
                    string name = ColumnNameOf(column);
                    if (name == "AvgLatency"
                        || name == "Connecting"
                        || name == "AvgDownSpeed"
                        || name == "MaxDownSpeed"
                        || name == "AvgUpSpeed"
                        || name == "MaxUpSpeed"
                        )
                    {
                        column.MinWidth = column.ActualWidth;
                    }
                }
                FitWindowToColumns();
            });
        }

        private void FitWindowToColumns()
        {
            try
            {
                double total = 0;
                foreach (DataGridColumn column in ServerDataGrid.Columns)
                {
                    if (column.Visibility != Visibility.Visible)
                        continue;
                    total += column.ActualWidth;
                }
                IntPtr hwnd = GetWindowHandle();
                RECT windowRect;
                RECT clientRect;
                if (!GetWindowRect(hwnd, out windowRect) || !GetClientRect(hwnd, out clientRect))
                    return;
                double pageWidth = ActualWidth;
                if (pageWidth <= 0)
                    return;
                double neededPageWidth = total + GetSystemMetrics(2) + 24;
                int newWidth = (int)Math.Round((clientRect.Right - clientRect.Left) + (neededPageWidth - pageWidth));
                if (newWidth < 600)
                    newWidth = 600;
                SetWindowPos(hwnd, IntPtr.Zero, 0, 0, newWidth, windowRect.Bottom - windowRect.Top, 0x0001 | 0x0002 | 0x0004);
            }
            catch
            {
            }
        }

        private IntPtr GetWindowHandle()
        {
            if (windowHandle == IntPtr.Zero)
            {
                try
                {
                    windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(ShadowsocksR_winui3.MainWindow.Instance);
                }
                catch
                {
                    windowHandle = IntPtr.Zero;
                }
            }
            return windowHandle;
        }

        private void timer_Tick(DispatcherQueueTimer sender, object args)
        {
            if (updatePause > 0)
            {
                updatePause -= 1;
                return;
            }
            if (IsIconic(GetWindowHandle()))
            {
                if (!wasMinimized)
                {
                    wasMinimized = true;
                    Shadowsocks.Util.Utils.ReleaseMemory();
                }
                if (++pendingUpdate < 40)
                {
                    return;
                }
            }
            else
            {
                wasMinimized = false;
                ++updateTick;
            }
            pendingUpdate = 0;
            RefreshLog();
            UpdateLog();
            if (updateSize > 1)
                --updateSize;
            if (updateTick == 2 || updateSize == 1)
            {
                updateSize = 0;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);
    }
}
