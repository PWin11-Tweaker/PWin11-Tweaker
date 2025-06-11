using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using PWin11_Tweaker_s.Script;
using Windows.System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace PWin11_Tweaker_s
{
    public sealed partial class SettingsPage : Microsoft.UI.Xaml.Controls.Page
    {
        private readonly string zipPath = "release.zip"; // Путь к временному ZIP-файлу
        private readonly string tempUnzipPath = "tempUpdate"; // Папка для распаковки
        private string localVersion = "1.12.8"; // Текущая версия приложения (замените на реальную)
        private string serverVersion;
        private CancellationTokenSource cancelTokenSource;

        public SettingsPage()
        {
            this.InitializeComponent();
            System.Diagnostics.Debug.WriteLine("SettingsPage: InitializeComponent завершён.");
            LoadLocalVersion(); // Загружаем локальную версию
        }

        private void LoadLocalVersion()
        {
            localVersion = "1.12.8"; 
            Debug.WriteLine($"SettingsPage: Local version loaded: {localVersion}");
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
                    UpdateStatusText.Text = $"Update Status: New version {serverVersion} available. Downloading...";
                    await DownloadAndInstallUpdateAsync();
                    UpdateStatusText.Text = "Update Status: Update completed. Restart the app to apply changes.";
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
                    var response = await client.GetStringAsync("https://raw.githubusercontent.com/phancyn/NightDAY.build/refs/heads/main/versionServer.xml");
                    var xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(response);
                    serverVersion = xmlDoc.SelectSingleNode("//version")?.InnerText;
                    Debug.WriteLine($"CheckForUpdatesAsync: Server version: {serverVersion}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"CheckForUpdatesAsync: Error fetching version - {ex.Message}");
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

            using (var client = new HttpClient())
            {
                var progress = new Progress<long>((bytes) =>
                {
                    UpdateProgressBar.Value = (bytes / 1024.0 / 1024.0) / 10 * 100; // Примерная оценка прогресса
                });

                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/drive/v3/files/1YoBviGKyi_GKwpLCdVxJe8XYd_uyASWA?alt=media&key=AIzaSyCdUn-3MMLar1y-cYa6j-hxpK5Ofd226WE");
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
                        int progress = 0;

                        foreach (var entry in zip.Entries)
                        {
                            entry.ExtractToFile(Path.Combine(tempUnzipPath, entry.FullName), true);
                            UpdateProgressBar.Value = ++progress;
                        }
                    }

                    UpdateProgressBar.Value = 80;
                    Debug.WriteLine("DownloadAndInstallUpdateAsync: Extraction completed.");

                    // Перемещение файлов (предполагается, что обновление в папке Build)
                    string targetDir = "Build"; // Адаптируйте под структуру PWin11 Tweaker
                    if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
                    Directory.CreateDirectory(targetDir);

                    foreach (string file in Directory.GetFiles(tempUnzipPath, "*.*", SearchOption.AllDirectories))
                    {
                        string relativePath = Path.GetRelativePath(tempUnzipPath, file);
                        string destPath = Path.Combine(targetDir, relativePath);
                        string destDir = Path.GetDirectoryName(destPath);
                        if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
                        File.Move(file, destPath, true);
                    }

                    UpdateProgressBar.Value = 100;
                    File.Delete(zipPath);
                    Directory.Delete(tempUnzipPath, true);
                    Debug.WriteLine("DownloadAndInstallUpdateAsync: Update installed.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"DownloadAndInstallUpdateAsync: Error - {ex.Message}");
                    throw;
                }
            }
        }

        private async void VisitGitHubButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("VisitGitHubButton_Click: Попытка открыть GitHub.");
                Uri gitHubUri = new Uri("https://github.com/PWin11-Tweaker/PWin11-Tweaker");
                bool success = await Launcher.LaunchUriAsync(gitHubUri);
                if (success)
                {
                    System.Diagnostics.Debug.WriteLine("VisitGitHubButton_Click: GitHub успешно открыт.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("VisitGitHubButton_Click: Не удалось открыть GitHub.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VisitGitHubButton_Click: Ошибка при открытии GitHub: {ex.Message}");
            }
        }

        private async void VisitWebsiteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("VisitWebsiteButton_Click: Попытка открыть веб-сайт.");
                Uri websiteUri = new Uri("https://t.me/ph1ncyn"); // Замените на ваш URL
                bool success = await Launcher.LaunchUriAsync(websiteUri);
                if (success)
                {
                    System.Diagnostics.Debug.WriteLine("VisitWebsiteButton_Click: Веб-сайт успешно открыт.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("VisitWebsiteButton_Click: Не удалось открыть веб-сайт.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VisitWebsiteButton_Click: Ошибка при открытии веб-сайта: {ex.Message}");
            }
        }

        private async void FeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("https://github.com/PWin11-Tweaker/PWin11-Tweaker/issues/new?labels=bug&template=bug-report---.md"));
            Debug.WriteLine("Feedback link opened.");
        }
    }
}