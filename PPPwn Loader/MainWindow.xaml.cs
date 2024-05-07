using Microsoft.Win32;
using Panuon.WPF.UI;
using PPPwn_Loader.Tools;
using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace PPPwn_Loader
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : WindowX
    {
        private enum RunningState
        {
            STATE_NOT_READY,
            STATE_REQ_INTERFACE,
            STATE_REQ_FW_VER,
            STATE_REQ_STAGE2,
            STATE_READY,
            STATE_RUNNING
        }

        private string[] allFwVersion = { "8.50", "9.00", "9.03", "9.04", "9.50", "9.60", "10.00", "10.01", "10.50", "10.70", "10.71", "11.00" };

        private RunningState runningState = RunningState.STATE_NOT_READY;

        private string ethName = null;
        private string fwVersion = null;
        private string stage2Path = null;

        private bool isFirstLine = true;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void WindowX_Loaded(object sender, RoutedEventArgs e)
        {
            GetVersion();
            RestoreConfig();
            StatusChanged();
            Task.Run(async () => await CloseAllPppwnProcessesAsync());

            if (!IsNpcapInstalled())
            {
                if (MessageBoxX.Show(this, "Checked that Npcap is not installed, will start installation soon", "Tip", MessageBoxButton.OK) != MessageBoxResult.Cancel)
                {
                    Task.Run(async () => await InstallNpcap());
                }
            }
        }

        private void GetVersion()
        {
            Version version = Assembly.GetEntryAssembly().GetName().Version;
            Title += " v" + version.Major + "." + version.Minor;
        }

        private void RestoreConfig()
        {
            ethName = ConfigHelper.GetAppConfig("ethName");
            fwVersion = ConfigHelper.GetAppConfig("fwVersion");
            stage2Path = ConfigHelper.GetAppConfig("stage2Path");
            lbPPPwnVer.Content = "PPPwn Version: v" + ConfigHelper.GetAppConfig("pppwnVer");
            if (!string.IsNullOrEmpty(stage2Path))
            {
                btnFile.Content = "Stage2 File: " + stage2Path;
            }
            else
            {
                btnFile.Content = "Select Stage2 File...";
            }
        }

        private void LoadStage2File()
        {
            // 创建文件选择框对象
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // 设置过滤条件为.bin文件
            openFileDialog.Filter = "Stage2 File (*.bin)|*.bin|All files (*.*)|*.*";

            // 打开文件选择框
            bool? result = openFileDialog.ShowDialog();

            // 检查是否选择了文件
            if (result == true)
            {
                // 获取选定的文件路径并保存到全局变量中
                stage2Path = openFileDialog.FileName;
                btnFile.Content = "Stage2 File: " + stage2Path;
                ConfigHelper.UpdateAppConfig("stage2Path", stage2Path);
            }
            else
            {
                Toast("The Stage2 File is not selected.");
            }
        }

        private async Task RunPPPwn()
        {
            try
            {
                string pppwnFath = @".\PPPwn\pppwn.exe";
                string newFwVer = fwVersion.Replace(".", "");
                string arguments = $"--interface=\"{ethName}\" --fw={newFwVer} --stage1=.\\PPPwn\\stage1\\{newFwVer}\\stage1.bin --stage2={stage2Path}";

                // 创建进程对象
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = pppwnFath; // 设置要执行的程序路径
                    process.StartInfo.Arguments = arguments; // 设置程序参数

                    Console.WriteLine(pppwnFath + " " + arguments);

                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;

                    // 订阅异步读取输出内容的事件
                    process.OutputDataReceived += (sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            // 在UI线程上更新UI元素
                            Dispatcher.Invoke(() =>
                            {
                                if (args.Data.Contains("STAGE 0"))
                                {
                                    btnStart.Content = "STAGE 0";
                                    pbProgress.Value = 20;
                                }
                                else if (args.Data.Contains("STAGE 1"))
                                {
                                    btnStart.Content = "STAGE 1";
                                    pbProgress.Value = 40;
                                }
                                else if (args.Data.Contains("STAGE 2"))
                                {
                                    btnStart.Content = "STAGE 2";
                                    pbProgress.Value = 60;
                                }
                                else if (args.Data.Contains("STAGE 3"))
                                {
                                    btnStart.Content = "STAGE 3";
                                    pbProgress.Value = 80;
                                }
                                else if (args.Data.Contains("STAGE 4"))
                                {
                                    btnStart.Content = "STAGE 4";
                                    pbProgress.Value = 90;
                                }
                                else if (args.Data.Contains("Done"))
                                {
                                    btnStart.Content = "DONE";
                                    pbProgress.Value = 100;
                                }
                                string newItem = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " - " + args.Data;
                                if (isFirstLine)
                                {
                                    isFirstLine = false;
                                    tbConsole.Text += newItem;
                                }
                                else
                                {
                                    tbConsole.Text += "\n" + newItem;
                                }
                                lbStatus.Content = args.Data;
                            });
                        }
                    };

                    // 异步启动进程
                    process.Start();

                    // 异步开始读取输出流
                    process.BeginOutputReadLine();

                    // 等待进程结束
                    await Task.Run(() => process.WaitForExit());

                    if (lbStatus.Content.ToString().Contains("failed"))
                    {
                        var result = MessageBoxX.Show(this, lbStatus.Content.ToString() + " Retry it?", "Result", MessageBoxButton.OKCancel, MessageBoxIcon.None, DefaultButton.YesOK, 5);
                        if (result == MessageBoxResult.Cancel)
                        {
                            RefreshUI(false);
                        }
                        else
                        {
                            await StartPPPwn();
                        }
                    }
                    else if (lbStatus.Content.ToString().Contains("Done"))
                    {
                        runningState = RunningState.STATE_READY;
                    }
                }
            }
            catch (Exception ex)
            {
                Toast(ex.Message);
            }
        }

        private async Task StartPPPwn()
        {
            RefreshUI(true);
            await RunPPPwn();
        }

        private async Task CloseAllPppwnProcessesAsync()
        {
            try
            {
                // 获取所有名为 "pppwn.exe" 的进程
                Process[] processes = Process.GetProcessesByName("pppwn");

                // 强制关闭所有 "pppwn.exe" 进程
                foreach (Process process in processes)
                {
                    process.Kill();
                    await Task.Delay(1000); // 等待一段时间确保进程被终止
                    process.Dispose();
                }
            }
            catch (Exception ex)
            {
                //Toast(ex.Message);
            }
        }

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            switch (runningState)
            {
                case RunningState.STATE_REQ_INTERFACE:
                    Toast("Please select a Network Interface.");
                    break;
                case RunningState.STATE_REQ_FW_VER:
                    Toast("Please select a Firmware Verion.");
                    break;
                case RunningState.STATE_REQ_STAGE2:
                    Toast("Please select a Stage2 File.");
                    break;
                case RunningState.STATE_READY:
                    await StartPPPwn();
                    break;
                case RunningState.STATE_RUNNING:
                    var result = MessageBoxX.Show(this, "PPPwn is on the run, confirmed exit?", "Tip", MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.OK)
                    {
                        btnStart.IsEnabled = false;
                        await StopPPPwn();
                    }
                    break;
                default: break;
            }
        }

        private async Task StopPPPwn()
        {
            await CloseAllPppwnProcessesAsync();
            RefreshUI(false);
        }

        private void btnFile_Click(object sender, RoutedEventArgs e)
        {
            LoadStage2File();
            StatusChanged();
        }

        private void StatusChanged()
        {
            if (!string.IsNullOrEmpty(ethName) && !string.IsNullOrEmpty(fwVersion) && !string.IsNullOrEmpty(stage2Path))
            {
                runningState = RunningState.STATE_READY;
                btnStart.Content = "START";
            }
            else
            {
                if (string.IsNullOrEmpty(ethName) || string.IsNullOrEmpty(fwVersion))
                {
                    if (string.IsNullOrEmpty(ethName))
                    {
                        runningState = RunningState.STATE_REQ_INTERFACE;
                    }
                    if (string.IsNullOrEmpty(fwVersion))
                    {
                        runningState = RunningState.STATE_REQ_FW_VER;
                    }
                    OpenSettings(true);
                }
                if (string.IsNullOrEmpty(stage2Path))
                {
                    runningState = RunningState.STATE_REQ_STAGE2;
                }
                btnStart.Content = "READY";
            }
        }

        private void RefreshUI(bool isRunning)
        {
            if (isRunning)
            {
                btnFile.IsEnabled = false;
                btnSettings.IsEnabled = false;
                btnStart.Content = "WAIT";
                btnStart.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6CBCEA"));
                lbStatus.Content = "Waiting for PPPoE connection...";
                runningState = RunningState.STATE_RUNNING;
            }
            else
            {
                btnFile.IsEnabled = true;
                btnSettings.IsEnabled = true;
                btnStart.Content = "START";
                btnStart.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                lbStatus.Content = "Ready to run Exploit.";
                runningState = RunningState.STATE_READY;
            }
            btnStart.IsEnabled = true;
            pbProgress.Value = 0;
        }

        private void WindowX_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (runningState == RunningState.STATE_RUNNING)
            {
                var result = MessageBoxX.Show(this, "PPPwn is on the run, confirmed exit?", "Tip", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.Cancel || result == MessageBoxResult.None)
                {
                    e.Cancel = true;
                }
            }
        }

        private async void WindowX_Closed(object sender, EventArgs e)
        {
            await CloseAllPppwnProcessesAsync();
            Environment.Exit(0);
        }

        private bool IsNpcapInstalled()
        {
            // Npcap安装后会在注册表中创建一个相关的项
            // 检查这个项是否存在来判断Npcap是否已经安装
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Npcap", false);
            return key != null;
        }

        private async Task InstallNpcap()
        {
            try
            {
                string npcapFath = @".\Drivers\npcap-1.79.exe";

                // 创建进程对象
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = npcapFath; // 设置要执行的程序路径

                    // 异步启动进程
                    process.Start();

                    // 等待进程结束
                    await Task.Run(() => process.WaitForExit());
                }
            }
            catch (Exception ex)
            {
                Toast(ex.Message);
            }
        }

        private void btnConsole_Click(object sender, RoutedEventArgs e)
        {
            gdConsole.Visibility = gdConsole.IsVisible ? Visibility.Hidden : Visibility.Visible;
        }

        private void tbConsole_TextChanged(object sender, TextChangedEventArgs e)
        {
            tbConsole.ScrollToEnd();
        }

        private void OpenSettings(bool isAuto)
        {
            var settings = new Settings(isAuto);
            if (settings.ShowDialog() == false)
            {
                RestoreConfig();
                StatusChanged();
            }
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            OpenSettings(false);
        }
    }
}
