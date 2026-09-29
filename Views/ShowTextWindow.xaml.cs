using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Shadowsocks.View;
using System;
using Windows.System;

namespace ShadowsocksR_winui3.Views
{
    public sealed partial class ShowTextWindow : Window
    {
        public ShowTextWindow(string title, string text)
        {
            this.InitializeComponent();
            Title = title;
            TextBox.Text = text;
            SizeChanged += ShowTextWindow_SizeChanged;
        }

        private void GenQR(string ssconfig)
        {
            try
            {
                int width = (int)Math.Max(QrImage.ActualWidth, 300);
                using (System.Drawing.Bitmap qr = BitmapConvert.GenerateQr(ssconfig, width, true))
                {
                    QrImage.Source = BitmapConvert.ToWriteableBitmap(qr);
                }
            }
            catch
            {
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            GenQR(TextBox.Text);
        }

        private void ShowTextWindow_SizeChanged(object sender, WindowSizeChangedEventArgs args)
        {
            GenQR(TextBox.Text);
        }

        private void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.A && HasControl())
            {
                TextBox.SelectAll();
                e.Handled = true;
            }
        }

        private bool HasControl()
        {
            var state = Windows.UI.Core.CoreWindow.GetForCurrentThread().GetKeyState(Windows.System.VirtualKey.Control);
            return (state & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
        }
    }
}
