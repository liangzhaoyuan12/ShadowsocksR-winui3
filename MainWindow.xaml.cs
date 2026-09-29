using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Shadowsocks.Controller;
using System;
using System.Collections.Generic;

namespace ShadowsocksR_winui3
{
    public interface IPageController
    {
        void OnShow(int arg);
    }

    public sealed partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; }

        private string _currentKey;
        private bool _navigating;

        public MainWindow()
        {
            this.InitializeComponent();
            Instance = this;
            NavView.IsPaneOpen = true;
            NavView.Loaded += (s, e) =>
            {
                NavView.PaneDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode.Left;
                NavView.IsPaneOpen = true;
                ApplyWindowIcon();
            };

            NavItemServers.Content = I18N.GetString("Edit servers...");
            NavItemSettings.Content = I18N.GetString("Global settings...");
            NavItemPortMap.Content = I18N.GetString("Port settings...");
            NavItemSubscribe.Content = I18N.GetString("Subscribe setting...");
            NavItemServerLog.Content = I18N.GetString("Server statistic...");
            NavItemLog.Content = I18N.GetString("Show logs...");
            NavItemAbout.Content = I18N.GetString("About");

            foreach (object item in AllNavItems())
            {
                NavigationViewItem navItem = item as NavigationViewItem;
                if (navItem != null)
                    ToolTipService.SetToolTip(navItem, navItem.Content as string ?? "");
            }

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            AppWindow.TitleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
            AppWindow.TitleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
            ApplyWindowIcon();
            this.Activated += (s, e) => ApplyWindowIcon();
            Microsoft.UI.Dispatching.DispatcherQueue dq = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            dq.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                var timer = dq.CreateTimer();
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.Tick += (s2, e2) =>
                {
                    timer.Stop();
                    ApplyWindowIcon();
                };
                timer.Start();
            });

            AppWindow.Closing += AppWindow_Closing;
        }

        private IntPtr _iconSmall;
        private IntPtr _iconBig;
        private bool _iconsExtracted;

        private void ApplyWindowIcon()
        {
            try
            {
                if (!_iconsExtracted)
                {
                    string iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "shadowsocks.ico");
                    if (System.IO.File.Exists(iconPath))
                    {
                        IntPtr hBig = IntPtr.Zero;
                        IntPtr hSmall = IntPtr.Zero;
                        if (ExtractIconEx(iconPath, 0, out hBig, out hSmall, 1) > 0)
                        {
                            _iconBig = hBig;
                            _iconSmall = hSmall;
                            _iconsExtracted = true;
                        }
                    }
                }
                if (!_iconsExtracted)
                    return;
                IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                if (_iconSmall != IntPtr.Zero)
                    SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_SMALL, _iconSmall);
                if (_iconBig != IntPtr.Zero)
                    SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_BIG, _iconBig);
            }
            catch
            {
            }
        }

        private const uint WM_SETICON = 0x0080;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern uint ExtractIconEx(string lpszFile, int nIconIndex, out IntPtr phiconLarge, out IntPtr phiconSmall, int nIcons);

        private void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
        {
            // close button hides to tray; the app stays alive until Quit
            args.Cancel = true;
            AppWindow.Hide();
        }

        public void ShowAndActivate()
        {
            AppWindow.Show();
            Activate();
        }

        public void HideWindow()
        {
            AppWindow.Hide();
        }

        public void ShowPage(string key, int arg = int.MinValue)
        {
            ShowAndActivate();
            NavigateTo(key, arg);
        }

        private Type GetPageType(string key)
        {
            switch (key)
            {
                case "servers":
                    return typeof(Views.ServersPage);
                case "settings":
                    return typeof(Views.SettingsPage);
                case "portmap":
                    return typeof(Views.PortMapPage);
                case "subscribe":
                    return typeof(Views.SubscribePage);
                case "serverlog":
                    return typeof(Views.ServerLogPage);
                case "log":
                    return typeof(Views.LogPage);
                case "about":
                    return typeof(Views.AboutPage);
                default:
                    return null;
            }
        }

        private IEnumerable<object> AllNavItems()
        {
            foreach (object item in NavView.MenuItems)
                yield return item;
            foreach (object item in NavView.FooterMenuItems)
                yield return item;
        }

        private void NavigateTo(string key, int arg)
        {
            Type pageType = GetPageType(key);
            if (pageType == null)
                return;
            _currentKey = key;
            _navigating = true;
            foreach (var item in AllNavItems())
            {
                NavigationViewItem navItem = item as NavigationViewItem;
                if (navItem != null && (string)navItem.Tag == key)
                {
                    NavView.SelectedItem = navItem;
                    break;
                }
            }
            _navigating = false;
            ContentFrame.Navigate(pageType, null, new EntranceNavigationTransitionInfo());
            ContentFrame.BackStack.Clear();
            IPageController controller = ContentFrame.Content as IPageController;
            if (controller != null)
                controller.OnShow(arg);
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (_navigating)
                return;
            NavigationViewItem item = args.SelectedItem as NavigationViewItem;
            if (item == null)
                return;
            string key = (string)item.Tag;
            if (key == null)
                return;
            NavigateTo(key, int.MinValue);
        }
    }
}
