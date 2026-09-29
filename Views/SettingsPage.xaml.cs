using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.View;
using System;

namespace ShadowsocksR_winui3.Views
{
    public sealed partial class SettingsPage : Page, IPageController
    {
        private ShadowsocksController controller;
        private Configuration _modifiedConfiguration;

        public SettingsPage()
        {
            this.InitializeComponent();

            controller = ShadowsocksR_winui3.App.Controller;

            foreach (string name in new string[] {
                "Socks5(support UDP)", "Http tunnel", "TCP Port tunnel"})
            {
                comboProxyType.Items.Add(name);
            }
            foreach (string name in new string[] {
                "Order", "Random", "LowLatency", "LowException",
                "SelectedFirst", "Timer"})
            {
                RandomComboBox.Items.Add(name);
            }

            UpdateTexts();

            if (controller != null)
                controller.ConfigChanged += controller_ConfigChanged;
            this.Unloaded += (s2, e2) =>
            {
                if (controller != null)
                    controller.ConfigChanged -= controller_ConfigChanged;
            };
        }

        public void OnShow(int arg)
        {
            if (controller == null)
                return;
            UpdateTexts();
            LoadCurrentConfiguration();
        }

        private void UpdateTexts()
        {
            if (controller != null)
            {
                Configuration config = controller.GetCurrentConfiguration();
                HeaderText.Text = I18N.GetString("Global Settings") + "("
                    + (config.shareOverLan ? "any" : "local") + ":" + config.localPort.ToString()
                    + I18N.GetString(" Version") + UpdateChecker.FullVersion
                    + ")";
            }

            ListenGroup.Text = I18N.GetString(ListenGroup.Text);
            checkShareOverLan.Content = I18N.GetString("Allow Clients from LAN");
            ProxyPortLabel.Text = I18N.GetString("Proxy Port");
            ReconnectLabel.Text = I18N.GetString("Reconnect Times");
            TTLLabel.Text = I18N.GetString("TTL");
            labelTimeout.Text = I18N.GetString(" Timeout");

            checkAutoStartup.Content = I18N.GetString("Start on Boot");
            checkRandom.Content = I18N.GetString("Load balance");
            CheckAutoBan.Content = I18N.GetString("AutoBan");

            Socks5ProxyGroup.Text = I18N.GetString(Socks5ProxyGroup.Text);
            checkBoxPacProxy.Content = I18N.GetString("PAC \"direct\" return this proxy");
            CheckSockProxy.Content = I18N.GetString("Proxy On");
            LabelS5Server.Text = I18N.GetString("Server IP");
            LabelS5Port.Text = I18N.GetString("Server Port");
            LabelS5Server.Text = I18N.GetString("Server IP");
            LabelS5Port.Text = I18N.GetString("Server Port");
            LabelS5Username.Text = I18N.GetString("Username");
            LabelS5Password.Text = I18N.GetString("Password");
            LabelAuthUser.Text = I18N.GetString("Username");
            LabelAuthPass.Text = I18N.GetString("Password");

            LabelRandom.Text = I18N.GetString("Balance");
            for (int i = 0; i < comboProxyType.Items.Count; ++i)
            {
                comboProxyType.Items[i] = I18N.GetString(comboProxyType.Items[i].ToString());
            }
            checkBalanceInGroup.Content = I18N.GetString("Balance in group");
            for (int i = 0; i < RandomComboBox.Items.Count; ++i)
            {
                RandomComboBox.Items[i] = I18N.GetString(RandomComboBox.Items[i].ToString());
            }

            OKButton.Content = I18N.GetString("OK");
            MyCancelButton.Content = I18N.GetString("Cancel");
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
        }

        private static int ReadNumber(NumberBox box)
        {
            double value = box.Value;
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new FormatException("Input string was not in a correct format.");
            }
            return Convert.ToInt32(value);
        }

        private int SaveOldSelectedServer()
        {
            try
            {
                int localPort = ReadNumber(NumProxyPort);
                Configuration.CheckPort(localPort);
                int ret = 0;
                _modifiedConfiguration.shareOverLan = checkShareOverLan.IsChecked == true;
                _modifiedConfiguration.localPort = localPort;
                _modifiedConfiguration.reconnectTimes = NumReconnect.Text.Length == 0 ? 0 : ReadNumber(NumReconnect);

                if ((checkAutoStartup.IsChecked == true) != AutoStartup.Check() && !AutoStartup.Set(checkAutoStartup.IsChecked == true))
                {
                    NativeUi.MessageBox(I18N.GetString("Failed to update registry"));
                }
                _modifiedConfiguration.random = checkRandom.IsChecked == true;
                _modifiedConfiguration.randomAlgorithm = RandomComboBox.SelectedIndex;
                _modifiedConfiguration.randomInGroup = checkBalanceInGroup.IsChecked == true;
                _modifiedConfiguration.TTL = ReadNumber(NumTTL);
                _modifiedConfiguration.connectTimeout = ReadNumber(NumTimeout);
                _modifiedConfiguration.dnsServer = DNSText.Text;
                _modifiedConfiguration.proxyEnable = CheckSockProxy.IsChecked == true;
                _modifiedConfiguration.pacDirectGoProxy = checkBoxPacProxy.IsChecked == true;
                _modifiedConfiguration.proxyType = comboProxyType.SelectedIndex;
                _modifiedConfiguration.proxyHost = TextS5Server.Text;
                _modifiedConfiguration.proxyPort = ReadNumber(NumS5Port);
                _modifiedConfiguration.proxyAuthUser = TextS5User.Text;
                _modifiedConfiguration.proxyAuthPass = TextS5Pass.Password;
                _modifiedConfiguration.proxyUserAgent = TextUserAgent.Text;
                _modifiedConfiguration.authUser = TextAuthUser.Text;
                _modifiedConfiguration.authPass = TextAuthPass.Password;

                _modifiedConfiguration.autoBan = CheckAutoBan.IsChecked == true;

                return ret;
            }
            catch (Exception ex)
            {
                NativeUi.MessageBox(ex.Message);
            }
            return -1;
        }

        private void LoadSelectedServer()
        {
            checkShareOverLan.IsChecked = _modifiedConfiguration.shareOverLan;
            NumProxyPort.Value = _modifiedConfiguration.localPort;
            NumReconnect.Value = _modifiedConfiguration.reconnectTimes;

            checkAutoStartup.IsChecked = AutoStartup.Check();
            checkRandom.IsChecked = _modifiedConfiguration.random;
            if (_modifiedConfiguration.randomAlgorithm >= 0 && _modifiedConfiguration.randomAlgorithm < RandomComboBox.Items.Count)
            {
                RandomComboBox.SelectedIndex = _modifiedConfiguration.randomAlgorithm;
            }
            else
            {
                RandomComboBox.SelectedIndex = (int)ServerSelectStrategy.SelectAlgorithm.LowException;
            }
            checkBalanceInGroup.IsChecked = _modifiedConfiguration.randomInGroup;
            NumTTL.Value = _modifiedConfiguration.TTL;
            NumTimeout.Value = _modifiedConfiguration.connectTimeout;
            DNSText.Text = _modifiedConfiguration.dnsServer ?? "";

            CheckSockProxy.IsChecked = _modifiedConfiguration.proxyEnable;
            checkBoxPacProxy.IsChecked = _modifiedConfiguration.pacDirectGoProxy;
            comboProxyType.SelectedIndex = _modifiedConfiguration.proxyType;
            TextS5Server.Text = _modifiedConfiguration.proxyHost ?? "";
            NumS5Port.Value = _modifiedConfiguration.proxyPort;
            TextS5User.Text = _modifiedConfiguration.proxyAuthUser ?? "";
            TextS5Pass.Password = _modifiedConfiguration.proxyAuthPass ?? "";
            TextUserAgent.Text = _modifiedConfiguration.proxyUserAgent ?? "";
            TextAuthUser.Text = _modifiedConfiguration.authUser ?? "";
            TextAuthPass.Password = _modifiedConfiguration.authPass ?? "";

            CheckAutoBan.IsChecked = _modifiedConfiguration.autoBan;
        }

        private void LoadCurrentConfiguration()
        {
            _modifiedConfiguration = controller.GetConfiguration();
            LoadSelectedServer();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (SaveOldSelectedServer() == -1)
            {
                return;
            }
            controller.SaveServersConfig(_modifiedConfiguration);
            ShadowsocksR_winui3.MainWindow.Instance?.HideWindow();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            LoadCurrentConfiguration();
            ShadowsocksR_winui3.MainWindow.Instance?.HideWindow();
        }

        private void buttonDefault_Click(object sender, RoutedEventArgs e)
        {
            if (CheckSockProxy.IsChecked == true)
            {
                NumReconnect.Value = 4;
                NumTimeout.Value = 10;
                NumTTL.Value = 60;
            }
            else
            {
                NumReconnect.Value = 4;
                NumTimeout.Value = 5;
                NumTTL.Value = 60;
            }
        }
    }
}
