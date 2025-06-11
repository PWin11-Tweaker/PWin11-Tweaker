using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;

namespace PWin11Update
{
    public sealed partial class MainWindow : Window
    {
        private readonly string localVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.12.6";
        private string serverVersion;

        public MainWindow()
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
                    UpdateStatusText.Text = $"Update Status: New version {serverVersion} available. Starting update...";
                    UpdateProgressBar.IsIndeterminate = true;

                    // Симуляция прогресса (замените на реальную логику обновления)
                    await DownloadAndInstallUpdateAsync();

                    UpdateStatusText.Text = "Update Status: Update completed successfully.";
                    UpdateProgressBar.Visibility = Visibility.Collapsed;

                    // Перезапуск основного приложения
                    string targetExe = Path.Combine(Environment.CurrentDirectory, "PWin11 Tweaker's.exe");
                    if (File.Exists(targetExe))
                    {
                        Process.Start(targetExe);
                    }
                    else
                    {
                        UpdateStatusText.Text = "Update Status: Error - Main application not found.";
                    }

                    // Закрытие окна обновления
                    Application.Current.Exit();
                }
                else
                {
                    UpdateStatusText.Text = "Update Status: You are on the latest version.";
                    UpdateProgressBar.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText.Text = $"Update Status: Error - {ex.Message}";
                Debug.WriteLine($"CheckUpdatesButton_Click: Error - {ex.Message}");
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

        private async Task DownloadAndInstallUpdateAsync()
        {
            try
            {
                UpdateStatusText.Text = "Update Status: Downloading update...";
                using var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, "https://github.com/PWin11-Tweaker/PWin11-Tweaker/releases/latest/download/release.zip");
                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();
                string zipPath = Path.Combine(Environment.CurrentDirectory, "release.zip");
                using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await stream.CopyToAsync(fileStream);

                UpdateStatusText.Text = "Update Status: Extracting update...";
                string tempUnzipPath = Path.Combine(Environment.CurrentDirectory, "tempUpdate");
                if (Directory.Exists(tempUnzipPath)) Directory.Delete(tempUnzipPath, true);
                Directory.CreateDirectory(tempUnzipPath);

                using var zip = ZipFile.OpenRead(zipPath);
                foreach (var entry in zip.Entries)
                {
                    string extractPath = Path.Combine(tempUnzipPath, entry.FullName);
                    string extractDir = Path.GetDirectoryName(extractPath);
                    if (!string.IsNullOrEmpty(extractDir) && !Directory.Exists(extractDir))
                    {
                        Directory.CreateDirectory(extractDir);
                    }
                    if (entry.Length > 0 || !entry.FullName.EndsWith("/"))
                    {
                        entry.ExtractToFile(extractPath, true);
                    }
                }

                UpdateStatusText.Text = "Update Status: Replacing files...";
                await Task.Delay(2000); // Задержка для освобождения файлов
                foreach (var process in Process.GetProcessesByName("PWin11 Tweaker's"))
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(5000);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to kill process {process.Id}: {ex.Message}");
                    }
                }

                string targetDir = Environment.CurrentDirectory;
                string[] filesToCopy = Directory.GetFiles(tempUnzipPath, "*.*", SearchOption.AllDirectories);

                foreach (string file in filesToCopy)
                {
                    string relativePath = Path.GetRelativePath(tempUnzipPath, file);
                    string destPath = Path.Combine(targetDir, relativePath);
                    string destDir = Path.GetDirectoryName(destPath);

                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    if (File.Exists(file))
                    {
                        try
                        {
                            if (File.Exists(destPath))
                            {
                                File.SetAttributes(destPath, FileAttributes.Normal);
                                File.Delete(destPath);
                            }
                            File.Copy(file, destPath, true);
                        }
                        catch (Exception ex)
                        {
                            UpdateStatusText.Text = $"Update Status: Error copying file - {ex.Message}";
                            throw;
                        }
                    }
                }

                UpdateStatusText.Text = "Update Status: Cleaning up...";
                File.Delete(zipPath);
                Directory.Delete(tempUnzipPath, true);
            }
            catch (Exception ex)
            {
                UpdateStatusText.Text = $"Update Status: Error - {ex.Message}";
                Debug.WriteLine($"DownloadAndInstallUpdateAsync: Error - {ex.Message}");
                throw;
            }
        }
    }
}