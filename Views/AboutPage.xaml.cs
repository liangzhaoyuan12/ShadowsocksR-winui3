using Microsoft.UI.Xaml.Controls;
using Shadowsocks.Controller;

namespace ShadowsocksR_winui3.Views
{
    public sealed partial class AboutPage : Page, IPageController
    {
        public AboutPage()
        {
            this.InitializeComponent();
            UpdateTexts();
        }

        public void OnShow(int arg)
        {
        }

        private void UpdateTexts()
        {
            HeaderText.Text = I18N.GetString("About");
            ProjectUrlLabel.Text = I18N.GetString("Project URL");
            LicenseLabel.Text = I18N.GetString("Open source license");
            DeveloperLabel.Text = I18N.GetString("Developer");
        }
    }
}
