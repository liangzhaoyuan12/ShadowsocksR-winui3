using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Shadowsocks.View
{
    /// <summary>
    /// Per-pixel alpha splash window that animates a red highlight rectangle onto a
    /// detected QR code, then flashes and closes itself. Port of QRCodeSplashForm
    /// (WinForms PerPixelAlphaForm) to a raw Win32 layered window.
    /// </summary>
    public class QrSplashWindow : IDisposable
    {
        public event Action Closed;

        private static double FPS = 1.0 / 15 * 1000;
        private static double ANIMATION_TIME = 0.5;
        private static int ANIMATION_STEPS = (int)(ANIMATION_TIME * FPS);

        private IntPtr _hwnd;
        private uint _classAtom;
        private WndProc _wndProc;
        private Stopwatch _sw;
        private int _flashStep;
        private int _x, _y, _w, _h;
        private Rectangle _targetRect;
        private Rectangle _bounds;
        private Bitmap _bitmap;
        private Graphics _g;
        private Pen _pen;
        private SolidBrush _brush;
        private bool _destroyed;

        private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private const int WS_POPUP = unchecked((int)0x80000000);
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_TOPMOST = 0x00000008;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const uint WM_TIMER = 0x0113;
        private const uint WM_DESTROY = 0x0002;

        public QrSplashWindow()
        {
        }

        public void Show(Rectangle windowBounds, Rectangle targetRect)
        {
            _bounds = windowBounds;
            _targetRect = new Rectangle(
                targetRect.X - windowBounds.X,
                targetRect.Y - windowBounds.Y,
                targetRect.Width,
                targetRect.Height);

            _flashStep = 0;
            _x = 0;
            _y = 0;
            _w = _bounds.Width;
            _h = _bounds.Height;
            _sw = Stopwatch.StartNew();

            _bitmap = new Bitmap(Math.Max(1, _bounds.Width), Math.Max(1, _bounds.Height), PixelFormat.Format32bppArgb);
            _g = Graphics.FromImage(_bitmap);
            _pen = new Pen(Color.Red, 3);
            _brush = new SolidBrush(Color.FromArgb(30, Color.Red));

            _wndProc = WndProcImpl;
            IntPtr module = GetModuleHandle(null);
            string className = "ShadowsocksRQrSplash_" + Guid.NewGuid().ToString("N");
            WNDCLASSEX cls = new WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEX)),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
                hInstance = module,
                lpszClassName = className,
            };
            _classAtom = RegisterClassEx(ref cls);

            _hwnd = CreateWindowEx(WS_EX_LAYERED | WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE,
                className, "", WS_POPUP,
                _bounds.X, _bounds.Y, _bounds.Width, _bounds.Height,
                IntPtr.Zero, IntPtr.Zero, module, IntPtr.Zero);
            if (_hwnd == IntPtr.Zero)
            {
                Dispose();
                return;
            }

            int interval = (int)(ANIMATION_TIME * 1000 / ANIMATION_STEPS);
            SetTimer(_hwnd, 1, (uint)Math.Max(1, interval), IntPtr.Zero);
        }

        private IntPtr WndProcImpl(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_TIMER)
            {
                Tick();
                return IntPtr.Zero;
            }
            if (msg == WM_DESTROY)
            {
                Finish();
                return IntPtr.Zero;
            }
            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        private void Tick()
        {
            if (_destroyed)
                return;
            double percent = (double)_sw.ElapsedMilliseconds / 1000.0 / ANIMATION_TIME;
            if (percent < 1)
            {
                // ease out
                percent = 1 - Math.Pow((1 - percent), 4);
                _x = (int)(_targetRect.X * percent);
                _y = (int)(_targetRect.Y * percent);
                _w = (int)(_targetRect.Width * percent + _bounds.Width * (1 - percent));
                _h = (int)(_targetRect.Height * percent + _bounds.Height * (1 - percent));
                _pen.Color = Color.FromArgb((int)(255 * percent), Color.Red);
                _brush.Color = Color.FromArgb((int)(30 * percent), Color.Red);
                _g.Clear(Color.Transparent);
                _g.FillRectangle(_brush, _x, _y, _w, _h);
                _g.DrawRectangle(_pen, _x, _y, _w, _h);
                SetBitmap(_bitmap);
            }
            else
            {
                if (_flashStep == 0)
                {
                    SetTimer(_hwnd, 1, 100, IntPtr.Zero);
                    _g.Clear(Color.Transparent);
                    SetBitmap(_bitmap);
                }
                else if (_flashStep == 1)
                {
                    SetTimer(_hwnd, 1, 50, IntPtr.Zero);
                    _g.FillRectangle(_brush, _x, _y, _w, _h);
                    _g.DrawRectangle(_pen, _x, _y, _w, _h);
                    SetBitmap(_bitmap);
                }
                else if (_flashStep == 2)
                {
                    _g.FillRectangle(_brush, _x, _y, _w, _h);
                    _g.DrawRectangle(_pen, _x, _y, _w, _h);
                    SetBitmap(_bitmap);
                }
                else if (_flashStep == 3)
                {
                    _g.Clear(Color.Transparent);
                    SetBitmap(_bitmap);
                }
                else if (_flashStep == 4)
                {
                    _g.FillRectangle(_brush, _x, _y, _w, _h);
                    _g.DrawRectangle(_pen, _x, _y, _w, _h);
                    SetBitmap(_bitmap);
                }
                else
                {
                    _sw.Stop();
                    KillTimer(_hwnd, 1);
                    CloseWindow();
                    return;
                }
                _flashStep++;
            }
        }

        private void SetBitmap(Bitmap bitmap)
        {
            IntPtr screenDc = GetDC(IntPtr.Zero);
            IntPtr memDc = CreateCompatibleDC(screenDc);
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;
            try
            {
                hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
                oldBitmap = SelectObject(memDc, hBitmap);

                Size size = new Size(bitmap.Width, bitmap.Height);
                Point pointSource = new Point(0, 0);
                Point topPos = new Point(_bounds.X, _bounds.Y);
                BLENDFUNCTION blend = new BLENDFUNCTION();
                blend.BlendOp = AC_SRC_OVER;
                blend.BlendFlags = 0;
                blend.SourceConstantAlpha = 255;
                blend.AlphaFormat = AC_SRC_ALPHA;

                UpdateLayeredWindow(_hwnd, screenDc, ref topPos, ref size, memDc, ref pointSource, 0,
                    ref blend, ULW_ALPHA);
            }
            finally
            {
                ReleaseDC(IntPtr.Zero, screenDc);
                if (hBitmap != IntPtr.Zero)
                {
                    SelectObject(memDc, oldBitmap);
                    DeleteObject(hBitmap);
                }
                DeleteDC(memDc);
            }
        }

        private void CloseWindow()
        {
            if (_hwnd != IntPtr.Zero && !_destroyed)
            {
                DestroyWindow(_hwnd);
            }
            else
            {
                Finish();
            }
        }

        private void Finish()
        {
            if (_destroyed)
                return;
            _destroyed = true;
            try
            {
                if (_pen != null) _pen.Dispose();
                if (_brush != null) _brush.Dispose();
                if (_g != null) _g.Dispose();
                if (_bitmap != null) _bitmap.Dispose();
            }
            catch
            {
            }
            _pen = null;
            _brush = null;
            _g = null;
            _bitmap = null;
            _hwnd = IntPtr.Zero;
            var handler = Closed;
            if (handler != null)
                handler();
        }

        public void Dispose()
        {
            if (!_destroyed && _hwnd != IntPtr.Zero)
            {
                DestroyWindow(_hwnd);
            }
            Finish();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Point
        {
            public int x;
            public int y;
            public Point(int x, int y) { this.x = x; this.y = y; }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Size
        {
            public int cx;
            public int cy;
            public Size(int cx, int cy) { this.cx = cx; this.cy = cy; }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
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

        private const int ULW_ALPHA = 0x00000002;
        private const byte AC_SRC_OVER = 0x00;
        private const byte AC_SRC_ALPHA = 0x01;

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern int UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref Point pptDst,
            ref Size psize, IntPtr hdcSrc, ref Point pprSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern int DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        private static extern int DeleteObject(IntPtr hObject);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateWindowEx(int dwExStyle, string lpClassName, string lpWindowName,
            int dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu,
            IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint SetTimer(IntPtr hWnd, int nIDEvent, uint uElapse, IntPtr lpTimerFunc);

        [DllImport("user32.dll")]
        private static extern bool KillTimer(IntPtr hWnd, int nIDEvent);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
