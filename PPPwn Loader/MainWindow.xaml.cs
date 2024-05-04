using Microsoft.Win32;
using Panuon.WPF.UI;
using PPPwn_Loader.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            STATE_REQ_PAYLOAD,
            STATE_READY,
            STATE_RUNNING
        }

        private string[] allFwVersion = { "9.00", "9.03", "9.04", "9.50", "9.60", "10.00", "10.01", "10.50", "10.70", "10.71", "11.00" };

        private RunningState runningState = RunningState.STATE_NOT_READY;

        private string ethName = null;
        private string fwVersion = null;
        private string payloadPath = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void WindowX_Loaded(object sender, RoutedEventArgs e)
        {
            GetVersion();
            InitConfig();
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

        private void InitConfig()
        {
            // 获取系统上的所有网络接口
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            // 遍历所有网络接口
            foreach (NetworkInterface networkInterface in networkInterfaces)
            {
                // 如果是以太网卡，则将其名称添加到ComboBox中
                if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    cbEthIf.Items.Add(networkInterface.Name);
                }
            }

            if (cbEthIf.Items.Count > 0)
            {
                ethName = ConfigHelper.GetAppConfig("ethName");
                if (string.IsNullOrEmpty(ethName) || !cbEthIf.Items.Contains(ethName))
                {
                    cbEthIf.SelectedIndex = 0;
                    ethName = cbEthIf.SelectedItem as string;
                    ConfigHelper.UpdateAppConfig("fwVersion", fwVersion);
                }
                else
                {
                    cbEthIf.Text = ethName;
                }
            }
            else
            {
                if (MessageBoxX.Show(this, "No Ethernet card found in your system", "Warning", MessageBoxButton.OK) != MessageBoxResult.Cancel)
                {
                    Close();
                }
            }

            cbFwVer.ItemsSource = allFwVersion;
            fwVersion = ConfigHelper.GetAppConfig("fwVersion");
            if (string.IsNullOrEmpty(fwVersion))
            {
                cbFwVer.SelectedIndex = 0;
                fwVersion = cbFwVer.SelectedItem as string;
                ConfigHelper.UpdateAppConfig("fwVersion", fwVersion);
            }
            else
            {
                cbFwVer.Text = fwVersion;
            }

            payloadPath = ConfigHelper.GetAppConfig("payloadPath");
            if (string.IsNullOrEmpty(payloadPath))
            {
                btnFile.Content = "Select Payload File...";
            }
            else
            {
                btnFile.Content = "Payload File: " + payloadPath;
            }
        }

        private void LoadPayloadFile()
        {
            // 创建文件选择框对象
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // 设置过滤条件为.bin文件
            openFileDialog.Filter = "Payload File (*.bin)|*.bin|All files (*.*)|*.*";

            // 打开文件选择框
            bool? result = openFileDialog.ShowDialog();

            // 检查是否选择了文件
            if (result == true)
            {
                // 获取选定的文件路径并保存到全局变量中
                payloadPath = openFileDialog.FileName;
                btnFile.Content = "Payload File: " + payloadPath;
                ConfigHelper.UpdateAppConfig("payloadPath", payloadPath);
            }
            else
            {
                Toast("The Payload File is not selected.");
            }
        }

        private async Task RunPPPwn()
        {
            try
            {
                string pppwnFath = @".\PPPwn\pppwn.exe";
                string newFwVer = fwVersion.Replace(".", "");
                string arguments = $"--interface=\"{ethName}\" --fw={newFwVer} --stage1=.\\PPPwn\\payload\\stage1.bin --stage2={payloadPath}";

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
                                Console.WriteLine(args.Data);
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

                    if (lbStatus.Content.ToString().Contains("failed") || lbStatus.Content.ToString().Contains("Done"))
                    {
                        MessageBoxX.Show(this, lbStatus.Content.ToString(), "Result", MessageBoxButton.OK);
                        RefreshUI(false);
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
                case RunningState.STATE_REQ_PAYLOAD:
                    Toast("Please select a Paykoad File.");
                    break;
                case RunningState.STATE_READY:
                    await StartPPPwn();
                    break;
                case RunningState.STATE_RUNNING:
                    MessageBoxResult result = MessageBoxX.Show(this, "PPPwn is on the run, confirmed exit?", "Tip", MessageBoxButton.OKCancel);
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

        private void cbEthIf_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ethName = (string)cbEthIf.SelectedItem;
            ConfigHelper.UpdateAppConfig("ethName", ethName);
            StatusChanged();
        }

        private void cbFwVer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            fwVersion = (string)cbFwVer.SelectedItem;
            ConfigHelper.UpdateAppConfig("fwVersion", fwVersion);
            StatusChanged();
        }

        private void btnFile_Click(object sender, RoutedEventArgs e)
        {
            LoadPayloadFile();
            StatusChanged();
        }

        private void StatusChanged()
        {
            if (!string.IsNullOrEmpty(ethName) && !string.IsNullOrEmpty(fwVersion) && !string.IsNullOrEmpty(payloadPath))
            {
                runningState = RunningState.STATE_READY;
                btnStart.Content = "START";
            }
            else
            {
                if (string.IsNullOrEmpty(ethName))
                {
                    runningState = RunningState.STATE_REQ_INTERFACE;
                }
                if (string.IsNullOrEmpty(fwVersion))
                {
                    runningState = RunningState.STATE_REQ_FW_VER;
                }
                if (string.IsNullOrEmpty(payloadPath))
                {
                    runningState = RunningState.STATE_REQ_PAYLOAD;
                }
                btnStart.Content = "READY";
            }
        }

        private void RefreshUI(bool isRunning)
        {
            if (isRunning)
            {
                cbEthIf.IsEnabled = false;
                cbFwVer.IsEnabled = false;
                btnFile.IsEnabled = false;
                btnStart.Content = "WAIT";
                btnStart.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6CBCEA"));
                lbStatus.Content = "Waiting for PPPoE connection...";
                runningState = RunningState.STATE_RUNNING;
            }
            else
            {
                cbEthIf.IsEnabled = true;
                cbFwVer.IsEnabled = true;
                btnFile.IsEnabled = true;
                btnStart.Content = "START";
                btnStart.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                lbStatus.Content = "";
                runningState = RunningState.STATE_READY;
            }
            btnStart.IsEnabled = true;
            pbProgress.Value = 0;
        }

        private void WindowX_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (runningState == RunningState.STATE_RUNNING)
            {
                MessageBoxResult result = MessageBoxX.Show(this, "PPPwn is on the run, confirmed exit?", "Tip", MessageBoxButton.OKCancel);
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
    }
}
