using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Shadowsocks.Controller;
using Shadowsocks.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ShadowsocksR_winui3.Views
{
    public sealed partial class PortMapPage : Page, IPageController
    {
        private ShadowsocksController controller;
        private Configuration _modifiedConfiguration;
        private int _oldSelectedIndex = -1;
        private bool _ignoreSelection;
        private ObservableCollection<string> _listItems = new ObservableCollection<string>();

        public PortMapPage()
        {
            this.InitializeComponent();

            controller = ShadowsocksR_winui3.App.Controller;
            listPorts.ItemsSource = _listItems;

            UpdateTexts();

            comboBoxType.Items.Add(I18N.GetString("Port Forward"));
            comboBoxType.Items.Add(I18N.GetString("Force Proxy"));
            comboBoxType.Items.Add(I18N.GetString("Proxy With Rule"));
            comboBoxType.SelectedIndex = 0;

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
        }

        private void UpdateTexts()
        {
            HeaderText.Text = I18N.GetString("Port Settings");
            groupBox1Title.Text = I18N.GetString("Map Setting");
            labelType.Text = I18N.GetString("Type");
            labelID.Text = I18N.GetString("Server ID");
            labelAddr.Text = I18N.GetString("Target Addr");
            labelPort.Text = I18N.GetString("Target Port");
            checkEnable.Content = I18N.GetString("Enable");
            labelLocal.Text = I18N.GetString("Local Port");
            label1.Text = I18N.GetString("Remarks");
            OKButton.Content = I18N.GetString("OK");
            MyCancelButton.Content = I18N.GetString("Cancel");
            Add.Content = I18N.GetString("&Add");
            Del.Content = I18N.GetString("&Delete");
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
        }

        private void LoadCurrentConfiguration()
        {
            _modifiedConfiguration = controller.GetConfiguration();
            LoadConfiguration(_modifiedConfiguration);
            LoadSelectedServer();
        }

        private void LoadConfiguration(Configuration configuration)
        {
            comboServers.Items.Clear();
            comboServers.Items.Add("");
            Dictionary<string, int> server_group = new Dictionary<string, int>();
            foreach (Server s in configuration.configs)
            {
                if (!string.IsNullOrEmpty(s.group) && !server_group.ContainsKey(s.group))
                {
                    comboServers.Items.Add("#" + s.group);
                    server_group[s.group] = 1;
                }
            }
            foreach (Server s in configuration.configs)
            {
                comboServers.Items.Add(GetDisplayText(s));
            }
            int[] list = new int[configuration.portMap.Count];
            int list_index = 0;
            foreach (KeyValuePair<string, PortMapConfig> it in configuration.portMap)
            {
                try
                {
                    list[list_index] = int.Parse(it.Key);
                }
                catch (FormatException)
                {
                }
                list_index += 1;
            }
            Array.Sort(list);
            _ignoreSelection = true;
            _listItems.Clear();
            for (int i = 0; i < list.Length; ++i)
            {
                string portKey = list[i].ToString();
                string remarks = "";
                if (configuration.portMap.ContainsKey(portKey))
                {
                    remarks = configuration.portMap[portKey].remarks ?? "";
                }
                _listItems.Add(portKey + "    " + remarks);
            }
            _ignoreSelection = false;
            _oldSelectedIndex = -1;
            if (_listItems.Count > 0)
            {
                listPorts.SelectedIndex = 0;
            }
        }

        private string ServerListText2Key(string text)
        {
            if (text != null)
            {
                int pos = text.IndexOf(' ');
                if (pos > 0)
                    return text.Substring(0, pos);
            }
            return text;
        }

        private string GetID(string text)
        {
            if (text.IndexOf('#') >= 0)
            {
                return text.Substring(text.IndexOf('#') + 1);
            }
            return text;
        }

        private string GetDisplayText(Server s)
        {
            return (!string.IsNullOrEmpty(s.group) ? s.group + " - " : "    - ") + s.FriendlyName() + "        #" + s.id;
        }

        private string GetIDText(string id)
        {
            foreach (Server s in _modifiedConfiguration.configs)
            {
                if (id == s.id)
                {
                    return GetDisplayText(s);
                }
            }
            return "";
        }

        private string ComboServersText
        {
            get
            {
                string text = comboServers.SelectedItem as string;
                return text ?? "";
            }
            set
            {
                int index = -1;
                for (int i = 0; i < comboServers.Items.Count; ++i)
                {
                    string item = comboServers.Items[i] as string;
                    if (item == value)
                    {
                        index = i;
                        break;
                    }
                }
                comboServers.SelectedIndex = index;
            }
        }

        private string LocalPortText
        {
            get
            {
                if (double.IsNaN(NumLocalPort.Value))
                    return NumLocalPort.Text;
                return ((int)NumLocalPort.Value).ToString();
            }
            set
            {
                int port;
                if (int.TryParse(value, out port))
                    NumLocalPort.Value = port;
                else
                    NumLocalPort.Text = value;
            }
        }

        private int TargetPortValue
        {
            get
            {
                if (double.IsNaN(NumTargetPort.Value))
                    return 0;
                return (int)NumTargetPort.Value;
            }
            set
            {
                NumTargetPort.Value = value;
            }
        }

        private PortMapType SelectedPortMapType
        {
            get
            {
                if (comboBoxType.SelectedIndex < 0 || comboBoxType.SelectedIndex >= comboBoxType.Items.Count)
                    return PortMapType.Forward;
                return (PortMapType)comboBoxType.SelectedIndex;
            }
            set
            {
                int index = (int)value;
                if (index >= 0 && index < comboBoxType.Items.Count)
                    comboBoxType.SelectedIndex = index;
            }
        }

        private void SaveSelectedServer()
        {
            if (_oldSelectedIndex != -1)
            {
                bool reflash_list = false;
                string key = _oldSelectedIndex.ToString();
                string localPortText = LocalPortText;
                if (key != localPortText)
                {
                    if (_modifiedConfiguration.portMap.ContainsKey(key))
                    {
                        _modifiedConfiguration.portMap.Remove(key);
                    }
                    reflash_list = true;
                    key = localPortText;
                    try
                    {
                        _oldSelectedIndex = int.Parse(key);
                    }
                    catch (FormatException)
                    {
                        _oldSelectedIndex = 0;
                    }
                }
                if (!_modifiedConfiguration.portMap.ContainsKey(key))
                {
                    _modifiedConfiguration.portMap[key] = new PortMapConfig();
                }
                PortMapConfig cfg = _modifiedConfiguration.portMap[key] as PortMapConfig;

                cfg.enable = checkEnable.IsChecked == true;
                cfg.type = SelectedPortMapType;
                cfg.id = GetID(ComboServersText);
                cfg.server_addr = textAddr.Text;
                if (cfg.remarks != textRemarks.Text)
                {
                    reflash_list = true;
                }
                cfg.remarks = textRemarks.Text;
                cfg.server_port = TargetPortValue;
                if (reflash_list)
                {
                    LoadConfiguration(_modifiedConfiguration);
                }
            }
        }

        private void LoadSelectedServer()
        {
            string key = ServerListText2Key(listPorts.SelectedItem as string);
            Dictionary<string, int> server_group = new Dictionary<string, int>();
            foreach (Server s in _modifiedConfiguration.configs)
            {
                if (!string.IsNullOrEmpty(s.group) && !server_group.ContainsKey(s.group))
                {
                    server_group[s.group] = 1;
                }
            }
            if (key != null && _modifiedConfiguration.portMap.ContainsKey(key))
            {
                PortMapConfig cfg = _modifiedConfiguration.portMap[key] as PortMapConfig;

                checkEnable.IsChecked = cfg.enable;
                SelectedPortMapType = cfg.type;
                string text = GetIDText(cfg.id);
                if (text.Length == 0 && cfg.id != null && server_group.ContainsKey(cfg.id))
                {
                    text = "#" + cfg.id;
                }
                ComboServersText = text;
                LocalPortText = key;
                textAddr.Text = cfg.server_addr ?? "";
                TargetPortValue = cfg.server_port;
                textRemarks.Text = cfg.remarks ?? "";

                try
                {
                    _oldSelectedIndex = int.Parse(key);
                }
                catch (FormatException)
                {
                    _oldSelectedIndex = 0;
                }
            }
        }

        private void listPorts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_ignoreSelection)
                return;
            SaveSelectedServer();
            LoadSelectedServer();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            SaveSelectedServer();
            string key = "0";
            if (!_modifiedConfiguration.portMap.ContainsKey(key))
            {
                _modifiedConfiguration.portMap[key] = new PortMapConfig();
            }
            PortMapConfig cfg = _modifiedConfiguration.portMap[key] as PortMapConfig;

            cfg.enable = checkEnable.IsChecked == true;
            cfg.type = SelectedPortMapType;
            cfg.id = GetID(ComboServersText);
            cfg.server_addr = textAddr.Text;
            cfg.remarks = textRemarks.Text;
            cfg.server_port = TargetPortValue;

            _oldSelectedIndex = -1;
            LoadConfiguration(_modifiedConfiguration);
            LoadSelectedServer();
        }

        private void Del_Click(object sender, RoutedEventArgs e)
        {
            string key = _oldSelectedIndex.ToString();
            if (_modifiedConfiguration.portMap.ContainsKey(key))
            {
                _modifiedConfiguration.portMap.Remove(key);
            }
            _oldSelectedIndex = -1;
            LoadConfiguration(_modifiedConfiguration);
            LoadSelectedServer();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSelectedServer();
            controller.SaveServersPortMap(_modifiedConfiguration);
            ShadowsocksR_winui3.MainWindow.Instance?.HideWindow();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            LoadCurrentConfiguration();
            ShadowsocksR_winui3.MainWindow.Instance?.HideWindow();
        }

        private void comboBoxType_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxType.SelectedIndex == 0)
            {
                textAddr.IsReadOnly = false;
                NumTargetPort.IsEnabled = true;
                NumTargetPort.SmallChange = 1;
            }
            else
            {
                textAddr.IsReadOnly = true;
                NumTargetPort.IsEnabled = false;
                NumTargetPort.SmallChange = 0;
            }
        }
    }
}
