using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Shadowsocks.Controller;
using System.Threading.Tasks;
using Windows.System;

namespace ShadowsocksR_winui3.Views
{
    public sealed partial class InputPasswordWindow : Window
    {
        private readonly TaskCompletionSource<bool> _completion = new TaskCompletionSource<bool>();

        public string Password { get; private set; }

        public InputPasswordWindow()
        {
            this.InitializeComponent();
            Title = I18N.GetString("InputPassword");
            InfoText.Text = I18N.GetString("Parse gui-config.json error, maybe require password to decrypt");
            Closed += (s, e) => _completion.TrySetResult(false);
        }

        public async Task<bool?> ShowModalAsync()
        {
            Activate();
            return await _completion.Task;
        }

        private void Complete(bool ok)
        {
            if (ok)
                Password = PasswordBox.Password;
            _completion.TrySetResult(ok);
            Close();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Complete(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Complete(false);
        }

        private void PasswordBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                Complete(true);
                e.Handled = true;
            }
        }
    }
}
