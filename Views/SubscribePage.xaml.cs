using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Shadowsocks.Controller;
using Shadowsocks.Model;
using System;
using System.Collections.ObjectModel;

namespace ShadowsocksR_winui3.Views
{
    public sealed partial class SubscribePage : Page, IPageController
    {
        private ShadowsocksController controller;
        private Configuration _modifiedConfiguration;
        private int _old_select_index;
        private bool _ignoreSelection;
        private ObservableCollection<string> _listItems = new ObservableCollection<string>();

        public SubscribePage()
        {
            this.InitializeComponent();

            controller = ShadowsocksR_winui3.App.Controller;
            listServerSubscribe.ItemsSource = _listItems;

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
        }

        private void UpdateTexts()
        {
            HeaderText.Text = I18N.GetString("Subscribe Settings");
            label1.Text = I18N.GetString("URL");
            label2.Text = I18N.GetString("Group name");
            checkBoxAutoUpdate.Content = I18N.GetString("Auto update");
            buttonOK.Content = I18N.GetString("OK");
            buttonCancel.Content = I18N.GetString("Cancel");
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
        }

        private void LoadCurrentConfiguration()
        {
            _modifiedConfiguration = controller.GetConfiguration();
            LoadAllSettings();
            if (listServerSubscribe.Items.Count == 0)
            {
                textBoxURL.IsEnabled = false;
            }
            else
            {
                textBoxURL.IsEnabled = true;
            }
        }

        private void LoadAllSettings()
        {
            int select_index = 0;
            checkBoxAutoUpdate.IsChecked = _modifiedConfiguration.nodeFeedAutoUpdate;
            UpdateList();
            UpdateSelected(select_index);
            SetSelectIndex(select_index);
        }

        private int SaveAllSettings()
        {
            _modifiedConfiguration.nodeFeedAutoUpdate = checkBoxAutoUpdate.IsChecked == true;
            return 0;
        }

        private void UpdateList()
        {
            _ignoreSelection = true;
            _listItems.Clear();
            for (int i = 0; i < _modifiedConfiguration.serverSubscribes.Count; ++i)
            {
                ServerSubscribe ss = _modifiedConfiguration.serverSubscribes[i];
                _listItems.Add((String.IsNullOrEmpty(ss.Group) ? "    " : ss.Group + " - ") + ss.URL);
            }
            _ignoreSelection = false;
        }

        private void SetSelectIndex(int index)
        {
            if (index >= 0 && index < _modifiedConfiguration.serverSubscribes.Count)
            {
                listServerSubscribe.SelectedIndex = index;
            }
        }

        private void UpdateSelected(int index)
        {
            if (index >= 0 && index < _modifiedConfiguration.serverSubscribes.Count)
            {
                ServerSubscribe ss = _modifiedConfiguration.serverSubscribes[index];
                textBoxURL.Text = ss.URL ?? "";
                textBoxGroup.Text = ss.Group ?? "";
                _old_select_index = index;
            }
        }

        private void SaveSelected(int index)
        {
            if (index >= 0 && index < _modifiedConfiguration.serverSubscribes.Count)
            {
                ServerSubscribe ss = _modifiedConfiguration.serverSubscribes[index];
                ss.URL = textBoxURL.Text;
                ss.Group = textBoxGroup.Text;
            }
        }

        private void listServerSubscribe_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_ignoreSelection)
                return;
            int select_index = listServerSubscribe.SelectedIndex;
            if (_old_select_index == select_index)
                return;

            SaveSelected(_old_select_index);
            UpdateList();
            UpdateSelected(select_index);
            SetSelectIndex(select_index);
        }

        private void textBoxURL_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxGroup.Text = "";
        }

        private void buttonAdd_Click(object sender, RoutedEventArgs e)
        {
            SaveSelected(_old_select_index);
            int select_index = _modifiedConfiguration.serverSubscribes.Count;
            if (_old_select_index >= 0 && _old_select_index < _modifiedConfiguration.serverSubscribes.Count)
            {
                _modifiedConfiguration.serverSubscribes.Insert(select_index, new ServerSubscribe());
            }
            else
            {
                _modifiedConfiguration.serverSubscribes.Add(new ServerSubscribe());
            }
            UpdateList();
            UpdateSelected(select_index);
            SetSelectIndex(select_index);

            textBoxURL.IsEnabled = true;
        }

        private void buttonDel_Click(object sender, RoutedEventArgs e)
        {
            int select_index = listServerSubscribe.SelectedIndex;
            if (select_index >= 0 && select_index < _modifiedConfiguration.serverSubscribes.Count)
            {
                _modifiedConfiguration.serverSubscribes.RemoveAt(select_index);
                if (select_index >= _modifiedConfiguration.serverSubscribes.Count)
                {
                    select_index = _modifiedConfiguration.serverSubscribes.Count - 1;
                }
                UpdateList();
                UpdateSelected(select_index);
                SetSelectIndex(select_index);
            }
            if (listServerSubscribe.Items.Count == 0)
            {
                textBoxURL.IsEnabled = false;
            }
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            int select_index = listServerSubscribe.SelectedIndex;
            SaveSelected(select_index);
            if (SaveAllSettings() == -1)
            {
                return;
            }
            controller.SaveServersConfig(_modifiedConfiguration);
            ShadowsocksR_winui3.MainWindow.Instance?.HideWindow();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            LoadCurrentConfiguration();
            ShadowsocksR_winui3.MainWindow.Instance?.HideWindow();
        }
    }
}
