using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace PWin11Updater
{
    public partial class MainWindow : Window
    {
        private readonly string appPath = AppDomain.CurrentDomain.BaseDirectory; // Корневая директория PWin11 Tweaker
        private readonly string zipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "release.zip");
        private readonly string tempUnzipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tempUpdate");
        private readonly string versionLocalPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Update", "versionLocal.xml");
        private string localVersion;
        private string serverVersion;
        private CancellationTokenSource cancelTokenSource;

        public MainWindow()
        {
            InitializeComponent();
            LoadLocalVersion();
        }

        private void LoadLocalVersion()
        {
            if (File.Exists(versionLocalPath))
            {
                try
                {
                    var xmlDoc = new XmlDocument();
                    xmlDoc.Load(versionLocalPath);
                    localVersion = xmlDoc.SelectSingleNode("//version")?.InnerText;
                    LocalVersionText.Text = $"Local Version: {localVersion ?? "Not Available"}";
                    Debug.WriteLine($"Local version loaded: {localVersion}");
                }
                catch (Exception ex)
                {
                    LocalVersionText.Text = "Local Version: Error loading version";
                    Debug.WriteLine($"Error loading local version: {ex.Message}");
                }
            }
            else
            {
                LocalVersionText.Text = "Local Version: File not found";
            }
        }

        private async void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatusText.Text = "Update Status: Checking...";
            UpdateProgressBar.Visibility = Visibility.Visible;
            UpdateProgressBar.Value = 0;

            try
            {
                await CheckForUpdatesAsync();

                if (serverVersion != null && serverVersion != localVersion)
                {
                    UpdateStatusText.Text = $"Update Status: New version {serverVersion} available. Downloading...";
                    await DownloadAndInstallUpdateAsync();
                    UpdateStatusText.Text = "Update Status: Update completed. Please restart.";
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
            using (var client = new HttpClient())
            {
                try
                {
                    var response = await client.GetStringAsync("https://raw.githubusercontent.com/PWin11-Tweaker/PWin11-Tweaker/refs/heads/main/PWin11%20Tweaker's/Assets/versionServer.xml");
                    var xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(response);
                    serverVersion = xmlDoc.SelectSingleNode("//version")?.InnerText;
                    ServerVersionText.Text = $"Server Version: {serverVersion ?? "Not Available"}";
                    Debug.WriteLine($"Server version: {serverVersion}");
                }
                catch (Exception ex)
                {
                    ServerVersionText.Text = "Server Version: Error fetching version";
                    Debug.WriteLine($"Error fetching server version: {ex.Message}");
                    throw;
                }
            }
        }

        private async Task DownloadAndInstallUpdateAsync()
        {
            UpdateProgressBar.Value = 10;
            Debug.WriteLine("DownloadAndInstallUpdateAsync: Starting download...");

            cancelTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancelTokenSource.Token;

            // Проверка и закрытие процесса PWin11 Tweaker
            if (IsProcessRunning("PWin11 Tweaker's"))
            {
                UpdateStatusText.Text = "Update Status: Closing PWin11 Tweaker...";
                CloseProcess("PWin11 Tweaker's");
                await Task.Delay(2000); // Ждём закрытия процесса
            }

            // Убедимся, что директория для zipPath существует
            string zipDirectory = Path.GetDirectoryName(zipPath) ?? appPath;
            if (!Directory.Exists(zipDirectory))
            {
                Directory.CreateDirectory(zipDirectory);
                Debug.WriteLine($"Created directory: {zipDirectory}");
            }

            using (var client = new HttpClient())
            {
                var progress = new Progress<long>((bytes) =>
                {
                    UpdateProgressBar.Value = (bytes / 1024.0 / 1024.0) / 10 * 100; // Примерная оценка прогресса
                });

                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, "https://github.com/PWin11-Tweaker/PWin11-Tweaker/releases/latest/download/release.zip");
                    var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write))
                    {
                        await stream.CopyToAsync(fileStream, 81920, cancellationToken); // Буфер 80KB
                    }

                    UpdateProgressBar.Value = 50;
                    Debug.WriteLine("DownloadAndInstallUpdateAsync: Download completed.");

                    // Распаковка
                    if (Directory.Exists(tempUnzipPath)) Directory.Delete(tempUnzipPath, true);
                    Directory.CreateDirectory(tempUnzipPath);
                    UpdateProgressBar.Value = 60;

                    using (var zip = ZipFile.OpenRead(zipPath))
                    {
                        int entryCount = zip.Entries.Count;
                        UpdateProgressBar.Maximum = entryCount;
                        int extractProgress = 0;

                        foreach (var entry in zip.Entries)
                        {
                            string extractPath = Path.Combine(tempUnzipPath, entry.FullName);
                            string extractDir = Path.GetDirectoryName(extractPath);
                            if (!string.IsNullOrEmpty(extractDir) && !Directory.Exists(extractDir))
                            {
                                Directory.CreateDirectory(extractDir);
                                Debug.WriteLine($"Created directory: {extractDir}");
                            }
                            entry.ExtractToFile(extractPath, true);
                            UpdateProgressBar.Value = ++extractProgress;
                        }
                    }

                    UpdateProgressBar.Value = 80;
                    Debug.WriteLine("DownloadAndInstallUpdateAsync: Extraction completed.");

                    // Замена файлов, кроме исполняемых
                    string[] excludeFiles = { "PWin11Updater.exe", "PWin11 Tweaker's.exe" }; // Исключаем исполняемые файлы
                    foreach (string file in Directory.GetFiles(tempUnzipPath, "*.*", SearchOption.AllDirectories))
                    {
                        // Ручная реализация относительного пути
                        string relativePath = file.Substring(tempUnzipPath.Length + 1); // Удаляем tempUnzipPath и слеш
                        string destPath = Path.Combine(appPath, relativePath);
                        string destDir = Path.GetDirectoryName(destPath);

                        if (!excludeFiles.Contains(Path.GetFileName(destPath), StringComparer.OrdinalIgnoreCase))
                        {
                            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                            {
                                Directory.CreateDirectory(destDir);
                                Debug.WriteLine($"Created directory: {destDir}");
                            }
                            File.Copy(file, destPath, true);
                            Debug.WriteLine($"Copied file: {file} to {destPath}");
                        }
                    }

                    UpdateProgressBar.Value = 100;
                    File.Delete(zipPath);
                    Directory.Delete(tempUnzipPath, true);
                    Debug.WriteLine("DownloadAndInstallUpdateAsync: Update installed.");
                }
                catch (Exception ex)
                {
                    UpdateStatusText.Text = $"Update Status: Error - {ex.Message}";
                    Debug.WriteLine($"DownloadAndInstallUpdateAsync: Error - {ex.Message}");
                    throw;
                }
            }
        }

        private bool IsProcessRunning(string processName)
        {
            return Process.GetProcessesByName(processName.Replace("'", "")).Length > 0;
        }

        private void CloseProcess(string processName)
        {
            foreach (var process in Process.GetProcessesByName(processName.Replace("'", "")))
            {
                process.Kill();
                process.WaitForExit();
                Debug.WriteLine($"Closed process: {processName}");
            }
        }
    }
}