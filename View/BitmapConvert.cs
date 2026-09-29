using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Media.Imaging;
using ZXing.QrCode.Internal;

namespace Shadowsocks.View
{
    /// <summary>
    /// System.Drawing.Bitmap conversion helpers and QR code rendering shared by all views.
    /// </summary>
    public static class BitmapConvert
    {
        public static WriteableBitmap ToWriteableBitmap(Bitmap bitmap)
        {
            if (bitmap == null)
                return null;
            BitmapData data = null;
            try
            {
                data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                int byteCount = Math.Abs(data.Stride) * bitmap.Height;
                byte[] pixels = new byte[byteCount];
                Marshal.Copy(data.Scan0, pixels, 0, byteCount);
                WriteableBitmap result = new WriteableBitmap(bitmap.Width, bitmap.Height);
                using (var stream = result.PixelBuffer.AsStream())
                {
                    // GDI+ may store negative stride (bottom-up); normalize to top-down BGRA
                    if (data.Stride > 0)
                    {
                        stream.Write(pixels, 0, byteCount);
                    }
                    else
                    {
                        int absStride = -data.Stride;
                        for (int y = 0; y < bitmap.Height; ++y)
                        {
                            int src = y * absStride;
                            stream.Write(pixels, src, absStride);
                        }
                    }
                }
                return result;
            }
            finally
            {
                if (data != null)
                    bitmap.UnlockBits(data);
            }
        }

        /// <summary>
        /// Render an SSR/SS link into a QR bitmap with the logo drawn in the center.
        /// Mirrors ConfigForm.GenQR / ShowTextForm.GenQR rendering.
        /// </summary>
        public static Bitmap GenerateQr(string text, int width, bool withLogo)
        {
            QRCode code = ZXing.QrCode.Internal.Encoder.encode(text, ErrorCorrectionLevel.M);
            ByteMatrix m = code.Matrix;
            int blockSize = Math.Max(width / (m.Width + 2), 1);
            Bitmap drawArea = new Bitmap((m.Width + 2) * blockSize, (m.Height + 2) * blockSize);
            using (Graphics g = Graphics.FromImage(drawArea))
            {
                g.Clear(Color.White);
                using (Brush b = new SolidBrush(Color.Black))
                {
                    for (int row = 0; row < m.Width; row++)
                    {
                        for (int col = 0; col < m.Height; col++)
                        {
                            if (m[row, col] != 0)
                            {
                                g.FillRectangle(b, blockSize * (row + 1), blockSize * (col + 1),
                                    blockSize, blockSize);
                            }
                        }
                    }
                }
                if (withLogo)
                {
                    Bitmap ngnl = Properties.Resources.ngnl;
                    int div = 13, div_l = 5, div_r = 8;
                    int l = (m.Width * div_l + div - 1) / div * blockSize;
                    int r = (m.Width * div_r + div - 1) / div * blockSize;
                    g.DrawImage(ngnl, new Rectangle(l + blockSize, l + blockSize, r - l, r - l));
                }
            }
            return drawArea;
        }

        /// <summary>
        /// Default logo image shown before the link box gets focus (ConfigForm.DrawLogo).
        /// </summary>
        public static Bitmap GenerateLogo(int width, bool hideTips)
        {
            Bitmap drawArea = new Bitmap(width, width);
            using (Graphics g = Graphics.FromImage(drawArea))
            {
                g.Clear(Color.White);
                Bitmap ngnl = Properties.Resources.ngnl;
                g.DrawImage(ngnl, new Rectangle(0, 0, width, width));
                if (!hideTips)
                    g.DrawString("Click the 'Link' text box", new Font("Arial", 14),
                        new SolidBrush(Color.Black), new RectangleF(0, 0, 300, 300));
            }
            return drawArea;
        }
    }
}
