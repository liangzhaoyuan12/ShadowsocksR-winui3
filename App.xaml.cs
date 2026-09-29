using Microsoft.UI.Xaml;
using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.View;
using System;
using System.Threading.Tasks;

namespace ShadowsocksR_winui3
{
    public partial class App : Application
    {
        public static ShadowsocksController Controller { get; private set; }
        public static UpdateChecker UpdateChecker { get; private set; }
        public static MenuViewController ViewController { get; private set; }
        public static MainWindow MainWindowInstance { get; private set; }

        public App()
        {
            this.InitializeComponent();
            this.UnhandledException += App_UnhandledException;
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            try
            {
                Logging.Log(LogLevel.Error, e.Exception != null ? e.Exception.ToString() : "");
            }
            catch
            {
            }
            Shadowsocks.Program.ExitApplication();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _ = InitializeApplicationAsync();
        }

        private async Task InitializeApplicationAsync()
        {
            try
            {
                int tryTimes = 0;
                while (Configuration.Load() == null)
                {
                    if (tryTimes >= 5)
                    {
                        Shadowsocks.Program.ExitApplication();
                        return;
                    }
                    Views.InputPasswordWindow dlg = new Views.InputPasswordWindow();
                    bool? result = await dlg.ShowModalAsync();
                    if (result != true)
                    {
                        Shadowsocks.Program.ExitApplication();
                        return;
                    }
                    Configuration.SetPassword(dlg.Password);
                    tryTimes += 1;
                }

                Controller = new ShadowsocksController();
                HostMap.Instance().LoadHostFile();

                UpdateChecker = new UpdateChecker();
                ViewController = new MenuViewController(Controller);

                MainWindowInstance = new MainWindow();
                MainWindowInstance.ShowPage("servers");

                Controller.Start();
            }
            catch (Exception e)
            {
                Logging.LogUsefulException(e);
                NativeUi.ShowError(e.ToString());
                Shadowsocks.Program.ExitApplication();
            }
        }
    }
}
