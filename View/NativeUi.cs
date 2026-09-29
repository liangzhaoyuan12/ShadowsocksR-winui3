using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Shadowsocks.Controller;

namespace Shadowsocks.View
{
    /// <summary>
    /// Win32 based UI helpers replacing System.Windows.Forms MessageBox/Clipboard.
    /// Usable from the tray message loop even before any WinUI window exists.
    /// </summary>
    public static class NativeUi
    {
        public const int MB_OK = 0x00000000;
        public const int MB_OKCANCEL = 0x00000001;
        public const int MB_YESNO = 0x00000004;
        public const int MB_ICONERROR = 0x00000010;
        public const int MB_ICONQUESTION = 0x00000020;
        public const int MB_ICONWARNING = 0x00000030;
        public const int MB_ICONINFO = 0x00000040;
        public const int IDOK = 1;
        public const int IDCANCEL = 2;
        public const int IDYES = 6;
        public const int IDNO = 7;

        public static int MessageBox(string text)
        {
            return MessageBox(text, "ShadowsocksR", MB_OK | MB_ICONINFO);
        }

        public static int MessageBox(string text, string caption)
        {
            return MessageBox(text, caption, MB_OK | MB_ICONINFO);
        }

        public static int MessageBox(string text, string caption, int flags)
        {
            return MessageBoxW(IntPtr.Zero, text ?? "", caption ?? "", flags);
        }

        public static void ShowError(string text)
        {
            MessageBox(text, "ShadowsocksR", MB_OK | MB_ICONERROR);
        }

        public static void OpenUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return;
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
            }
        }

        public static void OpenFileInExplorer(string path)
        {
            try
            {
                Process.Start("explorer.exe", "/select, " + path);
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
            }
        }

        #region clipboard

        private const uint CF_UNICODETEXT = 13;

        public static string GetClipboardText()
        {
            if (!IsClipboardFormatAvailable(CF_UNICODETEXT))
                return null;
            if (!OpenClipboard(IntPtr.Zero))
                return null;
            try
            {
                IntPtr handle = GetClipboardData(CF_UNICODETEXT);
                if (handle == IntPtr.Zero)
                    return null;
                IntPtr ptr = GlobalLock(handle);
                if (ptr == IntPtr.Zero)
                    return null;
                try
                {
                    return Marshal.PtrToStringUni(ptr);
                }
                finally
                {
                    GlobalUnlock(handle);
                }
            }
            finally
            {
                CloseClipboard();
            }
        }

        public static bool SetClipboardText(string text)
        {
            if (text == null)
                text = "";
            if (!OpenClipboard(IntPtr.Zero))
                return false;
            try
            {
                EmptyClipboard();
                int bytes = (text.Length + 1) * 2;
                IntPtr handle = GlobalAlloc(0x0002 /*GMEM_MOVEABLE*/, (UIntPtr)bytes);
                if (handle == IntPtr.Zero)
                    return false;
                IntPtr ptr = GlobalLock(handle);
                if (ptr == IntPtr.Zero)
                    return false;
                try
                {
                    byte[] buffer = Encoding.Unicode.GetBytes(text);
                    Marshal.Copy(buffer, 0, ptr, buffer.Length);
                    Marshal.WriteInt16(ptr, buffer.Length, 0);
                }
                finally
                {
                    GlobalUnlock(handle);
                }
                return SetClipboardData(CF_UNICODETEXT, handle) != IntPtr.Zero;
            }
            finally
            {
                CloseClipboard();
            }
        }

        #endregion

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, int type);

        [DllImport("user32.dll")]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        private static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("user32.dll")]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("user32.dll")]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll")]
        private static extern bool IsClipboardFormatAvailable(uint format);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);
    }
}
