using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.QrCode.Internal;

namespace Shadowsocks.View
{
    public class MenuViewController : IDisposable
    {
        private ShadowsocksController controller;
        private UpdateChecker updateChecker;
        private UpdateFreeNode updateFreeNodeChecker;
        private UpdateSubscribeManager updateSubscribeManager;

        private TrayIcon _trayIcon;
        private System.Timers.Timer timerDelayCheckUpdate;
        private string _urlToOpen;
        private bool _updateItemVisible;
        private string _updateItemText;
        private bool _disposed;

        private const int IDM_ENABLE = 0x1001;
        private const int IDM_PAC = 0x1002;
        private const int IDM_GLOBAL = 0x1003;
        private const int IDM_NOMODIFY = 0x1004;

        private const int IDM_PAC_LANIP = 0x1101;
        private const int IDM_PAC_CNWHITE = 0x1102;
        private const int IDM_PAC_CNIP = 0x1103;
        private const int IDM_PAC_GFW = 0x1104;
        private const int IDM_PAC_CNONLY = 0x1105;
        private const int IDM_PAC_COPYURL = 0x1106;
        private const int IDM_PAC_EDIT = 0x1107;
        private const int IDM_PAC_EDITUSER = 0x1108;

        private const int IDM_RULE_LAN = 0x1201;
        private const int IDM_RULE_CHINA = 0x1202;
        private const int IDM_RULE_NOTCHINA = 0x1203;
        private const int IDM_RULE_USER = 0x1204;
        private const int IDM_RULE_DISABLE = 0x1205;

        private const int IDM_SERVERS_EDIT = 0x1301;
        private const int IDM_SERVERS_IMPORT = 0x1302;
        private const int IDM_SAMEHOST = 0x1303;
        private const int IDM_SERVERS_STAT = 0x1304;
        private const int IDM_SERVERS_DISCONNECT = 0x1305;

        private const int IDM_SUB_SETTING = 0x1401;
        private const int IDM_SUB_UPDATE = 0x1402;
        private const int IDM_SUB_UPDATE_BYPASS = 0x1403;

        private const int IDM_RANDOM = 0x1501;
        private const int IDM_SETTINGS = 0x1502;
        private const int IDM_PORTMAP = 0x1503;
        private const int IDM_UPDATE = 0x1504;
        private const int IDM_EDIT_SERVERS = 0x1505;

        private const int IDM_SCAN_QR = 0x1601;
        private const int IDM_IMPORT_CLIP = 0x1602;

        private const int IDM_HELP_UPDATE = 0x1701;
        private const int IDM_HELP_LOG = 0x1702;
        private const int IDM_HELP_WIKI = 0x1703;
        private const int IDM_FEEDBACK = 0x1704;
        private const int IDM_HELP_GENQR = 0x1705;
        private const int IDM_HELP_RESET_PWD = 0x1706;
        private const int IDM_HELP_ABOUT = 0x1707;
        private const int IDM_HELP_DONATE = 0x1708;
        private const int IDM_ABOUT_PAGE = 0x1709;

        private const int IDM_QUIT = 0x1801;

        private const int IDM_SERVER_BASE = 0x2000;

        public MenuViewController(ShadowsocksController controller)
        {
            this.controller = controller;

            controller.ToggleModeChanged += controller_ToggleModeChanged;
            controller.ToggleRuleModeChanged += controller_ToggleRuleModeChanged;
            controller.ConfigChanged += controller_ConfigChanged;
            controller.PACFileReadyToOpen += controller_FileReadyToOpen;
            controller.UserRuleFileReadyToOpen += controller_FileReadyToOpen;
            controller.Errored += controller_Errored;
            controller.UpdatePACFromGFWListCompleted += controller_UpdatePACFromGFWListCompleted;
            controller.UpdatePACFromGFWListError += controller_UpdatePACFromGFWListError;
            controller.ShowConfigFormEvent += Config_Click;

            _trayIcon = new TrayIcon();
            _trayIcon.ContextMenuRequested += notifyIcon_ShowMenu;
            _trayIcon.Clicked += notifyIcon_Click;
            _trayIcon.BalloonClicked += notifyIcon1_BalloonTipClicked;
            _trayIcon.Create(I18N.GetString("ShadowsocksR"));
            UpdateTrayIcon();

            updateChecker = ShadowsocksR_winui3.App.UpdateChecker != null ? ShadowsocksR_winui3.App.UpdateChecker : new UpdateChecker();
            updateChecker.NewVersionFound += updateChecker_NewVersionFound;

            updateFreeNodeChecker = new UpdateFreeNode();
            updateFreeNodeChecker.NewFreeNodeFound += updateFreeNodeChecker_NewFreeNodeFound;

            updateSubscribeManager = new UpdateSubscribeManager();

            LoadCurrentConfiguration();

            Configuration cfg = controller.GetCurrentConfiguration();
            if (cfg.isDefaultConfig() || cfg.nodeFeedAutoUpdate)
            {
                updateSubscribeManager.CreateTask(controller.GetCurrentConfiguration(), updateFreeNodeChecker, -1, !cfg.isDefaultConfig());
            }

            timerDelayCheckUpdate = new System.Timers.Timer(1000.0 * 10);
            timerDelayCheckUpdate.Elapsed += timer_Elapsed;
            timerDelayCheckUpdate.Start();
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            if (timerDelayCheckUpdate != null)
            {
                timerDelayCheckUpdate.Elapsed -= timer_Elapsed;
                timerDelayCheckUpdate.Stop();
                timerDelayCheckUpdate = null;
            }
            try
            {
                controller.ToggleModeChanged -= controller_ToggleModeChanged;
                controller.ToggleRuleModeChanged -= controller_ToggleRuleModeChanged;
                controller.ConfigChanged -= controller_ConfigChanged;
                controller.PACFileReadyToOpen -= controller_FileReadyToOpen;
                controller.UserRuleFileReadyToOpen -= controller_FileReadyToOpen;
                controller.Errored -= controller_Errored;
                controller.UpdatePACFromGFWListCompleted -= controller_UpdatePACFromGFWListCompleted;
                controller.UpdatePACFromGFWListError -= controller_UpdatePACFromGFWListError;
                controller.ShowConfigFormEvent -= Config_Click;
            }
            catch
            {
            }
            if (_trayIcon != null)
            {
                _trayIcon.ContextMenuRequested -= notifyIcon_ShowMenu;
                _trayIcon.Clicked -= notifyIcon_Click;
                _trayIcon.BalloonClicked -= notifyIcon1_BalloonTipClicked;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
        }

        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (timerDelayCheckUpdate != null)
            {
                if (timerDelayCheckUpdate.Interval <= 1000.0 * 30)
                {
                    timerDelayCheckUpdate.Interval = 1000.0 * 60 * 5;
                }
                else
                {
                    timerDelayCheckUpdate.Interval = 1000.0 * 60 * 60 * 2;
                }
            }
            updateChecker.CheckUpdate(controller.GetCurrentConfiguration());
        }

        void controller_Errored(object sender, System.IO.ErrorEventArgs e)
        {
            NativeUi.MessageBox(e.GetException().ToString(),
                String.Format(I18N.GetString("Shadowsocks Error: {0}"), e.GetException().Message));
        }

        private void UpdateTrayIcon()
        {
            int dpi;
            using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                dpi = (int)graphics.DpiX;
            }
            Configuration config = controller.GetCurrentConfiguration();
            bool enabled = config.sysProxyMode != (int)ProxyMode.NoModify && config.sysProxyMode != (int)ProxyMode.Direct;
            bool global = config.sysProxyMode == (int)ProxyMode.Global;
            bool random = config.random;

            IntPtr hIcon = IntPtr.Zero;
            try
            {
                using (Bitmap icon = new Bitmap(Path.Combine(Util.Utils.StartupPath, "icon.png")))
                {
                    hIcon = icon.GetHicon();
                }
            }
            catch
            {
                Bitmap icon = null;
                if (dpi < 97)
                {
                    icon = Resources.ss16;
                }
                else if (dpi < 121)
                {
                    icon = Resources.ss20;
                }
                else
                {
                    icon = Resources.ss24;
                }
                double mul_a = 1.0, mul_r = 1.0, mul_g = 1.0, mul_b = 1.0;
                if (!enabled)
                {
                    mul_g = 0.4;
                }
                else if (!global)
                {
                    mul_b = 0.4;
                    mul_g = 0.8;
                }
                if (!random)
                {
                    mul_r = 0.4;
                }

                using (Bitmap iconCopy = new Bitmap(icon))
                {
                    for (int x = 0; x < iconCopy.Width; x++)
                    {
                        for (int y = 0; y < iconCopy.Height; y++)
                        {
                            Color color = icon.GetPixel(x, y);
                            iconCopy.SetPixel(x, y,
                                Color.FromArgb((byte)(color.A * mul_a),
                                ((byte)(color.R * mul_r)),
                                ((byte)(color.G * mul_g)),
                                ((byte)(color.B * mul_b))));
                        }
                    }
                    hIcon = iconCopy.GetHicon();
                }
            }
            _trayIcon.SetIcon(hIcon);

            string text = (enabled ?
                    I18N.GetString("System Proxy On: ") + (global ? I18N.GetString("Global") : I18N.GetString("PAC")) :
                    String.Format(I18N.GetString("Running: Port {0}"), config.localPort));
            _trayIcon.SetTooltip(text.Substring(0, Math.Min(63, text.Length)));
        }

        private void notifyIcon_ShowMenu()
        {
            ShowMenu();
        }

        private void notifyIcon_Click(TrayIcon.ClickKind kind)
        {
            if (kind == TrayIcon.ClickKind.Left)
            {
                int SCA_key = GetAsyncKeyState(0x10 /*VK_SHIFT*/) < 0 ? 1 : 0;
                SCA_key |= GetAsyncKeyState(0x11 /*VK_CONTROL*/) < 0 ? 2 : 0;
                SCA_key |= GetAsyncKeyState(0x12 /*VK_MENU*/) < 0 ? 4 : 0;
                if (SCA_key == 2)
                {
                    ShowServerLogForm();
                }
                else if (SCA_key == 1)
                {
                    ShowSettingForm();
                }
                else if (SCA_key == 4)
                {
                    ShowPortMapForm();
                }
                else
                {
                    ShowConfigForm(false);
                }
            }
            else if (kind == TrayIcon.ClickKind.Middle)
            {
                ShowServerLogForm();
            }
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private string T(string text)
        {
            return I18N.GetString(text);
        }

        private TrayMenuItem Item(int id, string text)
        {
            return TrayMenuItem.Item(id, T(text));
        }

        private TrayMenuItem Group(string text, params TrayMenuItem[] children)
        {
            return TrayMenuItem.Group(T(text), children);
        }

        private List<TrayMenuItem> BuildMenu()
        {
            Configuration config = controller.GetCurrentConfiguration();
            List<TrayMenuItem> servers = BuildServersMenu(config);

            List<TrayMenuItem> items = new List<TrayMenuItem>
            {
                Group("Mode",
                    Item(IDM_ENABLE, "Disable system proxy"),
                    Item(IDM_PAC, "PAC"),
                    Item(IDM_GLOBAL, "Global"),
                    TrayMenuItem.Separator(),
                    Item(IDM_NOMODIFY, "No modify system proxy")),
                Group("PAC ",
                    Item(IDM_PAC_LANIP, "Update local PAC from Lan IP list"),
                    TrayMenuItem.Separator(),
                    Item(IDM_PAC_CNWHITE, "Update local PAC from Chn White list"),
                    Item(IDM_PAC_CNIP, "Update local PAC from Chn IP list"),
                    Item(IDM_PAC_GFW, "Update local PAC from GFWList"),
                    TrayMenuItem.Separator(),
                    Item(IDM_PAC_CNONLY, "Update local PAC from Chn Only list"),
                    TrayMenuItem.Separator(),
                    Item(IDM_PAC_COPYURL, "Copy PAC URL"),
                    Item(IDM_PAC_EDIT, "Edit local PAC file..."),
                    Item(IDM_PAC_EDITUSER, "Edit user rule for GFWList...")),
                Group("Proxy rule",
                    Item(IDM_RULE_LAN, "Bypass LAN"),
                    Item(IDM_RULE_CHINA, "Bypass LAN && China"),
                    Item(IDM_RULE_NOTCHINA, "Bypass LAN && not China"),
                    Item(IDM_RULE_USER, "User custom"),
                    TrayMenuItem.Separator(),
                    Item(IDM_RULE_DISABLE, "Disable bypass")),
                TrayMenuItem.Separator(),
                Group("Servers", servers.ToArray()),
                Group("Servers Subscribe",
                    Item(IDM_SUB_SETTING, "Subscribe setting..."),
                    Item(IDM_SUB_UPDATE, "Update subscribe SSR node"),
                    Item(IDM_SUB_UPDATE_BYPASS, "Update subscribe SSR node(bypass proxy)")),
                Item(IDM_RANDOM, "Load balance"),
                Item(IDM_EDIT_SERVERS, "Edit server settings..."),
                Item(IDM_SETTINGS, "Global settings..."),
                Item(IDM_PORTMAP, "Port settings..."),
            };

            if (_updateItemVisible)
            {
                items.Add(TrayMenuItem.Item(IDM_UPDATE, _updateItemText));
            }

            items.Add(TrayMenuItem.Separator());
            items.Add(Item(IDM_SCAN_QR, "Scan QRCode from screen..."));
            items.Add(Item(IDM_IMPORT_CLIP, "Import SSR links from clipboard..."));
            items.Add(TrayMenuItem.Separator());
            items.Add(Item(IDM_ABOUT_PAGE, "About this software..."));
            items.Add(Item(IDM_QUIT, "Quit"));

            ApplyChecks(items, config);
            return items;
        }

        private void ApplyChecks(List<TrayMenuItem> items, Configuration config)
        {
            foreach (TrayMenuItem item in items)
            {
                if (item.Children != null && item.Children.Count > 0)
                {
                    ApplyChecks(item.Children, config);
                    continue;
                }
                switch (item.Id)
                {
                    case IDM_NOMODIFY:
                        item.Checked = config.sysProxyMode == (int)ProxyMode.NoModify;
                        break;
                    case IDM_ENABLE:
                        item.Checked = config.sysProxyMode == (int)ProxyMode.Direct;
                        break;
                    case IDM_PAC:
                        item.Checked = config.sysProxyMode == (int)ProxyMode.Pac;
                        break;
                    case IDM_GLOBAL:
                        item.Checked = config.sysProxyMode == (int)ProxyMode.Global;
                        break;
                    case IDM_RULE_DISABLE:
                        item.Checked = config.proxyRuleMode == (int)ProxyRuleMode.Disable;
                        break;
                    case IDM_RULE_LAN:
                        item.Checked = config.proxyRuleMode == (int)ProxyRuleMode.BypassLan;
                        break;
                    case IDM_RULE_CHINA:
                        item.Checked = config.proxyRuleMode == (int)ProxyRuleMode.BypassLanAndChina;
                        break;
                    case IDM_RULE_NOTCHINA:
                        item.Checked = config.proxyRuleMode == (int)ProxyRuleMode.BypassLanAndNotChina;
                        break;
                    case IDM_RULE_USER:
                        item.Checked = config.proxyRuleMode == (int)ProxyRuleMode.UserCustom;
                        break;
                    case IDM_RANDOM:
                        item.Checked = config.random;
                        break;
                    case IDM_SAMEHOST:
                        item.Checked = config.sameHostForSameTarget;
                        break;
                }
            }
        }

        private List<TrayMenuItem> BuildServersMenu(Configuration configuration)
        {
            const string def_group = "!(no group)";
            SortedDictionary<string, List<TrayMenuItem>> group = new SortedDictionary<string, List<TrayMenuItem>>();
            SortedDictionary<string, string> groupDisplay = new SortedDictionary<string, string>();
            string select_group = "";
            for (int i = 0; i < configuration.configs.Count; i++)
            {
                string group_name;
                Server server = configuration.configs[i];
                if (string.IsNullOrEmpty(server.group))
                    group_name = def_group;
                else
                    group_name = server.group;

                TrayMenuItem item = TrayMenuItem.Item(IDM_SERVER_BASE + i, server.FriendlyName());
                if (configuration.index == i)
                {
                    item.Checked = true;
                    select_group = group_name;
                }

                if (group.ContainsKey(group_name))
                {
                    group[group_name].Add(item);
                }
                else
                {
                    group[group_name] = new List<TrayMenuItem> { item };
                    groupDisplay[group_name] = group_name;
                }
            }

            List<TrayMenuItem> result = new List<TrayMenuItem>();
            foreach (KeyValuePair<string, List<TrayMenuItem>> pair in group)
            {
                string text = groupDisplay[pair.Key];
                if (pair.Key == def_group)
                {
                    text = "(empty group)";
                }
                if (pair.Key == select_group)
                {
                    text = "● " + text;
                }
                else
                {
                    text = "　" + text;
                }
                result.Add(TrayMenuItem.Group(text, pair.Value.ToArray()));
            }
            return result;
        }

        private void ShowMenu()
        {
            if (_trayIcon == null)
                return;
            int id = _trayIcon.ShowMenu(BuildMenu());
            if (id != 0)
                DispatchMenuCommand(id);
        }

        private void DispatchMenuCommand(int id)
        {
            if (id >= IDM_SERVER_BASE && id < IDM_SERVER_BASE + 16000)
            {
                controller.SelectServerIndex(id - IDM_SERVER_BASE);
                return;
            }
            switch (id)
            {
                case IDM_NOMODIFY: NoModifyItem_Click(); break;
                case IDM_ENABLE: EnableItem_Click(); break;
                case IDM_PAC: PACModeItem_Click(); break;
                case IDM_GLOBAL: GlobalModeItem_Click(); break;
                case IDM_PAC_LANIP: UpdatePACFromLanIPListItem_Click(); break;
                case IDM_PAC_CNWHITE: UpdatePACFromCNWhiteListItem_Click(); break;
                case IDM_PAC_CNIP: UpdatePACFromCNIPListItem_Click(); break;
                case IDM_PAC_GFW: UpdatePACFromGFWListItem_Click(); break;
                case IDM_PAC_CNONLY: UpdatePACFromCNOnlyListItem_Click(); break;
                case IDM_PAC_COPYURL: CopyPACURLItem_Click(); break;
                case IDM_PAC_EDIT: EditPACFileItem_Click(); break;
                case IDM_PAC_EDITUSER: EditUserRuleFileForGFWListItem_Click(); break;
                case IDM_RULE_LAN: RuleBypassLanItem_Click(); break;
                case IDM_RULE_CHINA: RuleBypassChinaItem_Click(); break;
                case IDM_RULE_NOTCHINA: RuleBypassNotChinaItem_Click(); break;
                case IDM_RULE_USER: RuleUserItem_Click(); break;
                case IDM_RULE_DISABLE: RuleBypassDisableItem_Click(); break;
                case IDM_SERVERS_EDIT: Config_Click(null, EventArgs.Empty); break;
                case IDM_SERVERS_IMPORT: Import_Click(); break;
                case IDM_SAMEHOST: SelectSameHostForSameTargetItem_Click(); break;
                case IDM_SERVERS_STAT: ShowServerLogItem_Click(); break;
                case IDM_SERVERS_DISCONNECT: DisconnectCurrent_Click(); break;
                case IDM_SUB_SETTING: SubscribeSetting_Click(); break;
                case IDM_SUB_UPDATE: CheckNodeUpdate_Click(); break;
                case IDM_SUB_UPDATE_BYPASS: CheckNodeUpdateBypassProxy_Click(); break;
                case IDM_RANDOM: SelectRandomItem_Click(); break;
                case IDM_EDIT_SERVERS: Config_Click(null, EventArgs.Empty); break;
                case IDM_SETTINGS: Setting_Click(); break;
                case IDM_PORTMAP: ShowPortMapItem_Click(); break;
                case IDM_UPDATE: UpdateItem_Clicked(); break;
                case IDM_SCAN_QR: ScanQRCodeItem_Click(); break;
                case IDM_IMPORT_CLIP: CopyAddress_Click(); break;
                case IDM_HELP_UPDATE: CheckUpdate_Click(); break;
                case IDM_HELP_LOG: ShowLogItem_Click(); break;
                case IDM_HELP_WIKI: OpenWiki_Click(); break;
                case IDM_FEEDBACK: FeedbackItem_Click(); break;
                case IDM_HELP_GENQR: showURLFromQRCode(); break;
                case IDM_HELP_RESET_PWD: ResetPasswordItem_Click(); break;
                case IDM_HELP_ABOUT: AboutItem_Click(); break;
                case IDM_HELP_DONATE: DonateItem_Click(); break;
                case IDM_ABOUT_PAGE: ShowAboutPage(); break;
                case IDM_QUIT: Quit_Click(); break;
            }
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
            UpdateTrayIcon();
        }

        private void controller_ToggleModeChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
            UpdateTrayIcon();
        }

        private void controller_ToggleRuleModeChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
        }

        void controller_FileReadyToOpen(object sender, ShadowsocksController.PathEventArgs e)
        {
            NativeUi.OpenFileInExplorer(e.Path);
        }

        void ShowBalloonTip(string title, string content, int icon, int timeout)
        {
            if (_trayIcon != null)
                _trayIcon.ShowBalloon(title, content, icon, timeout);
        }

        void controller_UpdatePACFromGFWListError(object sender, System.IO.ErrorEventArgs e)
        {
            ShowBalloonTip(I18N.GetString("Failed to update PAC file"), e.GetException().Message, 3, 5000);
            Logging.LogUsefulException(e.GetException());
        }

        void controller_UpdatePACFromGFWListCompleted(object sender, GFWListUpdater.ResultEventArgs e)
        {
            GFWListUpdater updater = (GFWListUpdater)sender;
            string result = e.Success ?
                (updater.update_type <= 1 ? I18N.GetString("PAC updated") : I18N.GetString("Domain white list list updated"))
                : I18N.GetString("No updates found. Please report to GFWList if you have problems with it.");
            ShowBalloonTip(I18N.GetString("Shadowsocks"), result, 1, 1000);
        }

        void updateFreeNodeChecker_NewFreeNodeFound(object sender, EventArgs e)
        {
            int count = 0;
            if (!String.IsNullOrEmpty(updateFreeNodeChecker.FreeNodeResult))
            {
                List<string> urls = new List<string>();
                updateFreeNodeChecker.FreeNodeResult = updateFreeNodeChecker.FreeNodeResult.TrimEnd('\r', '\n', ' ');
                Configuration config = controller.GetCurrentConfiguration();
                Server selected_server = null;
                if (config.index >= 0 && config.index < config.configs.Count)
                {
                    selected_server = config.configs[config.index];
                }
                try
                {
                    updateFreeNodeChecker.FreeNodeResult = Util.Base64.DecodeBase64(updateFreeNodeChecker.FreeNodeResult);
                }
                catch
                {
                    updateFreeNodeChecker.FreeNodeResult = "";
                }
                int max_node_num = 0;

                Match match_maxnum = Regex.Match(updateFreeNodeChecker.FreeNodeResult, "^MAX=([0-9]+)");
                if (match_maxnum.Success)
                {
                    try
                    {
                        max_node_num = Convert.ToInt32(match_maxnum.Groups[1].Value, 10);
                    }
                    catch
                    {
                    }
                }
                URL_Split(updateFreeNodeChecker.FreeNodeResult, ref urls);
                for (int i = urls.Count - 1; i >= 0; --i)
                {
                    if (!urls[i].StartsWith("ssr"))
                        urls.RemoveAt(i);
                }
                if (urls.Count > 0)
                {
                    bool keep_selected_server = false;
                    if (max_node_num <= 0 || max_node_num >= urls.Count)
                    {
                        urls.Reverse();
                    }
                    else
                    {
                        Random r = new Random();
                        Util.Utils.Shuffle(urls, r);
                        urls.RemoveRange(max_node_num, urls.Count - max_node_num);
                        if (!config.isDefaultConfig())
                            keep_selected_server = true;
                    }
                    string lastGroup = null;
                    string curGroup = null;
                    foreach (string url in urls)
                    {
                        try
                        {
                            Server server = new Server(url, null);
                            if (!String.IsNullOrEmpty(server.group))
                            {
                                curGroup = server.group;
                                break;
                            }
                        }
                        catch
                        { }
                    }
                    string subscribeURL = updateSubscribeManager.URL;
                    if (String.IsNullOrEmpty(curGroup))
                    {
                        curGroup = subscribeURL;
                    }
                    for (int i = 0; i < config.serverSubscribes.Count; ++i)
                    {
                        if (subscribeURL == config.serverSubscribes[i].URL)
                        {
                            lastGroup = config.serverSubscribes[i].Group;
                            config.serverSubscribes[i].Group = curGroup;
                            break;
                        }
                    }
                    if (lastGroup == null)
                    {
                        lastGroup = curGroup;
                    }

                    if (keep_selected_server && selected_server.group == curGroup)
                    {
                        bool match = false;
                        for (int i = 0; i < urls.Count; ++i)
                        {
                            try
                            {
                                Server server = new Server(urls[i], null);
                                if (selected_server.isMatchServer(server))
                                {
                                    match = true;
                                    break;
                                }
                            }
                            catch
                            { }
                        }
                        if (!match)
                        {
                            urls.RemoveAt(0);
                            urls.Add(selected_server.GetSSRLinkForServer());
                        }
                    }

                    {
                        Dictionary<string, Server> old_servers = new Dictionary<string, Server>();
                        if (!String.IsNullOrEmpty(lastGroup))
                        {
                            for (int i = config.configs.Count - 1; i >= 0; --i)
                            {
                                if (lastGroup == config.configs[i].group)
                                {
                                    old_servers[config.configs[i].id] = config.configs[i];
                                }
                            }
                        }
                        foreach (string url in urls)
                        {
                            try
                            {
                                Server server = new Server(url, curGroup);
                                bool match = false;
                                foreach (KeyValuePair<string, Server> pair in old_servers)
                                {
                                    if (server.isMatchServer(pair.Value))
                                    {
                                        match = true;
                                        old_servers.Remove(pair.Key);
                                        pair.Value.CopyServerInfo(server);
                                        ++count;
                                        break;
                                    }
                                }
                                if (!match)
                                {
                                    config.configs.Add(server);
                                    ++count;
                                }
                            }
                            catch
                            { }
                        }
                        foreach (KeyValuePair<string, Server> pair in old_servers)
                        {
                            for (int i = config.configs.Count - 1; i >= 0; --i)
                            {
                                if (config.configs[i].id == pair.Key)
                                {
                                    config.configs.RemoveAt(i);
                                    break;
                                }
                            }
                        }
                        controller.SaveServersConfig(config);
                    }
                    config = controller.GetCurrentConfiguration();
                    if (selected_server != null)
                    {
                        bool match = false;
                        for (int i = config.configs.Count - 1; i >= 0; --i)
                        {
                            if (config.configs[i].id == selected_server.id)
                            {
                                config.index = i;
                                match = true;
                                break;
                            }
                            else if (config.configs[i].group == selected_server.group)
                            {
                                if (config.configs[i].isMatchServer(selected_server))
                                {
                                    config.index = i;
                                    match = true;
                                    break;
                                }
                            }
                        }
                        if (!match)
                        {
                            config.index = config.configs.Count - 1;
                        }
                    }
                    else
                    {
                        config.index = config.configs.Count - 1;
                    }
                    controller.SaveServersConfig(config);
                }
            }
            if (count > 0)
            {
                ShowBalloonTip(I18N.GetString("Success"),
                    I18N.GetString("Update subscribe SSR node success"), 1, 10000);
            }
            else
            {
                ShowBalloonTip(I18N.GetString("Error"),
                    I18N.GetString("Update subscribe SSR node failure"), 1, 10000);
            }
            if (updateSubscribeManager.Next())
            {
            }
        }

        void updateChecker_NewVersionFound(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(updateChecker.LatestVersionNumber))
            {
                Logging.Log(LogLevel.Error, "connect to update server error");
            }
            else
            {
                if (!_updateItemVisible)
                {
                    ShowBalloonTip(String.Format(I18N.GetString("{0} {1} Update Found"), UpdateChecker.Name, updateChecker.LatestVersionNumber),
                        I18N.GetString("Click menu to download"), 1, 10000);
                    _trayIcon.BalloonClicked += notifyIcon1_BalloonTipClicked;

                    if (timerDelayCheckUpdate != null)
                    {
                        timerDelayCheckUpdate.Elapsed -= timer_Elapsed;
                        timerDelayCheckUpdate.Stop();
                        timerDelayCheckUpdate = null;
                    }
                }
                _updateItemVisible = true;
                _updateItemText = String.Format(I18N.GetString("New version {0} {1} available"), UpdateChecker.Name, updateChecker.LatestVersionNumber);
            }
        }

        void UpdateItem_Clicked()
        {
            NativeUi.OpenUrl(updateChecker.LatestVersionURL);
        }

        void notifyIcon1_BalloonTipClicked()
        {
            _trayIcon.BalloonClicked -= notifyIcon1_BalloonTipClicked;
        }

        private void LoadCurrentConfiguration()
        {
            // menu check states are computed from the live configuration when the menu is shown
        }

        private void ShowConfigForm(bool addNode)
        {
            var mainWindow = ShadowsocksR_winui3.MainWindow.Instance;
            if (mainWindow != null)
            {
                mainWindow.ShowPage("servers", addNode ? -1 : -2);
            }
        }

        private void ShowConfigForm(int index)
        {
            var mainWindow = ShadowsocksR_winui3.MainWindow.Instance;
            if (mainWindow != null)
            {
                mainWindow.ShowPage("servers", index);
            }
        }

        private void ShowSettingForm()
        {
            var mainWindow = ShadowsocksR_winui3.MainWindow.Instance;
            if (mainWindow != null)
                mainWindow.ShowPage("settings");
        }

        private void ShowPortMapForm()
        {
            var mainWindow = ShadowsocksR_winui3.MainWindow.Instance;
            if (mainWindow != null)
                mainWindow.ShowPage("portmap");
        }

        private void ShowServerLogForm()
        {
            var mainWindow = ShadowsocksR_winui3.MainWindow.Instance;
            if (mainWindow != null)
                mainWindow.ShowPage("serverlog");
        }

        private void ShowGlobalLogForm()
        {
            var mainWindow = ShadowsocksR_winui3.MainWindow.Instance;
            if (mainWindow != null)
                mainWindow.ShowPage("log");
        }

        private void ShowAboutPage()
        {
            var mainWindow = ShadowsocksR_winui3.MainWindow.Instance;
            if (mainWindow != null)
                mainWindow.ShowPage("about");
        }

        private void ShowSubscribeSettingForm()
        {
            var mainWindow = ShadowsocksR_winui3.MainWindow.Instance;
            if (mainWindow != null)
                mainWindow.ShowPage("subscribe");
        }

        private void Config_Click(object sender, EventArgs e)
        {
            if (sender != null && typeof(int) == sender.GetType())
            {
                ShowConfigForm((int)sender);
            }
            else
            {
                ShowConfigForm(false);
            }
        }

        private async void Import_Click()
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                var window = ShadowsocksR_winui3.MainWindow.Instance;
                if (window != null)
                {
                    IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
                }
                picker.FileTypeFilter.Add("*");
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    string name = file.Path;
                    Configuration cfg = Configuration.LoadFile(name);
                    if (cfg.configs.Count == 1 && cfg.configs[0].server == Configuration.GetDefaultServer().server)
                    {
                        NativeUi.MessageBox("Load config file failed", "ShadowsocksR");
                    }
                    else
                    {
                        controller.MergeConfiguration(cfg);
                        LoadCurrentConfiguration();
                        var mainWindow = ShadowsocksR_winui3.MainWindow.Instance;
                        if (mainWindow != null)
                            mainWindow.ShowPage("servers");
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.LogUsefulException(ex);
            }
        }

        private void Setting_Click()
        {
            ShowSettingForm();
        }

        private void Quit_Click()
        {
            Shadowsocks.Program.ExitApplication();
        }

        private void OpenWiki_Click()
        {
            NativeUi.OpenUrl("https://github.com/breakwa11/shadowsocks-rss/wiki");
        }

        private void FeedbackItem_Click()
        {
            NativeUi.OpenUrl("https://github.com/shadowsocksr/shadowsocksr-csharp/issues/new");
        }

        private void ResetPasswordItem_Click()
        {
            ShadowsocksR_winui3.Views.ResetPasswordWindow dlg = new ShadowsocksR_winui3.Views.ResetPasswordWindow();
            dlg.Activate();
        }

        private void AboutItem_Click()
        {
            NativeUi.OpenUrl("https://shadowsocksr-rm.github.io/breakwa11.github.io/index.html");
        }

        private void DonateItem_Click()
        {
            ShowBalloonTip(I18N.GetString("Donate"), I18N.GetString("Please contract to breakwa11 to get more infomation"), 1, 10000);
        }

        private void NoModifyItem_Click()
        {
            controller.ToggleMode(ProxyMode.NoModify);
        }

        private void EnableItem_Click()
        {
            controller.ToggleMode(ProxyMode.Direct);
        }

        private void GlobalModeItem_Click()
        {
            controller.ToggleMode(ProxyMode.Global);
        }

        private void PACModeItem_Click()
        {
            controller.ToggleMode(ProxyMode.Pac);
        }

        private void RuleBypassLanItem_Click()
        {
            controller.ToggleRuleMode((int)ProxyRuleMode.BypassLan);
        }

        private void RuleBypassChinaItem_Click()
        {
            controller.ToggleRuleMode((int)ProxyRuleMode.BypassLanAndChina);
        }

        private void RuleBypassNotChinaItem_Click()
        {
            controller.ToggleRuleMode((int)ProxyRuleMode.BypassLanAndNotChina);
        }

        private void RuleUserItem_Click()
        {
            controller.ToggleRuleMode((int)ProxyRuleMode.UserCustom);
        }

        private void RuleBypassDisableItem_Click()
        {
            controller.ToggleRuleMode((int)ProxyRuleMode.Disable);
        }

        private void SelectRandomItem_Click()
        {
            controller.ToggleSelectRandom(!controller.GetCurrentConfiguration().random);
        }

        private void SelectSameHostForSameTargetItem_Click()
        {
            controller.ToggleSameHostForSameTargetRandom(!controller.GetCurrentConfiguration().sameHostForSameTarget);
        }

        private void CopyPACURLItem_Click()
        {
            try
            {
                Configuration config = controller.GetCurrentConfiguration();
                string pacUrl;
                pacUrl = "http://127.0.0.1:" + config.localPort.ToString() + "/pac?" + "auth=" + config.localAuthPassword + "&t=" + Util.Utils.GetTimestamp(DateTime.Now);
                NativeUi.SetClipboardText(pacUrl);
            }
            catch
            {
            }
        }

        private void EditPACFileItem_Click()
        {
            controller.TouchPACFile();
        }

        private void UpdatePACFromGFWListItem_Click()
        {
            controller.UpdatePACFromGFWList();
        }

        private void UpdatePACFromLanIPListItem_Click()
        {
            controller.UpdatePACFromOnlinePac("https://raw.githubusercontent.com/shadowsocksr-rm/breakwa11.github.io/master/ssr/ss_lanip.pac");
        }

        private void UpdatePACFromCNWhiteListItem_Click()
        {
            controller.UpdatePACFromOnlinePac("https://raw.githubusercontent.com/shadowsocksr-rm/breakwa11.github.io/master/ssr/ss_white.pac");
        }

        private void UpdatePACFromCNOnlyListItem_Click()
        {
            controller.UpdatePACFromOnlinePac("https://raw.githubusercontent.com/shadowsocksr-rm/breakwa11.github.io/master/ssr/ss_white_r.pac");
        }

        private void UpdatePACFromCNIPListItem_Click()
        {
            controller.UpdatePACFromOnlinePac("https://raw.githubusercontent.com/shadowsocksr-rm/breakwa11.github.io/master/ssr/ss_cnip.pac");
        }

        private void EditUserRuleFileForGFWListItem_Click()
        {
            controller.TouchUserRuleFile();
        }

        private void CheckUpdate_Click()
        {
            updateChecker.CheckUpdate(controller.GetCurrentConfiguration());
        }

        private void CheckNodeUpdate_Click()
        {
            updateSubscribeManager.CreateTask(controller.GetCurrentConfiguration(), updateFreeNodeChecker, -1, true);
        }

        private void CheckNodeUpdateBypassProxy_Click()
        {
            updateSubscribeManager.CreateTask(controller.GetCurrentConfiguration(), updateFreeNodeChecker, -1, false);
        }

        private void ShowLogItem_Click()
        {
            ShowGlobalLogForm();
        }

        private void ShowPortMapItem_Click()
        {
            ShowPortMapForm();
        }

        private void ShowServerLogItem_Click()
        {
            ShowServerLogForm();
        }

        private void SubscribeSetting_Click()
        {
            ShowSubscribeSettingForm();
        }

        private void DisconnectCurrent_Click()
        {
            Configuration config = controller.GetCurrentConfiguration();
            for (int id = 0; id < config.configs.Count; ++id)
            {
                Server server = config.configs[id];
                server.GetConnections().CloseAll();
            }
        }

        private void URL_Split(string text, ref List<string> out_urls)
        {
            if (String.IsNullOrEmpty(text))
            {
                return;
            }
            int ss_index = text.IndexOf("ss://", 1, StringComparison.OrdinalIgnoreCase);
            int ssr_index = text.IndexOf("ssr://", 1, StringComparison.OrdinalIgnoreCase);
            int index = ss_index;
            if (index == -1 || index > ssr_index && ssr_index != -1) index = ssr_index;
            if (index == -1)
            {
                out_urls.Insert(0, text);
            }
            else
            {
                out_urls.Insert(0, text.Substring(0, index));
                URL_Split(text.Substring(index), ref out_urls);
            }
        }

        private void CopyAddress_Click()
        {
            try
            {
                string text = NativeUi.GetClipboardText();
                if (!String.IsNullOrEmpty(text))
                {
                    List<string> urls = new List<string>();
                    URL_Split(text, ref urls);
                    int count = 0;
                    foreach (string url in urls)
                    {
                        if (controller.AddServerBySSURL(url))
                            ++count;
                    }
                    if (count > 0)
                        ShowConfigForm(true);
                }
            }
            catch
            {
            }
        }

        private bool ScanQRCode(Bitmap fullImage, Rectangle cropRect, out string url, out Rectangle rect)
        {
            using (Bitmap target = new Bitmap(cropRect.Width, cropRect.Height))
            {
                using (Graphics g = Graphics.FromImage(target))
                {
                    g.DrawImage(fullImage, new Rectangle(0, 0, cropRect.Width, cropRect.Height),
                                    cropRect,
                                    System.Drawing.GraphicsUnit.Pixel);
                }
                var source = new BitmapLuminanceSource(target);
                var bitmap = new BinaryBitmap(new HybridBinarizer(source));
                QRCodeReader reader = new QRCodeReader();
                var result = reader.decode(bitmap);
                if (result != null)
                {
                    url = result.Text;
                    double minX = Int32.MaxValue, minY = Int32.MaxValue, maxX = 0, maxY = 0;
                    foreach (ResultPoint point in result.ResultPoints)
                    {
                        minX = Math.Min(minX, point.X);
                        minY = Math.Min(minY, point.Y);
                        maxX = Math.Max(maxX, point.X);
                        maxY = Math.Max(maxY, point.Y);
                    }
                    rect = new Rectangle(cropRect.Left + (int)minX, cropRect.Top + (int)minY, (int)(maxX - minX), (int)(maxY - minY));
                    return true;
                }
            }
            url = "";
            rect = new Rectangle();
            return false;
        }

        private bool ScanQRCodeStretch(Bitmap fullImage, Rectangle cropRect, double mul, out string url, out Rectangle rect)
        {
            using (Bitmap target = new Bitmap((int)(cropRect.Width * mul), (int)(cropRect.Height * mul)))
            {
                using (Graphics g = Graphics.FromImage(target))
                {
                    g.DrawImage(fullImage, new Rectangle(0, 0, target.Width, target.Height),
                                    cropRect,
                                    System.Drawing.GraphicsUnit.Pixel);
                }
                var source = new BitmapLuminanceSource(target);
                var bitmap = new BinaryBitmap(new HybridBinarizer(source));
                QRCodeReader reader = new QRCodeReader();
                var result = reader.decode(bitmap);
                if (result != null)
                {
                    url = result.Text;
                    double minX = Int32.MaxValue, minY = Int32.MaxValue, maxX = 0, maxY = 0;
                    foreach (ResultPoint point in result.ResultPoints)
                    {
                        minX = Math.Min(minX, point.X);
                        minY = Math.Min(minY, point.Y);
                        maxX = Math.Max(maxX, point.X);
                        maxY = Math.Max(maxY, point.Y);
                    }
                    rect = new Rectangle(cropRect.Left + (int)(minX / mul), cropRect.Top + (int)(minY / mul), (int)((maxX - minX) / mul), (int)((maxY - minY) / mul));
                    return true;
                }
            }
            url = "";
            rect = new Rectangle();
            return false;
        }

        private Rectangle GetScanRect(int width, int height, int index, out double stretch)
        {
            stretch = 1;
            if (index < 5)
            {
                const int div = 5;
                int w = width * 3 / div;
                int h = height * 3 / div;
                Point[] pt = new Point[5] {
                    new Point(1, 1),

                    new Point(0, 0),
                    new Point(0, 2),
                    new Point(2, 0),
                    new Point(2, 2),
                };
                return new Rectangle(pt[index].X * width / div, pt[index].Y * height / div, w, h);
            }
            {
                const int base_index = 5;
                if (index < base_index + 6)
                {
                    double[] s = new double[] {
                        1,
                        2,
                        3,
                        4,
                        6,
                        8
                    };
                    stretch = 1 / s[index - base_index];
                    return new Rectangle(0, 0, width, height);
                }
            }
            {
                const int base_index = 11;
                if (index < base_index + 8)
                {
                    const int hdiv = 7;
                    const int vdiv = 5;
                    int w = width * 3 / hdiv;
                    int h = height * 3 / vdiv;
                    Point[] pt = new Point[8] {
                        new Point(1, 1),
                        new Point(3, 1),

                        new Point(0, 0),
                        new Point(0, 2),

                        new Point(2, 0),
                        new Point(2, 2),

                        new Point(4, 0),
                        new Point(4, 2),
                    };
                    return new Rectangle(pt[index - base_index].X * width / hdiv, pt[index - base_index].Y * height / vdiv, w, h);
                }
            }
            return new Rectangle(0, 0, 0, 0);
        }

        private void ScanScreenQRCode(bool ss_only)
        {
            Thread.Sleep(100);
            foreach (Rectangle screen in Util.Utils.GetScreens())
            {
                Point screen_size = Util.Utils.GetScreenPhysicalSize();
                using (Bitmap fullImage = new Bitmap(screen_size.X, screen_size.Y))
                {
                    using (Graphics g = Graphics.FromImage(fullImage))
                    {
                        g.CopyFromScreen(screen.X,
                                         screen.Y,
                                         0, 0,
                                         fullImage.Size,
                                         System.Drawing.CopyPixelOperation.SourceCopy);
                    }
                    bool decode_fail = false;
                    for (int i = 0; i < 100; i++)
                    {
                        double stretch;
                        Rectangle cropRect = GetScanRect(fullImage.Width, fullImage.Height, i, out stretch);
                        if (cropRect.Width == 0)
                            break;

                        string url;
                        Rectangle rect;
                        if (stretch == 1 ? ScanQRCode(fullImage, cropRect, out url, out rect) : ScanQRCodeStretch(fullImage, cropRect, stretch, out url, out rect))
                        {
                            var success = controller.AddServerBySSURL(url);
                            QrSplashWindow splash = new QrSplashWindow();
                            if (success)
                            {
                                splash.Closed += splash_FormClosed;
                            }
                            else if (!ss_only)
                            {
                                _urlToOpen = url;
                                splash.Closed += showURLFromQRCode;
                            }
                            else
                            {
                                decode_fail = true;
                                continue;
                            }
                            double dpi = Util.Utils.GetPrimaryScreenWidth() / (double)screen_size.X;
                            Rectangle targetRect = new Rectangle(
                                (int)(rect.Left * dpi + screen.X),
                                (int)(rect.Top * dpi + screen.Y),
                                (int)(rect.Width * dpi),
                                (int)(rect.Height * dpi));
                            splash.Show(screen, targetRect);
                            return;
                        }
                    }
                    if (decode_fail)
                    {
                        NativeUi.MessageBox(I18N.GetString("Failed to decode QRCode"));
                        return;
                    }
                }
            }
            NativeUi.MessageBox(I18N.GetString("No QRCode found. Try to zoom in or move it to the center of the screen."));
        }

        private void ScanQRCodeItem_Click()
        {
            ScanScreenQRCode(false);
        }

        void splash_FormClosed()
        {
            ShowConfigForm(true);
        }

        void showURLFromQRCode()
        {
            ShadowsocksR_winui3.Views.ShowTextWindow dlg = new ShadowsocksR_winui3.Views.ShowTextWindow("QRCode", _urlToOpen);
            dlg.Activate();
        }

        void showURLFromQRCode(object sender, EventArgs e)
        {
            showURLFromQRCode();
        }
    }
}
