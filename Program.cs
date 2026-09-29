using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.View;
using ShadowsocksR_winui3;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Win32;

namespace Shadowsocks
{
    static class Program
    {
        private static Mutex _mutex;
        private static ShadowsocksController _controller;
        private static int exited = 0;

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length >= 2 && args[0] == "--guardian")
            {
                int guardianPid;
                if (int.TryParse(args[1], out guardianPid))
                {
                    RunGuardian(guardianPid);
                }
                return;
            }

            foreach (string arg in args)
            {
                if (arg == "--setautorun")
                {
                    if (!Controller.AutoStartup.Switch())
                    {
                        Environment.ExitCode = 1;
                    }
                    return;
                }
            }

            _mutex = new Mutex(false, "Global\\ShadowsocksR_" + Util.Utils.GetStableHashCode(Util.Utils.StartupPath));

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            try
            {
                SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
            }
            catch
            {
            }

            bool mutexAcquired;
            try
            {
                mutexAcquired = _mutex.WaitOne(0, false);
            }
            catch (AbandonedMutexException)
            {
                // previous instance was killed while holding the mutex;
                // the mutex is now ours, continue as normal
                mutexAcquired = true;
            }

            if (!mutexAcquired)
            {
                NativeUi.MessageBox(
                    I18N.GetString("Find Shadowsocks icon in your notify tray.") + "\n" +
                    I18N.GetString("If you want to start multiple Shadowsocks, make a copy in another directory."),
                    I18N.GetString("ShadowsocksR is already running."),
                    NativeUi.MB_OK | NativeUi.MB_ICONINFO);
                return;
            }

            SpawnGuardian();

            Directory.SetCurrentDirectory(Util.Utils.StartupPath);
            Logging.OpenLogFile();

            Microsoft.UI.Xaml.Application.Start((p) =>
            {
                var context = new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(
                    Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                new App();
            });

            _controller = App.Controller;
            if (_controller != null)
                _controller.Stop();
        }

        private static void SpawnGuardian()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = Util.Utils.GetExecutablePath(),
                    Arguments = "--guardian " + Process.GetCurrentProcess().Id.ToString(),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Util.Utils.StartupPath,
                };
                Process.Start(psi);
            }
            catch (Exception e)
            {
                try
                {
                    Logging.LogUsefulException(e);
                }
                catch
                {
                }
            }
        }

        private static void RunGuardian(int pid)
        {
            try
            {
                using (Process parent = Process.GetProcessById(pid))
                {
                    bool sameProcess = false;
                    try
                    {
                        sameProcess = string.Equals(parent.ProcessName,
                            Process.GetCurrentProcess().ProcessName,
                            StringComparison.OrdinalIgnoreCase);
                    }
                    catch
                    {
                    }
                    if (sameProcess)
                    {
                        parent.WaitForExit();
                    }
                }
            }
            catch (ArgumentException)
            {
                // parent already exited
            }
            catch (Exception)
            {
            }

            Thread.Sleep(1500);
            SystemProxy.DisableLoopbackProxyIfEnabled();
        }

        public static void ExitApplication()
        {
            if (Interlocked.Increment(ref exited) != 1)
                return;
            try
            {
                var controller = App.Controller;
                if (controller != null)
                    controller.Stop();
            }
            catch
            {
            }
            try
            {
                var viewController = App.ViewController;
                if (viewController != null)
                    viewController.Dispose();
            }
            catch
            {
            }
            Environment.Exit(0);
        }

        private static void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            try
            {
                var controller = App.Controller;
                switch (e.Mode)
                {
                    case PowerModes.Resume:
                        if (controller != null)
                        {
                            System.Timers.Timer timer = new System.Timers.Timer(5 * 1000);
                            timer.Elapsed += Timer_Elapsed;
                            timer.AutoReset = false;
                            timer.Enabled = true;
                            timer.Start();
                        }
                        break;
                    case PowerModes.Suspend:
                        if (controller != null)
                            controller.Stop();
                        break;
                }
            }
            catch (Exception ex)
            {
                Logging.LogUsefulException(ex);
            }
        }

        private static void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                var controller = App.Controller;
                if (controller != null)
                    controller.Start();
            }
            catch (Exception ex)
            {
                Logging.LogUsefulException(ex);
            }
            finally
            {
                try
                {
                    System.Timers.Timer timer = (System.Timers.Timer)sender;
                    timer.Enabled = false;
                    timer.Stop();
                    timer.Dispose();
                }
                catch (Exception ex)
                {
                    Logging.LogUsefulException(ex);
                }
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (Interlocked.Increment(ref exited) == 1)
            {
                try
                {
                    Logging.Log(LogLevel.Error, e.ExceptionObject != null ? e.ExceptionObject.ToString() : "");
                }
                catch
                {
                }
                try
                {
                    NativeUi.ShowError(I18N.GetString("Unexpected error, ShadowsocksR will exit.") +
                        Environment.NewLine + (e.ExceptionObject != null ? e.ExceptionObject.ToString() : ""));
                }
                catch
                {
                }
                Environment.Exit(1);
            }
        }
    }
}
