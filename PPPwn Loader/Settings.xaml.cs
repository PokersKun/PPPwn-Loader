using Panuon.WPF.UI;
using PPPwn_Loader.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PPPwn_Loader
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class Settings : WindowX
    {
        private bool _isAuto = false;

        private string ethName = null;
        private string fwVersion = null;
        private IPendingHandler updateHandler = null;

        public Settings(bool isAuto)
        {
            InitializeComponent();
            _isAuto = isAuto;
        }

        private void WindowX_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_isAuto)
            {
                btnCheck.Visibility = Visibility.Hidden;
            }
            else
            {
                btnCheck.Visibility = Visibility.Visible;
            }

            InitConfig();
            CheckNewVerison(true);
            CheckNewPPPwn(true);
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
                    ConfigHelper.UpdateAppConfig("ethName", ethName);
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
                    ConfigHelper.UpdateAppConfig("ethName", null);
                    Environment.Exit(0);
                }
            }

            string stage1Path = Path.Combine(Directory.GetCurrentDirectory(), "PPPwn", "stage1");
            if (Directory.Exists(stage1Path))
            {
                string[] allFwVersion = Directory.GetDirectories(stage1Path);
                for (int i = 0; i < allFwVersion.Length; i++)
                {
                    allFwVersion[i] = Path.GetFileName(allFwVersion[i]);
                }
                Array.Sort(allFwVersion, new NumericComparer());
                Array.Reverse(allFwVersion);
                string[] formattedallFwVersion = allFwVersion.Select(name => (Convert.ToDouble(name) / 100).ToString("0.00")).ToArray();

                cbFwVer.ItemsSource = formattedallFwVersion;
            }
            if (cbFwVer.Items.Count > 0)
            {
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
            }
            else
            {
                if (MessageBoxX.Show(this, "No Stage1 Files in your disk", "Warning", MessageBoxButton.OK) != MessageBoxResult.Cancel)
                {
                    ConfigHelper.UpdateAppConfig("fwVersion", null);
                    Environment.Exit(0);
                }
            }
        }

        private void CheckNewVerison(bool isAuto)
        {
            string userOrOrgName = "PokersKun";
            string repoName = "PPPwn-Loader";
            string currentVersion = null;

            Version version = Assembly.GetEntryAssembly().GetName().Version;
            currentVersion = version.Major + "." + version.Minor;

            UpdateChecker checker = new UpdateChecker(userOrOrgName, repoName, currentVersion);
            checker.HasNewVersion(result =>
            {
                if (result)
                {
                    if (MessageBoxX.Show(this, "Check for new version for PPPwn Loader, please update", "Tip", MessageBoxButton.OK) == MessageBoxResult.OK)
                    {
                        checker.OpenBrowserToReleases();
                    }
                }
                else
                {
                    if (!isAuto)
                    {
                        Toast("PPPwn Loader is up to date.");
                    }
                }
            });
        }

        private async Task DownloadFileAsync(string url, string filePath)
        {
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(5);
                using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    using (Stream stream = await response.Content.ReadAsStreamAsync())
                    {
                        using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            const int bufferSize = 8192;
                            byte[] buffer = new byte[bufferSize];
                            long downloadedBytes = 0;
                            long totalBytes = response.Content.Headers.ContentLength ?? -1;
                            int bytesRead;
                            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead);
                                downloadedBytes += bytesRead;
                                if (totalBytes > 0)
                                {
                                    double progress = (double)downloadedBytes / totalBytes;
                                    updateHandler.UpdateMessage($"Download Progress: {progress:P0}");
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ExtractZip(string zipFilePath, string extractPath)
        {
            ZipFile.ExtractToDirectory(zipFilePath, extractPath);
        }

        private void DeleteDirectory(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }
        }

        private async Task StartUpdatePPPwn(string url, string version)
        {
            updateHandler = PendingBox.Show(this, "Start updating PPPwn...");
            string downloadPath = @".\pppwn-update.zip";
            string extractPath = @".\PPPwn";

            try
            {
                await DownloadFileAsync(url, downloadPath);
                updateHandler.UpdateMessage("Download completed.");
                DeleteDirectory("PPPwn");
                Directory.CreateDirectory("PPPwn");
                ExtractZip(downloadPath, extractPath);
                updateHandler.UpdateMessage("Extraction completed.");
                File.Delete(downloadPath);
                ConfigHelper.UpdateAppConfig("pppwnVer", version.Replace("v", ""));
                await Task.Delay(1000);
                updateHandler.Close();
                Dispatcher.Invoke(() =>
                {
                    Toast("PPPwn has been updated.");
                });
            }
            catch (Exception ex)
            {
                updateHandler.Close();
                Dispatcher.Invoke(() =>
                {
                    Console.WriteLine(ex.Message);
                    Toast($"An error occurred: {ex.Message}");
                });
            }
        }

        private void CheckNewPPPwn(bool isAuto)
        {
            string userOrOrgName = "PokersKun";
            string repoName = "PPPwn";
            string currentVersion = ConfigHelper.GetAppConfig("pppwnVer");

            UpdateChecker checker = new UpdateChecker(userOrOrgName, repoName, currentVersion);
            checker.GetNewVersion(result =>
            {
                if (result != null)
                {
                    if (MessageBoxX.Show(this, "Check for new version for PPPwn, please update", "Tip", MessageBoxButton.OK) == MessageBoxResult.OK)
                    {
                        Task.Run(async () => await StartUpdatePPPwn(result.assets[0].browser_download_url, result.tag_name));
                    }
                }
                else
                {
                    if (!isAuto)
                    {
                        Toast("PPPwn is up to date.");
                    }
                }
            });
        }

        private void cbEthIf_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ethName = (string)cbEthIf.SelectedItem;
            ConfigHelper.UpdateAppConfig("ethName", ethName);
        }

        private void cbFwVer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            fwVersion = (string)cbFwVer.SelectedItem;
            ConfigHelper.UpdateAppConfig("fwVersion", fwVersion);
        }

        private async void btnCheck_Click(object sender, RoutedEventArgs e)
        {
            CheckNewVerison(false);
            await Task.Delay(1000);
            CheckNewPPPwn(false);
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        class NumericComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                double nx = Convert.ToDouble(x);
                double ny = Convert.ToDouble(y);
                return nx.CompareTo(ny);
            }
        }
    }
}
