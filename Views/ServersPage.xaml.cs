using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Shadowsocks.Controller;
using Shadowsocks.Encryption;
using Shadowsocks.Model;
using Shadowsocks.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.System;

namespace ShadowsocksR_winui3.Views
{
    public sealed partial class ServersPage : Page, IPageController
    {
        private ShadowsocksController controller;
        private UpdateChecker updateChecker;

        private Configuration _modifiedConfiguration;
        private int _oldSelectedIndex = -1;
        private bool _allowSave = true;
        private bool _ignoreLoad = false;
        private string _oldSelectedID = null;
        private string _SelectedID = null;
        private bool _textLinkFocused = false;

        private ObservableCollection<string> _listItems = new ObservableCollection<string>();

        public ServersPage()
        {
            this.InitializeComponent();

            controller = ShadowsocksR_winui3.App.Controller;
            updateChecker = ShadowsocksR_winui3.App.UpdateChecker;

            foreach (string name in EncryptorFactory.GetEncryptor())
            {
                EncryptorInfo info = EncryptorFactory.GetEncryptorInfo(name);
                if (info.display)
                    EncryptionSelect.Items.Add(name);
            }
            foreach (string name in new string[] {
                "origin", "verify_deflate", "auth_sha1_v4", "auth_aes128_md5",
                "auth_aes128_sha1", "auth_chain_a", "auth_chain_b"})
            {
                TCPProtocolComboBox.Items.Add(I18N.GetString(name));
            }
            foreach (string name in new string[] {
                "plain", "http_simple", "http_post", "random_head",
                "tls1.2_ticket_auth", "tls1.2_ticket_fastauth"})
            {
                ObfsCombo.Items.Add(name);
            }

            ServersListBox.ItemsSource = _listItems;
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
            LoadCurrentConfiguration();
            if (_modifiedConfiguration.index >= 0 && _modifiedConfiguration.index < _modifiedConfiguration.configs.Count)
                _oldSelectedID = _modifiedConfiguration.configs[_modifiedConfiguration.index].id;
            else
                _oldSelectedID = null;

            int focusIndex = arg;
            if (focusIndex == -1)
            {
                int index = _modifiedConfiguration.index + 1;
                if (index < 0 || index > _modifiedConfiguration.configs.Count)
                    index = _modifiedConfiguration.configs.Count;
                focusIndex = index;
            }

            if (_modifiedConfiguration.isHideTips)
                QrImage.Visibility = Visibility.Collapsed;

            DrawLogo(350);

            UpdateLinkVisibility();

            if (focusIndex >= 0 && focusIndex < _modifiedConfiguration.configs.Count)
            {
                SetServerListSelectedIndex(focusIndex);
                LoadSelectedServer();
            }

            ScrollSelectedIntoView();
        }

        private void UpdateLinkVisibility()
        {
            if (updateChecker == null)
                return;
            if (updateChecker.LatestVersionURL == null)
            {
                LinkUpdate.Visibility = Visibility.Collapsed;
            }
            else
            {
                LinkUpdate.Visibility = Visibility.Visible;
                LinkUpdate.Content = String.Format(I18N.GetString("New version {0} {1} available"),
                    UpdateChecker.Name, updateChecker.LatestVersionNumber);
            }
        }

        private void UpdateTexts()
        {
            Configuration config = controller != null ? controller.GetCurrentConfiguration() : null;
            if (config != null)
            {
                HeaderText.Text = I18N.GetString("Edit Servers") + "("
                    + (config.shareOverLan ? "any" : "local") + ":" + config.localPort.ToString()
                    + I18N.GetString(" Version") + UpdateChecker.FullVersion
                    + ")";
            }

            AddButton.Content = I18N.GetString("&Add");
            DeleteButton.Content = I18N.GetString("&Delete");
            UpButton.Content = I18N.GetString("Up");
            DownButton.Content = I18N.GetString("Down");

            const string mark_str = "* ";
            IPLabel.Content = mark_str + I18N.GetString("Server IP");
            ServerPortLabel.Text = mark_str + I18N.GetString("Server Port");
            labelUDPPort.Text = I18N.GetString("UDP Port");
            PasswordLabel.Content = mark_str + I18N.GetString("Password");
            EncryptionLabel.Text = mark_str + I18N.GetString("Encryption");
            TCPProtocolLabel.Text = mark_str + I18N.GetString("Protocol");
            labelObfs.Text = mark_str + I18N.GetString("Obfs");
            labelRemarks.Text = I18N.GetString("Remarks");
            labelGroup.Text = I18N.GetString("Group");

            checkAdvSetting.Content = I18N.GetString("Adv. Setting");
            UDPoverTCPLabel.Text = I18N.GetString("UDP over TCP");
            labelProtocolParam.Text = I18N.GetString("Protocol param");
            labelObfsParam.Text = I18N.GetString("Obfs param");
            NoteText.Text = I18N.GetString("NOT all server support belows");
            CheckUDPoverUDP.Content = I18N.GetString("UDP over UDP if not checked");
            checkSSRLink.Content = I18N.GetString("SSR Link");

            ServerGroupTitle.Text = I18N.GetString("Server");
            OKButton.Content = I18N.GetString("OK");
            MyCancelButton.Content = I18N.GetString("Cancel");
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
        }

        private int SaveOldSelectedServer()
        {
            try
            {
                if (_oldSelectedIndex == -1 || _oldSelectedIndex >= _modifiedConfiguration.configs.Count)
                {
                    return 0;
                }
                if (double.IsNaN(NumServerPort.Value) || double.IsNaN(NumUDPPort.Value))
                {
                    throw new FormatException();
                }
                Server server = new Server
                {
                    server = IPTextBox.Password.Trim(),
                    server_port = Convert.ToInt32(NumServerPort.Value),
                    server_udp_port = Convert.ToInt32(NumUDPPort.Value),
                    password = PasswordTextBox.Password,
                    method = ComboOrOld(EncryptionSelect, _modifiedConfiguration.configs[_oldSelectedIndex].method, "aes-256-cfb"),
                    protocol = ComboOrOld(TCPProtocolComboBox, _modifiedConfiguration.configs[_oldSelectedIndex].protocol, "origin"),
                    protocolparam = TextProtocolParam.Text,
                    obfs = ComboOrOld(ObfsCombo, _modifiedConfiguration.configs[_oldSelectedIndex].obfs, "plain"),
                    obfsparam = TextObfsParam.Text,
                    remarks = RemarksTextBox.Text,
                    group = TextGroup.Text.Trim(),
                    udp_over_tcp = CheckUDPoverUDP.IsChecked == true,
                    id = _SelectedID
                };
                Configuration.CheckServer(server);
                int ret = 0;
                if (_modifiedConfiguration.configs[_oldSelectedIndex].server != server.server
                    || _modifiedConfiguration.configs[_oldSelectedIndex].server_port != server.server_port
                    || _modifiedConfiguration.configs[_oldSelectedIndex].remarks_base64 != server.remarks_base64
                    || _modifiedConfiguration.configs[_oldSelectedIndex].group != server.group
                    )
                {
                    ret = 1;
                }
                Server oldServer = _modifiedConfiguration.configs[_oldSelectedIndex];
                if (oldServer.isMatchServer(server))
                {
                    server.setObfsData(oldServer.getObfsData());
                    server.setProtocolData(oldServer.getProtocolData());
                    server.enable = oldServer.enable;
                }
                _modifiedConfiguration.configs[_oldSelectedIndex] = server;

                return ret;
            }
            catch (FormatException)
            {
                NativeUi.MessageBox(I18N.GetString("Illegal port number format"));
            }
            catch (Exception ex)
            {
                NativeUi.MessageBox(ex.Message);
            }
            return -1;
        }

        private string DisplayText(Server server)
        {
            if (!string.IsNullOrEmpty(server.group))
                return server.group + " - " + server.HiddenName();
            return "      " + server.HiddenName();
        }

        private void SelectCombo(ComboBox combo, string value)
        {
            if (value == null)
                value = "";
            foreach (object item in combo.Items)
            {
                if (Equals(item as string, value))
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
            if (value.Length == 0)
            {
                combo.SelectedIndex = -1;
                return;
            }
            combo.Items.Add(value);
            combo.SelectedItem = value;
        }

        private string GetComboValue(ComboBox combo)
        {
            string selected = combo.SelectedItem as string;
            if (!string.IsNullOrEmpty(selected))
                return selected;
            string text = combo.Text;
            return text ?? "";
        }

        private string ComboOrOld(ComboBox combo, string oldValue, string defaultValue)
        {
            string value = GetComboValue(combo);
            if (value.Length > 0)
                return value;
            if (!string.IsNullOrEmpty(oldValue))
                return oldValue;
            return defaultValue;
        }

        private void GenQR(string ssconfig)
        {
            if (_textLinkFocused)
            {
                using (System.Drawing.Bitmap drawArea = BitmapConvert.GenerateQr(ssconfig, 350, true))
                {
                    QrImage.Source = BitmapConvert.ToWriteableBitmap(drawArea);
                }
                QrImage.Visibility = Visibility.Visible;
                _modifiedConfiguration.isHideTips = true;
            }
            else
            {
                DrawLogo(350);
            }
        }

        private void DrawLogo(int width)
        {
            using (System.Drawing.Bitmap drawArea = BitmapConvert.GenerateLogo(width, _modifiedConfiguration.isHideTips))
            {
                QrImage.Source = BitmapConvert.ToWriteableBitmap(drawArea);
            }
        }

        private void LoadSelectedServer()
        {
            int index = CurrentIndex;
            if (index >= 0 && index < _modifiedConfiguration.configs.Count)
            {
                Server server = _modifiedConfiguration.configs[index];

                IPTextBox.Password = server.server ?? "";
                NumServerPort.Value = server.server_port;
                NumUDPPort.Value = server.server_udp_port;
                PasswordTextBox.Password = server.password ?? "";
                SelectCombo(EncryptionSelect, server.method ?? "aes-256-cfb");
                string protocol_text;
                if (string.IsNullOrEmpty(server.protocol))
                {
                    protocol_text = "origin";
                }
                else
                {
                    protocol_text = server.protocol ?? "origin";
                }
                SelectCombo(TCPProtocolComboBox, protocol_text);
                string obfs_text = server.obfs ?? "plain";
                SelectCombo(ObfsCombo, obfs_text);
                TextProtocolParam.Text = server.protocolparam;
                TextObfsParam.Text = server.obfsparam;
                RemarksTextBox.Text = server.remarks;
                TextGroup.Text = server.group;
                CheckUDPoverUDP.IsChecked = server.udp_over_tcp;
                _SelectedID = server.id;

                ServerGroupBox.Visibility = Visibility.Visible;

                if (protocol_text == "origin"
                    && obfs_text == "plain"
                    && CheckUDPoverUDP.IsChecked != true
                    )
                {
                    checkAdvSetting.IsChecked = false;
                }

                if (checkSSRLink.IsChecked == true)
                {
                    TextLink.Text = server.GetSSRLinkForServer();
                }
                else
                {
                    TextLink.Text = server.GetSSLinkForServer();
                }

                if (CheckUDPoverUDP.IsChecked == true || server.server_udp_port != 0)
                {
                    checkAdvSetting.IsChecked = true;
                }

                Update_SSR_controls_Visable();
                UpdateObfsTextbox();
                TextLink.SelectAll();
                GenQR(TextLink.Text);
            }
            else
            {
                ServerGroupBox.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadConfiguration(Configuration configuration)
        {
            if (_listItems.Count != configuration.configs.Count)
            {
                _listItems.Clear();
                foreach (Server server in configuration.configs)
                {
                    _listItems.Add(DisplayText(server));
                }
            }
            else
            {
                for (int i = 0; i < configuration.configs.Count; ++i)
                {
                    _listItems[i] = DisplayText(configuration.configs[i]);
                }
            }
        }

        public void SetServerListSelectedIndex(int index)
        {
            ServersListBox.SelectedIndex = -1;
            if (index < _listItems.Count)
                ServersListBox.SelectedIndex = index;
            else
                _oldSelectedIndex = CurrentIndex;
        }

        private void LoadCurrentConfiguration()
        {
            _modifiedConfiguration = controller.GetConfiguration();
            LoadConfiguration(_modifiedConfiguration);
            _allowSave = false;
            SetServerListSelectedIndex(_modifiedConfiguration.index);
            _allowSave = true;
            LoadSelectedServer();
        }

        private List<int> GetSelectedIndices()
        {
            List<int> indices = new List<int>();
            foreach (object item in ServersListBox.SelectedItems)
            {
                int found = -1;
                for (int i = 0; i < _listItems.Count; ++i)
                {
                    if (indices.Contains(i))
                        continue;
                    if (ReferenceEquals(_listItems[i], item))
                    {
                        found = i;
                        break;
                    }
                    if (found < 0 && Equals(_listItems[i], item))
                        found = i;
                }
                if (found >= 0)
                    indices.Add(found);
            }
            indices.Sort();
            return indices;
        }

        private int CurrentIndex
        {
            get
            {
                List<int> indices = GetSelectedIndices();
                if (indices.Count == 0)
                    return -1;
                return indices[0];
            }
        }

        private void ScrollSelectedIntoView()
        {
            int index = CurrentIndex;
            if (index >= 0 && index < _listItems.Count)
            {
                ServersListBox.ScrollIntoView(_listItems[index]);
            }
        }

        private void ServersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int newIndex = CurrentIndex;
            if (newIndex == -1)
            {
                return;
            }
            if (_oldSelectedIndex == newIndex)
            {
                return;
            }
            if (_allowSave)
            {
                int change = SaveOldSelectedServer();
                if (change == -1)
                {
                    ServersListBox.SelectedIndex = _oldSelectedIndex;
                    return;
                }
                if (change == 1)
                {
                    LoadConfiguration(_modifiedConfiguration);
                }
            }
            if (!_ignoreLoad) LoadSelectedServer();
            _oldSelectedIndex = newIndex;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (SaveOldSelectedServer() == -1)
            {
                return;
            }
            Server server = _oldSelectedIndex >= 0 && _oldSelectedIndex < _modifiedConfiguration.configs.Count
                ? Configuration.CopyServer(_modifiedConfiguration.configs[_oldSelectedIndex])
                : Configuration.GetDefaultServer();
            _modifiedConfiguration.configs.Insert(_oldSelectedIndex < 0 ? 0 : _oldSelectedIndex + 1, server);
            LoadConfiguration(_modifiedConfiguration);
            _SelectedID = server.id;
            ServersListBox.SelectedIndex = _oldSelectedIndex + 1;
            _oldSelectedIndex = CurrentIndex;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            List<int> items = GetSelectedIndices();
            _oldSelectedIndex = CurrentIndex;
            if (items.Count > 0)
            {
                for (int i = items.Count - 1; i >= 0; --i)
                {
                    int index = items[i];
                    if (index >= 0 && index < _modifiedConfiguration.configs.Count)
                    {
                        _modifiedConfiguration.configs.RemoveAt(index);
                    }
                }
            }
            if (_oldSelectedIndex >= _modifiedConfiguration.configs.Count)
            {
                _oldSelectedIndex = _modifiedConfiguration.configs.Count - 1;
            }
            if (_oldSelectedIndex < 0)
            {
                _oldSelectedIndex = 0;
            }
            ServersListBox.SelectedIndex = _oldSelectedIndex;
            LoadConfiguration(_modifiedConfiguration);
            SetServerListSelectedIndex(_oldSelectedIndex);
            LoadSelectedServer();
            ScrollSelectedIntoView();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (SaveOldSelectedServer() == -1)
            {
                return;
            }
            if (_modifiedConfiguration.configs.Count == 0)
            {
                NativeUi.MessageBox(I18N.GetString("Please add at least one server"));
                return;
            }
            if (_oldSelectedID != null)
            {
                for (int i = 0; i < _modifiedConfiguration.configs.Count; ++i)
                {
                    if (_modifiedConfiguration.configs[i].id == _oldSelectedID)
                    {
                        _modifiedConfiguration.index = i;
                        break;
                    }
                }
            }
            controller.SaveServersConfig(_modifiedConfiguration);
            ShadowsocksR_winui3.MainWindow.Instance?.HideWindow();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            LoadCurrentConfiguration();
            ShadowsocksR_winui3.MainWindow.Instance?.HideWindow();
        }

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            List<int> items = GetSelectedIndices();
            int index = CurrentIndex;
            _oldSelectedIndex = index;
            SaveOldSelectedServer();
            if (items.Count == 1)
            {
                if (index > 0 && index < _modifiedConfiguration.configs.Count)
                {
                    _modifiedConfiguration.configs.Reverse(index - 1, 2);
                    ServersListBox.SelectedIndex = _oldSelectedIndex = index - 1;
                    LoadConfiguration(_modifiedConfiguration);
                    ServersListBox.SelectedIndex = _oldSelectedIndex = index - 1;
                    LoadSelectedServer();
                }
            }
            else
            {
                List<int> all_items = new List<int>();
                foreach (int item in items)
                {
                    if (item == 0)
                        return;
                    all_items.Add(item);
                }
                foreach (int item in all_items)
                {
                    _modifiedConfiguration.configs.Reverse(item - 1, 2);
                }
                _allowSave = false;
                _ignoreLoad = true;
                ServersListBox.SelectedIndex = _oldSelectedIndex = index - 1;
                LoadConfiguration(_modifiedConfiguration);
                ServersListBox.SelectedItems.Clear();
                foreach (int item in all_items)
                {
                    ServersListBox.SelectedItems.Add(_listItems[item - 1]);
                }
                _oldSelectedIndex = index - 1;
                _ignoreLoad = false;
                _allowSave = true;
                LoadSelectedServer();
            }
            ScrollSelectedIntoView();
        }

        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            List<int> items = GetSelectedIndices();
            int index = CurrentIndex;
            _oldSelectedIndex = index;
            SaveOldSelectedServer();
            if (items.Count == 1)
            {
                if (_oldSelectedIndex >= 0 && _oldSelectedIndex < _modifiedConfiguration.configs.Count - 1)
                {
                    _modifiedConfiguration.configs.Reverse(index, 2);
                    ServersListBox.SelectedIndex = _oldSelectedIndex = index + 1;
                    LoadConfiguration(_modifiedConfiguration);
                    ServersListBox.SelectedIndex = _oldSelectedIndex = index + 1;
                    LoadSelectedServer();
                }
            }
            else
            {
                List<int> rev_items = new List<int>();
                int max_index = _listItems.Count - 1;
                foreach (int item in items)
                {
                    if (item == max_index)
                        return;
                    rev_items.Insert(0, item);
                }
                foreach (int item in rev_items)
                {
                    _modifiedConfiguration.configs.Reverse(item, 2);
                }
                _allowSave = false;
                _ignoreLoad = true;
                ServersListBox.SelectedIndex = _oldSelectedIndex = index + 1;
                LoadConfiguration(_modifiedConfiguration);
                ServersListBox.SelectedItems.Clear();
                foreach (int item in rev_items)
                {
                    ServersListBox.SelectedItems.Add(_listItems[item + 1]);
                }
                _oldSelectedIndex = index + 1;
                _ignoreLoad = false;
                _allowSave = true;
                LoadSelectedServer();
            }
            ScrollSelectedIntoView();
        }

        private void EnterTextBox(UIElement sender)
        {
            int change = SaveOldSelectedServer();
            if (change == 1)
            {
                LoadConfiguration(_modifiedConfiguration);
            }
            LoadSelectedServer();
            TextBox textBox = sender as TextBox;
            if (textBox != null)
            {
                textBox.SelectAll();
                return;
            }
            PasswordBox passwordBox = sender as PasswordBox;
            if (passwordBox != null)
            {
                passwordBox.SelectAll();
            }
        }

        private void TextLink_GotFocus(object sender, RoutedEventArgs e)
        {
            _textLinkFocused = true;
            EnterTextBox(sender as UIElement);
        }

        private void TextLink_LostFocus(object sender, RoutedEventArgs e)
        {
            _textLinkFocused = false;
        }

        private void TextLink_Tapped(object sender, TappedRoutedEventArgs e)
        {
            TextLink.SelectAll();
        }

        private void LinkUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (updateChecker != null)
                NativeUi.OpenUrl(updateChecker.LatestVersionURL);
        }

        private void PasswordLabel_Changed(object sender, RoutedEventArgs e)
        {
            PasswordTextBox.PasswordRevealMode =
                PasswordLabel.IsChecked == true ? PasswordRevealMode.Visible : PasswordRevealMode.Hidden;
        }

        private void IPLabel_Changed(object sender, RoutedEventArgs e)
        {
            IPTextBox.PasswordRevealMode =
                IPLabel.IsChecked == true ? PasswordRevealMode.Visible : PasswordRevealMode.Hidden;
        }

        private void UpdateObfsTextbox()
        {
            try
            {
                string obfs_value = GetComboValue(ObfsCombo);
                Shadowsocks.Obfs.ObfsBase obfs = (Shadowsocks.Obfs.ObfsBase)Shadowsocks.Obfs.ObfsFactory.GetObfs(obfs_value);
                int[] properties = obfs.GetObfs()[obfs_value];
                TextObfsParam.IsEnabled = properties[2] > 0;
            }
            catch
            {
                TextObfsParam.IsEnabled = true;
            }
        }

        private void ObfsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateObfsTextbox();
        }

        private void ObfsCombo_DropDownClosed(object sender, object e)
        {
            UpdateObfsTextbox();
        }

        private void ObfsCombo_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateObfsTextbox();
        }

        private void checkSSRLink_Changed(object sender, RoutedEventArgs e)
        {
            if (_modifiedConfiguration == null)
                return;
            int change = SaveOldSelectedServer();
            if (change == 1)
            {
                LoadConfiguration(_modifiedConfiguration);
            }
            LoadSelectedServer();
        }

        private void checkAdvSetting_Changed(object sender, RoutedEventArgs e)
        {
            Update_SSR_controls_Visable();
        }

        private void Update_SSR_controls_Visable()
        {
            bool adv = checkAdvSetting.IsChecked == true;
            labelUDPPort.Visibility = adv ? Visibility.Visible : Visibility.Collapsed;
            NumUDPPort.Visibility = adv ? Visibility.Visible : Visibility.Collapsed;
            UDPoverTCPLabel.Visibility = adv ? Visibility.Visible : Visibility.Collapsed;
            CheckUDPoverUDP.Visibility = adv ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
