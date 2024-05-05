using Panuon.WPF.UI;
using PPPwn_Loader.Tools;
using System;
using System.Net.NetworkInformation;
using System.Reflection;
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

        private string[] allFwVersion = { "8.50", "9.00", "9.03", "9.04", "9.50", "9.60", "10.00", "10.01", "10.50", "10.70", "10.71", "11.00" };

        private string ethName = null;
        private string fwVersion = null;

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
                btnApply.Visibility = Visibility.Visible;
            }
            else
            {
                btnCheck.Visibility = Visibility.Visible;
                btnApply.Visibility = Visibility.Hidden;
            }

            InitConfig();
            CheckNewVerison(true);
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
        }

        private void CheckNewVerison(bool isAuto)
        {
            string userOrOrgName = "PokersKun";
            string repoName = "PPPwn-Loader";
            string currentVersion = null;

            Version version = Assembly.GetEntryAssembly().GetName().Version;
            currentVersion = version.Major + "." + version.Minor;

            UpdateChecker checker = new UpdateChecker(userOrOrgName, repoName, currentVersion);
            checker.HasNewVersion(result => {
                if (result)
                {
                    if (MessageBoxX.Show(this, "Check for new version, please update", "Tip", MessageBoxButton.OK) == MessageBoxResult.OK)
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

        private void btnCheck_Click(object sender, RoutedEventArgs e)
        {
            CheckNewVerison(false);
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
