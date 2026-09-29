using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Shadowsocks.View
{
    public class TrayMenuItem
    {
        public int Id;
        public string Text;
        public bool IsSeparator;
        public bool Checked;
        public bool Enabled = true;
        public bool Visible = true;
        public List<TrayMenuItem> Children;

        public static TrayMenuItem Separator()
        {
            return new TrayMenuItem { IsSeparator = true };
        }

        public static TrayMenuItem Item(int id, string text, bool isChecked = false)
        {
            return new TrayMenuItem { Id = id, Text = text, Checked = isChecked };
        }

        public static TrayMenuItem Group(string text, params TrayMenuItem[] children)
        {
            return new TrayMenuItem
            {
                Id = -1,
                Text = text,
                Children = new List<TrayMenuItem>(children),
            };
        }
    }

    /// <summary>
    /// Shell notification area icon with a Win32 popup menu,
    /// replacing System.Windows.Forms.NotifyIcon/ContextMenu.
    /// </summary>
    public sealed class TrayIcon : IDisposable
    {
        public const int WM_CONTEXTMENU = 0x007B;
        public const int WM_LBUTTONUP = 0x0202;
        public const int WM_RBUTTONUP = 0x0205;
        public const int WM_MBUTTONUP = 0x0209;
        public const int WM_LBUTTONDBLCLK = 0x0203;
        public const int NIN_SELECT = 0x0400;
        public const int NIN_KEYSELECT = 0x0401;
        public const int NIN_BALLOONUSERCLICK = 0x0405;
        public const int NIN_BALLOONTIMEOUT = 0x0404;

        public enum ClickKind
        {
            Left,
            Middle,
        }

        public event Action<ClickKind> Clicked;
        public event Action BalloonClicked;

        private const uint NIM_ADD = 0x00000000;
        private const uint NIM_MODIFY = 0x00000001;
        private const uint NIM_DELETE = 0x00000002;
        private const uint NIF_MESSAGE = 0x00000001;
        private const uint NIF_ICON = 0x00000002;
        private const uint NIF_TIP = 0x00000004;
        private const uint NIF_INFO = 0x00000010;
        private const uint NIF_SHOWTIP = 0x00000080;
        private const int NIIF_NONE = 0;
        private const int NIIF_INFO = 1;
        private const int NIIF_WARNING = 2;
        private const int NIIF_ERROR = 3;

        private const uint TPM_RETURNCMD = 0x0100;
        private const uint TPM_RIGHTBUTTON = 0x0001;
        private const uint TPM_NONOTIFY = 0x0080;

        private const int MF_STRING = 0x00000000;
        private const int MF_GRAYED = 0x00000001;
        private const int MF_CHECKED = 0x00000008;
        private const int MF_POPUP = 0x00000010;
        private const int MF_SEPARATOR = 0x00000800;
        private const int MF_BYPOSITION = 0x00000400;

        private const int GWLP_WNDPROC = -4;
        private static readonly uint TaskbarCreatedMessage = RegisterWindowMessage("TaskbarCreated");

        private IntPtr _hwnd;
        private IntPtr _origProc;
        private bool _endSessionQueryReceived;
        private bool _endSessionCleanupDone;
        private IntPtr _hIcon;
        private string _tooltip = "";
        private bool _created;
        private WndProc _wndProc; // keep reference to avoid GC of the delegate
        private ushort _classAtom;

        private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NOTIFYICONDATA
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uID;
            public uint uFlags;
            public uint uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
            public uint dwState;
            public uint dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;
            public uint uTimeoutOrVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;
            public uint dwInfoFlags;
            public Guid guidItem;
            public IntPtr hBalloonIcon;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WNDCLASSEX
        {
            public uint cbSize;
            public uint style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        public IntPtr Handle
        {
            get { return _hwnd; }
        }

        public void Create(string tooltip)
        {
            if (_created)
                return;
            _tooltip = tooltip ?? "";

            _wndProc = WndProcImpl;
            IntPtr module = GetModuleHandle(null);
            string className = "ShadowsocksRTrayWindow_" + Guid.NewGuid().ToString("N");
            WNDCLASSEX cls = new WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEX)),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
                hInstance = module,
                lpszClassName = className,
            };
            _classAtom = RegisterClassEx(ref cls);
            int regErr = _classAtom == 0 ? Marshal.GetLastWin32Error() : 0;
            _hwnd = CreateWindowEx(0, className, "", 0, 0, 0, 0, 0,
                IntPtr.Zero, IntPtr.Zero, module, IntPtr.Zero);
            if (_hwnd == IntPtr.Zero)
            {
                int createErr = Marshal.GetLastWin32Error();
                _hwnd = CreateWindowEx(0, "STATIC", "ShadowsocksRTrayFallback", 0, 0, 0, 0, 0,
                    IntPtr.Zero, IntPtr.Zero, module, IntPtr.Zero);
                if (_hwnd != IntPtr.Zero)
                {
                    _origProc = SetWindowLongPtr(_hwnd, GWLP_WNDPROC,
                        Marshal.GetFunctionPointerForDelegate(_wndProc));
                }
                else
                {
                    int fallbackErr = Marshal.GetLastWin32Error();
                    throw new InvalidOperationException(string.Format(
                        "Failed to create tray message window (atom={0} regErr={1} createErr={2} fallbackErr={3})",
                        _classAtom, regErr, createErr, fallbackErr));
                }
            }

            _created = true;
            AddIcon();
        }

        private void AddIcon()
        {
            NOTIFYICONDATA data = BuildData();
            data.uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP | NIF_SHOWTIP;
            Shell_NotifyIcon(NIM_ADD, ref data);
        }

        private NOTIFYICONDATA BuildData()
        {
            NOTIFYICONDATA data = new NOTIFYICONDATA();
            data.cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONDATA));
            data.hWnd = _hwnd;
            data.uID = 1;
            data.uCallbackMessage = 0x0400 + 77; // WM_APP + 77
            data.hIcon = _hIcon;
            data.szTip = _tooltip;
            return data;
        }

        public void SetIcon(IntPtr hIcon)
        {
            if (hIcon == IntPtr.Zero)
                return;
            _hIcon = hIcon;
            if (!_created)
                return;
            NOTIFYICONDATA data = BuildData();
            data.uFlags = NIF_ICON | NIF_TIP | NIF_SHOWTIP;
            Shell_NotifyIcon(NIM_MODIFY, ref data);
        }

        public void SetTooltip(string tooltip)
        {
            _tooltip = tooltip ?? "";
            if (!_created)
                return;
            NOTIFYICONDATA data = BuildData();
            data.uFlags = NIF_TIP | NIF_SHOWTIP;
            Shell_NotifyIcon(NIM_MODIFY, ref data);
        }

        public void ShowBalloon(string title, string content, int iconType, int timeoutMs)
        {
            if (!_created)
                return;
            NOTIFYICONDATA data = BuildData();
            data.uFlags = NIF_INFO | NIF_SHOWTIP;
            data.uTimeoutOrVersion = (uint)timeoutMs;
            data.szInfoTitle = Truncate(title, 63);
            data.szInfo = Truncate(content, 255);
            data.dwInfoFlags = (uint)iconType;
            Shell_NotifyIcon(NIM_MODIFY, ref data);
        }

        private static string Truncate(string s, int max)
        {
            if (s == null)
                return "";
            return s.Length <= max ? s : s.Substring(0, max);
        }

        /// <summary>
        /// Builds the popup menu from the given items and shows it at the cursor.
        /// Returns the selected command id, or 0 when cancelled.
        /// </summary>
        public int ShowMenu(List<TrayMenuItem> items)
        {
            if (!_created)
                return 0;
            POINT pt;
            GetCursorPos(out pt);
            IntPtr hMenu = BuildMenu(items);
            if (hMenu == IntPtr.Zero)
                return 0;
            try
            {
                SetForegroundWindow(_hwnd);
                int id = (int)TrackPopupMenuEx(hMenu, TPM_RETURNCMD | TPM_RIGHTBUTTON | TPM_NONOTIFY,
                    pt.x, pt.y, _hwnd, IntPtr.Zero);
                PostMessage(_hwnd, 0x0000 /*WM_NULL*/, IntPtr.Zero, IntPtr.Zero);
                return id;
            }
            finally
            {
                DestroyMenu(hMenu);
            }
        }

        private IntPtr BuildMenu(List<TrayMenuItem> items)
        {
            IntPtr hMenu = CreatePopupMenu();
            AppendItems(hMenu, items);
            return hMenu;
        }

        private void AppendItems(IntPtr hMenu, List<TrayMenuItem> items)
        {
            if (items == null)
                return;
            foreach (TrayMenuItem item in items)
            {
                if (item == null || !item.Visible)
                    continue;
                if (item.IsSeparator)
                {
                    AppendMenu(hMenu, MF_SEPARATOR, UIntPtr.Zero, null);
                    continue;
                }
                if (item.Children != null && item.Children.Count > 0)
                {
                    IntPtr hSub = CreatePopupMenu();
                    AppendItems(hSub, item.Children);
                    AppendMenu(hMenu, MF_POPUP | MF_BYPOSITION, (UIntPtr)hSub.ToInt64(), item.Text ?? "");
                    continue;
                }
                uint flags = MF_STRING | MF_BYPOSITION;
                if (item.Checked)
                    flags |= MF_CHECKED;
                if (!item.Enabled)
                    flags |= MF_GRAYED;
                AppendMenu(hMenu, flags, (UIntPtr)(uint)item.Id, item.Text ?? "");
            }
        }

        private IntPtr WndProcImpl(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == TaskbarCreatedMessage)
            {
                // explorer restarted: re-add the icon
                if (_created)
                    AddIcon();
                return IntPtr.Zero;
            }
            if (msg == 0x0011 /* WM_QUERYENDSESSION */)
            {
                // shutdown/logoff broadcast phase: the process is still alive here,
                // so synchronously turn off our loopback system proxy to guarantee
                // networking keeps working after the machine turns off / reboots
                if (!_endSessionCleanupDone)
                {
                    _endSessionCleanupDone = true;
                    _endSessionQueryReceived = true;
                    try
                    {
                        Shadowsocks.Controller.SystemProxy.DisableLoopbackProxyIfEnabled(true);
                    }
                    catch
                    {
                    }
                }
            }
            else if (msg == 0x0016 /* WM_ENDSESSION */)
            {
                if (wParam == IntPtr.Zero)
                {
                    // shutdown was cancelled by another application: restore the proxy
                    if (_endSessionQueryReceived)
                    {
                        _endSessionQueryReceived = false;
                        _endSessionCleanupDone = false;
                        try
                        {
                            var controller = ShadowsocksR_winui3.App.Controller;
                            if (controller != null)
                            {
                                Shadowsocks.Controller.SystemProxy.Update(
                                    controller.GetCurrentConfiguration(), false);
                            }
                        }
                        catch
                        {
                        }
                    }
                }
                else if (!_endSessionCleanupDone)
                {
                    _endSessionCleanupDone = true;
                    try
                    {
                        Shadowsocks.Controller.SystemProxy.DisableLoopbackProxyIfEnabled(true);
                    }
                    catch
                    {
                    }
                }
            }
            if (msg == 0x0400 + 77 || msg == WM_CONTEXTMENU)
            {
                int notify = LowWord(lParam);
                if (notify == 0)
                    notify = (int)lParam;
                switch (notify)
                {
                    case WM_LBUTTONUP:
                    case NIN_SELECT:
                    case NIN_KEYSELECT:
                        var leftHandler = Clicked;
                        if (leftHandler != null)
                            leftHandler(ClickKind.Left);
                        return IntPtr.Zero;
                    case WM_LBUTTONDBLCLK:
                        var leftHandler2 = Clicked;
                        if (leftHandler2 != null)
                            leftHandler2(ClickKind.Left);
                        return IntPtr.Zero;
                    case WM_MBUTTONUP:
                        var midHandler = Clicked;
                        if (midHandler != null)
                            midHandler(ClickKind.Middle);
                        return IntPtr.Zero;
                    case WM_RBUTTONUP:
                    case WM_CONTEXTMENU:
                        ShowContextMenuRequested();
                        return IntPtr.Zero;
                    case NIN_BALLOONUSERCLICK:
                        var balloonHandler = BalloonClicked;
                        if (balloonHandler != null)
                            balloonHandler();
                        return IntPtr.Zero;
                }
                // legacy layout: whole lParam is the message
                if ((int)lParam == WM_RBUTTONUP)
                {
                    ShowContextMenuRequested();
                    return IntPtr.Zero;
                }
            }
            if (_origProc != IntPtr.Zero)
                return CallWindowProc(_origProc, hWnd, msg, wParam, lParam);
            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        /// <summary>Raised when the user requests the context menu (right click).</summary>
        public event Action ContextMenuRequested;

        private void ShowContextMenuRequested()
        {
            var handler = ContextMenuRequested;
            if (handler != null)
                handler();
        }

        private static int LowWord(IntPtr value)
        {
            return (int)((long)value & 0xFFFF);
        }

        public void Dispose()
        {
            if (_created)
            {
                NOTIFYICONDATA data = BuildData();
                Shell_NotifyIcon(NIM_DELETE, ref data);
                DestroyWindow(_hwnd);
                _created = false;
                _hwnd = IntPtr.Zero;
            }
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName,
            uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu,
            IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll")]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, UIntPtr uIDNewItem, string lpNewItem);

        [DllImport("user32.dll")]
        private static extern bool DestroyMenu(IntPtr hMenu);

        [DllImport("user32.dll")]
        private static extern uint TrackPopupMenuEx(IntPtr hMenu, uint uFlags, int x, int y, IntPtr hWnd, IntPtr lptpm);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern uint RegisterWindowMessage(string lpString);
    }
}
