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
        private bool isUpdating = false; // Флаг состояния обновления

        public MainWindow()
        {
            InitializeComponent();
            LoadLocalVersion();
            OpenProgramButton.IsEnabled = true; // Изначально кнопка активна
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
            if (isUpdating) return; // Предотвращаем повторный запуск обновления
            isUpdating = true;
            OpenProgramButton.IsEnabled = false; // Отключаем кнопку во время обновления
            UpdateStatusText.Text = "Update Status: Checking...";

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
                isUpdating = false;
                OpenProgramButton.IsEnabled = true; // Включаем кнопку после завершения
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
            Debug.WriteLine("DownloadAndInstallUpdateAsync: Starting download...");

            cancelTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancelTokenSource.Token;

            if (IsProcessRunning("PWin11 Tweaker's"))
            {
                UpdateStatusText.Text = "Update Status: Closing PWin11 Tweaker's...";
                CloseProcess("PWin11 Tweaker's");
                await Task.Delay(2000);
            }

            string zipDirectory = Path.GetDirectoryName(zipPath) ?? appPath;
            if (!Directory.Exists(zipDirectory))
            {
                Directory.CreateDirectory(zipDirectory);
                Debug.WriteLine($"Created directory: {zipDirectory}");
            }

            using (var client = new HttpClient())
            {
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

                    Debug.WriteLine("DownloadAndInstallUpdateAsync: Download completed.");

                    // Распаковка
                    if (Directory.Exists(tempUnzipPath)) Directory.Delete(tempUnzipPath, true);
                    Directory.CreateDirectory(tempUnzipPath);

                    using (var zip = ZipFile.OpenRead(zipPath))
                    {
                        int entryCount = zip.Entries.Count;
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
                            if (entry.Length > 0 || !entry.FullName.EndsWith("/"))
                            {
                                entry.ExtractToFile(extractPath, true);
                            }

                        }
                    }

                    Debug.WriteLine("DownloadAndInstallUpdateAsync: Extraction completed.");
                    string[] excludeFiles = { "PWin11Updater.exe" };
                    foreach (string file in Directory.GetFiles(tempUnzipPath, "*.*", SearchOption.AllDirectories))
                    {
                        string relativePath = file.Substring(tempUnzipPath.Length + 1);
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

                    string sourceExe = Path.Combine(tempUnzipPath, "PWin11 Tweaker's.exe");
                    string destExe = Path.Combine(appPath, "PWin11 Tweaker's.exe");
                    if (File.Exists(sourceExe))
                    {
                        if (!string.IsNullOrEmpty(Path.GetDirectoryName(destExe)) && !Directory.Exists(Path.GetDirectoryName(destExe)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(destExe));
                        }
                        File.Copy(sourceExe, destExe, true);
                        Debug.WriteLine($"Copied PWin11 Tweaker's.exe from {sourceExe} to {destExe}");
                    }
                    else
                    {
                        Debug.WriteLine($"PWin11 Tweaker's.exe not found in {tempUnzipPath}");
                    }

                    File.Delete(zipPath);
                    Directory.Delete(tempUnzipPath, true);
                    Debug.WriteLine("DownloadAndInstallUpdateAsync: Update installed.");
                }
                catch (Exception ex)
                {
                    UpdateStatusText.Text = $"Update Status: Error - {ex.Message}";
                    Debug.WriteLine($"DownloadAndInstallUpdateAsync: Error - {ex.Message}\nStackTrace: {ex.StackTrace}");
                    throw;
                }
            }
        }

        private void OpenProgramButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isUpdating) // Запуск только если обновление не активно
            {
                string exePath = Path.Combine(appPath, "PWin11 Tweaker's.exe");
                if (File.Exists(exePath))
                {
                    Process.Start(exePath);
                    Debug.WriteLine($"Opened PWin11 Tweaker's.exe: {exePath}");
                }
                else
                {
                    Debug.WriteLine($"PWin11 Tweaker's.exe not found at: {exePath}");
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