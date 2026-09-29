using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.View;
using Windows.System;

namespace ShadowsocksR_winui3.Views
{
    public sealed partial class ResetPasswordWindow : Window
    {
        public ResetPasswordWindow()
        {
            this.InitializeComponent();
            Title = I18N.GetString("ResetPassword");
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (NewPasswordBox.Password == NewPasswordBox2.Password
                && Configuration.SetPasswordTry(OldPasswordBox.Password, NewPasswordBox.Password))
            {
                Configuration cfg = Configuration.Load();
                Configuration.SetPassword(NewPasswordBox.Password);
                Configuration.Save(cfg);
                Close();
            }
            else
            {
                NativeUi.MessageBox(I18N.GetString("Password NOT match"), "SSR error", NativeUi.MB_OK | NativeUi.MB_ICONERROR);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OldPasswordBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                NewPasswordBox.Focus(FocusState.Programmatic);
                e.Handled = true;
            }
        }

        private void NewPasswordBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                NewPasswordBox2.Focus(FocusState.Programmatic);
                e.Handled = true;
            }
        }

        private void NewPasswordBox2_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                OkButton_Click(this, e);
                e.Handled = true;
            }
        }
    }
}
