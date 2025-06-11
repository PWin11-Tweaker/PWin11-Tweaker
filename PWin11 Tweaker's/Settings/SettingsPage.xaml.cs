using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Reflection;
using PWin11Update;

namespace PWin11_Tweaker_s
{
    public sealed partial class SettingsPage : Microsoft.UI.Xaml.Controls.Page
    {
        private readonly string localVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.12.6";
        private string serverVersion;

        public SettingsPage()
        {
            this.InitializeComponent();
            LoadLocalVersion();
            CheckServerVersionAsync();
        }

        private void LoadLocalVersion()
        {
            LocalVersionText.Text = $"Local Version: {localVersion}";
        }

        private async void CheckServerVersionAsync()
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.GetStringAsync("https://raw.githubusercontent.com/PWin11-Tweaker/PWin11-Tweaker/refs/heads/main/PWin11%20Tweaker's/Assets/versionServer.xml");
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(response);
                serverVersion = xmlDoc.SelectSingleNode("//version")?.InnerText;
                ServerVersionText.Text = $"Server Version: {serverVersion ?? "Not Available"}";
            }
            catch (Exception ex)
            {
                ServerVersionText.Text = "Server Version: Error fetching version";
                Debug.WriteLine($"CheckServerVersionAsync: Error - {ex.Message}");
            }
        }

        private async void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatusText.Text = "Update Status: Checking...";
                UpdateProgressBar.Visibility = Visibility.Visible;
                UpdateProgressBar.Value = 0;

                await CheckForUpdatesAsync();

                if (serverVersion != null && serverVersion != localVersion)
                {
                    UpdateStatusText.Text = $"Update Status: New version {serverVersion} available. Starting updater...";
                    // Запуск окна обновления
                    var updaterWindow = new MainWindow();
                    updaterWindow.Activate(); // Показать окно
                    Application.Current.Exit(); // Завершить текущее приложение
                }
                else
                {
                    UpdateStatusText.Text = "Update Status: You are on the latest version.";
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText.Text = $"Update Status: Error - {ex.Message}";
                Debug.WriteLine($"CheckUpdatesButton_Click: Error - {ex.Message}");
            }
            finally
            {
                UpdateProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        private async Task CheckForUpdatesAsync()
        {
            using var client = new HttpClient();
            try
            {
                var response = await client.GetStringAsync("https://raw.githubusercontent.com/PWin11-Tweaker/PWin11-Tweaker/refs/heads/main/PWin11%20Tweaker's/Assets/versionServer.xml");
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(response);
                serverVersion = xmlDoc.SelectSingleNode("//version")?.InnerText;
                ServerVersionText.Text = $"Server Version: {serverVersion ?? "Not Available"}";
            }
            catch (Exception ex)
            {
                ServerVersionText.Text = "Server Version: Error fetching version";
                Debug.WriteLine($"CheckForUpdatesAsync: Error - {ex.Message}");
                throw;
            }
        }
    }
}