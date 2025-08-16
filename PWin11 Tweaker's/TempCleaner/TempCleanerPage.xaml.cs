using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.System;

namespace PWin11_Tweaker_s.TempCleaner
{
    public sealed partial class TempCleanerPage : Page
    {
        private CancellationTokenSource? _cts;
        private long _tempFilesSize;
        private long _recycleBinSize;
        private long _browserCacheSize;
        private long _windowsUpdateCacheSize;
        private long _thumbnailsSize;
        private long _deliveryOptSize;
        private long _systemLogsSize;
        private long _oldWinFilesSize;
        private long _appCacheSize;
        private ObservableCollection<FileInfoModel> _previewFiles = new ObservableCollection<FileInfoModel>();
        private readonly string[] _exclusionPaths = new string[0];
        private bool _isPreviewVisible = false;

        public TempCleanerPage()
        {
            try
            {
                Debug.WriteLine("Attempting to initialize TempCleanerPage...");
                this.InitializeComponent();
                Debug.WriteLine("TempCleanerPage initialized successfully.");
                PreviewListView.ItemsSource = _previewFiles;
                LoadSizesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize TempCleanerPage: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private async void LoadSizesAsync()
        {
            try
            {
                Debug.WriteLine("Loading sizes for categories...");
                _tempFilesSize = await CalculateTempFilesSizeAsync();
                _recycleBinSize = await CalculateRecycleBinSizeAsync();
                _browserCacheSize = await CalculateBrowserCacheSizeAsync();
                _windowsUpdateCacheSize = await CalculateWindowsUpdateCacheSizeAsync();
                _thumbnailsSize = await CalculateThumbnailsSizeAsync();
                _deliveryOptSize = await CalculateDeliveryOptSizeAsync();
                _systemLogsSize = await CalculateSystemLogsSizeAsync();
                _oldWinFilesSize = await CalculateOldWinFilesSizeAsync();
                _appCacheSize = await CalculateAppCacheSizeAsync();

                DispatcherQueue.TryEnqueue(() =>
                {
                    TempFilesSizeText.Text = FormatSize(_tempFilesSize);
                    RecycleBinSizeText.Text = FormatSize(_recycleBinSize);
                    BrowserCacheSizeText.Text = FormatSize(_browserCacheSize);
                    WindowsUpdateCacheSizeText.Text = FormatSize(_windowsUpdateCacheSize);
                    ThumbnailsSizeText.Text = FormatSize(_thumbnailsSize);
                    DeliveryOptSizeText.Text = FormatSize(_deliveryOptSize);
                    SystemLogsSizeText.Text = FormatSize(_systemLogsSize);
                    OldWinFilesSizeText.Text = FormatSize(_oldWinFilesSize);
                    AppCacheSizeText.Text = FormatSize(_appCacheSize);
                    UpdateTotalSize();
                    UpdateStatistics();
                    Debug.WriteLine("UI updated with category sizes.");
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading sizes: {ex.Message}\nStackTrace: {ex.StackTrace}");
                await ShowErrorAsync($"Ошибка при подсчете размеров: {ex.Message}");
            }
        }

        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateTotalSize();
                UpdateStatistics();
                Debug.WriteLine("Total size updated after checkbox change.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CheckBox_Changed: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private void UpdateTotalSize()
        {
            long totalSize = 0;
            if (TempFilesCheckBox.IsChecked == true) totalSize += _tempFilesSize;
            if (RecycleBinCheckBox.IsChecked == true) totalSize += _recycleBinSize;
            if (BrowserCacheCheckBox.IsChecked == true) totalSize += _browserCacheSize;
            if (WindowsUpdateCacheCheckBox.IsChecked == true) totalSize += _windowsUpdateCacheSize;
            if (ThumbnailsCheckBox.IsChecked == true) totalSize += _thumbnailsSize;
            if (DeliveryOptCheckBox.IsChecked == true) totalSize += _deliveryOptSize;
            if (SystemLogsCheckBox.IsChecked == true) totalSize += _systemLogsSize;
            if (OldWinFilesCheckBox.IsChecked == true) totalSize += _oldWinFilesSize;
            if (AppCacheCheckBox.IsChecked == true) totalSize += _appCacheSize;

            var resourceLoader = new ResourceLoader();
            string totalSizeLabel = resourceLoader.GetString("TotalSizeText.Text");
            TotalSizeText.Text = string.Format(totalSizeLabel, FormatSize(totalSize));
        }

        private void UpdateStatistics()
        {
            var resourceLoader = new ResourceLoader();
            string stats = $"{resourceLoader.GetString("StatisticsLabel")}" +
                           $"Temp: {FormatSize(_tempFilesSize)}" +
                           $"Recycle Bin: {FormatSize(_recycleBinSize)}" +
                           $"Browser Cache: {FormatSize(_browserCacheSize)}" +
                           $"Windows Update: {FormatSize(_windowsUpdateCacheSize)}" +
                           $"Thumbnails: {FormatSize(_thumbnailsSize)}" +
                           $"Delivery Opt: {FormatSize(_deliveryOptSize)}" +
                           $"System Logs: {FormatSize(_systemLogsSize)}" +
                           $"Old Win Files: {FormatSize(_oldWinFilesSize)}" +
                           $"App Cache: {FormatSize(_appCacheSize)}";
            StatisticsText.Text = stats;
        }

        private async void CleanButton_Click(object sender, RoutedEventArgs e)
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    ProgressBar.Visibility = Visibility.Visible;
                    CleanButton.IsEnabled = false;
                });

                long totalFreedSpace = 0;
                if (TempFilesCheckBox.IsChecked == true) totalFreedSpace += await CleanTempFilesAsync(token);
                if (RecycleBinCheckBox.IsChecked == true) totalFreedSpace += await CleanRecycleBinAsync(token);
                if (BrowserCacheCheckBox.IsChecked == true) totalFreedSpace += await CleanBrowserCacheAsync(token);
                if (WindowsUpdateCacheCheckBox.IsChecked == true) totalFreedSpace += await CleanWindowsUpdateCacheAsync(token);
                if (ThumbnailsCheckBox.IsChecked == true) totalFreedSpace += await CleanThumbnailsAsync(token);
                if (DeliveryOptCheckBox.IsChecked == true) totalFreedSpace += await CleanDeliveryOptAsync(token);
                if (SystemLogsCheckBox.IsChecked == true) totalFreedSpace += await CleanSystemLogsAsync(token);
                if (OldWinFilesCheckBox.IsChecked == true) totalFreedSpace += await CleanOldWinFilesAsync(token);
                if (AppCacheCheckBox.IsChecked == true) totalFreedSpace += await CleanAppCacheAsync(token);

                DispatcherQueue.TryEnqueue(() =>
                {
                    UpdateTotalSize();
                    UpdateStatistics();
                });
                await UpdateStatusAsync($"Освобождено {FormatSize(totalFreedSpace)}.");
                _ = ShowNotificationAsync("Очистка завершена!");
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Cleaning operation was canceled.");
                await UpdateStatusAsync("Очистка отменена.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during cleaning: {ex.Message}\nStackTrace: {ex.StackTrace}");
                await ShowErrorAsync($"Ошибка при очистке: {ex.Message}");
            }
            finally
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    ProgressBar.Visibility = Visibility.Collapsed;
                    CleanButton.IsEnabled = true;
                });
                _cts?.Dispose();
                _cts = null;
            }
        }

        private async void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isPreviewVisible)
            {
                _previewFiles.Clear();
                long totalSize = 0;
                if (TempFilesCheckBox.IsChecked == true) totalSize += await AddPreviewFilesAsync(Path.GetTempPath(), "Temp Files");
                if (RecycleBinCheckBox.IsChecked == true) totalSize += await AddPreviewFilesAsync(@"C:\$Recycle.Bin", "Recycle Bin");
                if (BrowserCacheCheckBox.IsChecked == true) totalSize += await AddPreviewFilesAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Edge\User Data\Default\Cache"), "Browser Cache");
                if (WindowsUpdateCacheCheckBox.IsChecked == true) totalSize += await AddPreviewFilesAsync(@"C:\Windows\SoftwareDistribution\Download", "Windows Update");
                if (ThumbnailsCheckBox.IsChecked == true) totalSize += await AddPreviewFilesAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Windows\Explorer"), "Thumbnails");
                if (DeliveryOptCheckBox.IsChecked == true) totalSize += await AddPreviewFilesAsync(@"C:\Windows\SoftwareDistribution\DeliveryOptimization", "Delivery Opt");
                if (SystemLogsCheckBox.IsChecked == true) totalSize += await AddPreviewFilesAsync(@"C:\Windows\Logs", "System Logs");
                if (OldWinFilesCheckBox.IsChecked == true) totalSize += await AddPreviewFilesAsync(@"C:\Windows.old", "Old Win Files");
                if (AppCacheCheckBox.IsChecked == true) totalSize += await AddPreviewFilesAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Packages"), "App Cache");

                DispatcherQueue.TryEnqueue(() =>
                {
                    PreviewListView.Visibility = Visibility.Visible;
                    ToggleIcon.Visibility = Visibility.Visible;
                    ToggleIcon.Text = "\xE70E"; // ChevronUp (свернуть)
                    _isPreviewVisible = true;
                    UpdateStatistics();
                });
            }
            else
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    PreviewListView.Visibility = Visibility.Collapsed;
                    ToggleIcon.Text = "\xE70D"; // ChevronDown (развернуть)
                    _isPreviewVisible = false;
                });
            }
        }

        private async Task<long> AddPreviewFilesAsync(string path, string category)
        {
            long size = 0;
            try
            {
                if (Directory.Exists(path))
                {
                    var files = Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly).Take(100);
                    foreach (var file in files)
                    {
                        if (!_exclusionPaths.Contains(file))
                        {
                            var info = new FileInfo(file);
                            size += info.Length;
                            _previewFiles.Add(new FileInfoModel { Name = info.Name, Size = FormatSize(info.Length), Path = path });
                        }
                    }

                    long maxSubDirSize = 0;
                    string maxSubDirPath = path;
                    var subDirs = Directory.GetDirectories(path);
                    foreach (var subDir in subDirs)
                    {
                        long subDirSize = await CalculateDirectorySizeAsync(subDir);
                        if (subDirSize > maxSubDirSize)
                        {
                            maxSubDirSize = subDirSize;
                            maxSubDirPath = subDir;
                        }
                    }

                    if (maxSubDirSize > 0)
                    {
                        _previewFiles.Add(new FileInfoModel
                        {
                            Name = "[Largest Subdirectory]",
                            Size = FormatSize(maxSubDirSize),
                            Path = maxSubDirPath
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error previewing files in {category}: {ex.Message}");
            }
            return size;
        }

        private async Task<long> CalculateDirectorySizeAsync(string directoryPath)
        {
            return await Task.Run(() =>
            {
                long size = 0;
                try
                {
                    var dirInfo = new DirectoryInfo(directoryPath);
                    size = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                        .Take(10000)
                        .Sum(f => f.Length);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error calculating size of {directoryPath}: {ex.Message}");
                }
                return size;
            });
        }

        private async void FeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool success = await Launcher.LaunchUriAsync(new Uri("https://github.com/PWin11-Tweaker/PWin11-Tweaker/issues/new?labels=bug&template=bug-report---.md"));
                if (success)
                {
                    Debug.WriteLine("Feedback link opened successfully.");
                }
                else
                {
                    Debug.WriteLine("Failed to open feedback link.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening feedback link: {ex.Message}");
            }
        }

        private async Task ShowNotificationAsync(string message)
        {
            await Task.Run(() => DispatcherQueue.TryEnqueue(() =>
            {
                StatusText.Text = message;
                StatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
            }));
        }

        private async Task ShowErrorAsync(string message)
        {
            await Task.Run(() => DispatcherQueue.TryEnqueue(() =>
            {
                StatusText.Text = message;
                StatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
            }));
        }

        private async Task<long> CalculateTempFilesSizeAsync()
        {
            return await Task.Run(() =>
            {
                long size = 0;
                string tempPath = Path.GetTempPath();

                try
                {
                    if (Directory.Exists(tempPath))
                    {
                        var dirInfo = new DirectoryInfo(tempPath);
                        size = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                            .Take(10000)
                            .Sum(f => f.Length);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error calculating %TEMP% size: {ex.Message}");
                }

                return size;
            });
        }

        private async Task<long> CalculateRecycleBinSizeAsync()
        {
            return await Task.Run(async () =>
            {
                long size = 0;

                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-Command \"(Get-ChildItem -Path 'C:\\$Recycle.Bin' -Recurse -Force | Measure-Object -Property Length -Sum).Sum\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using (Process process = new Process())
                    {
                        process.StartInfo = startInfo;
                        process.Start();
                        string output = await process.StandardOutput.ReadToEndAsync();
                        await process.WaitForExitAsync();

                        if (process.ExitCode == 0 && long.TryParse(output.Trim(), out long result))
                        {
                            size = result;
                        }
                        else
                        {
                            string error = await process.StandardError.ReadToEndAsync();
                            Debug.WriteLine($"Error calculating Recycle Bin size: {error}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error calculating Recycle Bin size: {ex.Message}");
                }

                return size;
            });
        }

        private async Task<long> CalculateBrowserCacheSizeAsync()
        {
            return await Task.Run(() =>
            {
                long size = 0;
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                try
                {
                    string edgeCachePath = Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Cache");
                    if (Directory.Exists(edgeCachePath))
                    {
                        var dirInfo = new DirectoryInfo(edgeCachePath);
                        size += dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                            .Take(10000)
                            .Sum(f => f.Length);
                    }

                    string chromeCachePath = Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Cache");
                    if (Directory.Exists(chromeCachePath))
                    {
                        var dirInfo = new DirectoryInfo(chromeCachePath);
                        size += dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                            .Take(10000)
                            .Sum(f => f.Length);
                    }

                    string firefoxProfilesPath = Path.Combine(localAppData, @"Mozilla\Firefox\Profiles");
                    if (Directory.Exists(firefoxProfilesPath))
                    {
                        foreach (var profileDir in Directory.GetDirectories(firefoxProfilesPath))
                        {
                            string firefoxCachePath = Path.Combine(profileDir, "cache");
                            if (Directory.Exists(firefoxCachePath))
                            {
                                var dirInfo = new DirectoryInfo(firefoxCachePath);
                                size += dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                                    .Take(10000)
                                    .Sum(f => f.Length);
                            }

                            string firefoxCache2Path = Path.Combine(profileDir, "cache2");
                            if (Directory.Exists(firefoxCache2Path))
                            {
                                var dirInfo = new DirectoryInfo(firefoxCache2Path);
                                size += dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                                    .Take(10000)
                                    .Sum(f => f.Length);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error calculating browser cache size: {ex.Message}");
                }

                return size;
            });
        }

        private async Task<long> CalculateWindowsUpdateCacheSizeAsync()
        {
            return await Task.Run(() =>
            {
                long size = 0;
                string updateCachePath = @"C:\Windows\SoftwareDistribution\Download";

                try
                {
                    if (Directory.Exists(updateCachePath))
                    {
                        var dirInfo = new DirectoryInfo(updateCachePath);
                        size = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                            .Take(10000)
                            .Sum(f => f.Length);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error calculating Windows Update cache size: {ex.Message}");
                }

                return size;
            });
        }

        private async Task<long> CalculateThumbnailsSizeAsync()
        {
            return await Task.Run(() =>
            {
                long size = 0;
                string thumbnailsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Microsoft\Windows\Explorer");

                try
                {
                    if (Directory.Exists(thumbnailsPath))
                    {
                        var dirInfo = new DirectoryInfo(thumbnailsPath);
                        size = dirInfo.EnumerateFiles("thumbcache_*.db", SearchOption.TopDirectoryOnly)
                            .Sum(f => f.Length);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error calculating thumbnails size: {ex.Message}");
                }

                return size;
            });
        }

        private async Task<long> CalculateDeliveryOptSizeAsync()
        {
            return await Task.Run(() =>
            {
                long size = 0;
                string deliveryOptPath = @"C:\Windows\SoftwareDistribution\DeliveryOptimization";

                try
                {
                    if (Directory.Exists(deliveryOptPath))
                    {
                        var dirInfo = new DirectoryInfo(deliveryOptPath);
                        size = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                            .Take(10000)
                            .Sum(f => f.Length);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error calculating Delivery Optimization size: {ex.Message}");
                }

                return size;
            });
        }

        private async Task<long> CalculateSystemLogsSizeAsync()
        {
            return await Task.Run(() =>
            {
                long size = 0;
                string logsPath = @"C:\Windows\Logs";

                try
                {
                    if (Directory.Exists(logsPath))
                    {
                        var dirInfo = new DirectoryInfo(logsPath);
                        size = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                            .Take(10000)
                            .Sum(f => f.Length);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error calculating System Logs size: {ex.Message}");
                }

                return size;
            });
        }

        private async Task<long> CalculateOldWinFilesSizeAsync()
        {
            return await Task.Run(() =>
            {
                long size = 0;
                string[] oldWinPaths = { @"C:\Windows.old", @"C:\$Windows.~WS" };

                try
                {
                    foreach (var path in oldWinPaths)
                    {
                        if (Directory.Exists(path))
                        {
                            var dirInfo = new DirectoryInfo(path);
                            size += dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                                .Take(10000)
                                .Sum(f => f.Length);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error calculating Old Windows Files size: {ex.Message}");
                }

                return size;
            });
        }

        private async Task<long> CalculateAppCacheSizeAsync()
        {
            return await Task.Run(() =>
            {
                long size = 0;
                string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Packages");

                try
                {
                    if (Directory.Exists(appDataPath))
                    {
                        var dirInfo = new DirectoryInfo(appDataPath);
                        size = dirInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly)
                            .SelectMany(d => d.EnumerateFiles("*", SearchOption.AllDirectories))
                            .Take(10000)
                            .Sum(f => f.Length);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error calculating App Cache size: {ex.Message}");
                }

                return size;
            });
        }

        private async Task<long> CleanTempFilesAsync(CancellationToken token)
        {
            return await Task.Run(() =>
            {
                long freedSpace = 0;
                string tempPath = Path.GetTempPath();

                try
                {
                    token.ThrowIfCancellationRequested();

                    if (Directory.Exists(tempPath))
                    {
                        foreach (var file in Directory.GetFiles(tempPath, "*", SearchOption.AllDirectories))
                        {
                            token.ThrowIfCancellationRequested();
                            try
                            {
                                FileInfo fileInfo = new FileInfo(file);
                                freedSpace += fileInfo.Length;
                                File.Delete(file);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error deleting temp file {file}: {ex.Message}");
                            }
                        }

                        foreach (var dir in Directory.GetDirectories(tempPath, "*", SearchOption.AllDirectories))
                        {
                            token.ThrowIfCancellationRequested();
                            try
                            {
                                Directory.Delete(dir, true);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error deleting temp directory {dir}: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error cleaning %TEMP%: {ex.Message}");
                }

                return freedSpace;
            }, token);
        }

        private async Task<long> CleanRecycleBinAsync(CancellationToken token)
        {
            return await Task.Run(async () =>
            {
                long freedSpace = 0;

                try
                {
                    token.ThrowIfCancellationRequested();

                    long initialSize = await CalculateRecycleBinSizeAsync();
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-Command Clear-RecycleBin -Force",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using (Process process = new Process())
                    {
                        process.StartInfo = startInfo;
                        process.Start();
                        await process.WaitForExitAsync(token);

                        if (process.ExitCode != 0)
                        {
                            string error = await process.StandardError.ReadToEndAsync();
                            Debug.WriteLine($"Error cleaning Recycle Bin: {error}");
                        }
                        else
                        {
                            freedSpace = initialSize;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error cleaning Recycle Bin: {ex.Message}");
                }

                return freedSpace;
            }, token);
        }

        private async Task<long> CleanBrowserCacheAsync(CancellationToken token)
        {
            return await Task.Run(() =>
            {
                long freedSpace = 0;
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                try
                {
                    token.ThrowIfCancellationRequested();

                    string edgeCachePath = Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Cache");
                    if (Directory.Exists(edgeCachePath))
                    {
                        foreach (var file in Directory.GetFiles(edgeCachePath, "*", SearchOption.AllDirectories))
                        {
                            token.ThrowIfCancellationRequested();
                            try
                            {
                                FileInfo fileInfo = new FileInfo(file);
                                freedSpace += fileInfo.Length;
                                File.Delete(file);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error deleting Edge cache file {file}: {ex.Message}");
                            }
                        }

                        foreach (var dir in Directory.GetDirectories(edgeCachePath, "*", SearchOption.AllDirectories))
                        {
                            token.ThrowIfCancellationRequested();
                            try
                            {
                                Directory.Delete(dir, true);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error deleting Edge cache directory {dir}: {ex.Message}");
                            }
                        }
                    }

                    string chromeCachePath = Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Cache");
                    if (Directory.Exists(chromeCachePath))
                    {
                        foreach (var file in Directory.GetFiles(chromeCachePath, "*", SearchOption.AllDirectories))
                        {
                            token.ThrowIfCancellationRequested();
                            try
                            {
                                FileInfo fileInfo = new FileInfo(file);
                                freedSpace += fileInfo.Length;
                                File.Delete(file);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error deleting Chrome cache file {file}: {ex.Message}");
                            }
                        }

                        foreach (var dir in Directory.GetDirectories(chromeCachePath, "*", SearchOption.AllDirectories))
                        {
                            token.ThrowIfCancellationRequested();
                            try
                            {
                                Directory.Delete(dir, true);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error deleting Chrome cache directory {dir}: {ex.Message}");
                            }
                        }
                    }

                    string firefoxProfilesPath = Path.Combine(localAppData, @"Mozilla\Firefox\Profiles");
                    if (Directory.Exists(firefoxProfilesPath))
                    {
                        foreach (var profileDir in Directory.GetDirectories(firefoxProfilesPath))
                        {
                            string firefoxCachePath = Path.Combine(profileDir, "cache");
                            if (Directory.Exists(firefoxCachePath))
                            {
                                foreach (var file in Directory.GetFiles(firefoxCachePath, "*", SearchOption.AllDirectories))
                                {
                                    token.ThrowIfCancellationRequested();
                                    try
                                    {
                                        FileInfo fileInfo = new FileInfo(file);
                                        freedSpace += fileInfo.Length;
                                        File.Delete(file);
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Error deleting Firefox cache file {file}: {ex.Message}");
                                    }
                                }

                                foreach (var dir in Directory.GetDirectories(firefoxCachePath, "*", SearchOption.AllDirectories))
                                {
                                    token.ThrowIfCancellationRequested();
                                    try
                                    {
                                        Directory.Delete(dir, true);
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Error deleting Firefox cache directory {dir}: {ex.Message}");
                                    }
                                }
                            }

                            string firefoxCache2Path = Path.Combine(profileDir, "cache2");
                            if (Directory.Exists(firefoxCache2Path))
                            {
                                foreach (var file in Directory.GetFiles(firefoxCache2Path, "*", SearchOption.AllDirectories))
                                {
                                    token.ThrowIfCancellationRequested();
                                    try
                                    {
                                        FileInfo fileInfo = new FileInfo(file);
                                        freedSpace += fileInfo.Length;
                                        File.Delete(file);
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Error deleting Firefox cache2 file {file}: {ex.Message}");
                                    }
                                }

                                foreach (var dir in Directory.GetDirectories(firefoxCache2Path, "*", SearchOption.AllDirectories))
                                {
                                    token.ThrowIfCancellationRequested();
                                    try
                                    {
                                        Directory.Delete(dir, true);
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Error deleting Firefox cache2 directory {dir}: {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error cleaning browser cache: {ex.Message}");
                }

                return freedSpace;
            }, token);
        }

        private async Task<long> CleanWindowsUpdateCacheAsync(CancellationToken token)
        {
            return await Task.Run(async () =>
            {
                long freedSpace = 0;
                string updateCachePath = @"C:\Windows\SoftwareDistribution\Download";

                try
                {
                    token.ThrowIfCancellationRequested();

                    ProcessStartInfo stopServiceInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/c net stop wuauserv",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using (Process stopProcess = new Process())
                    {
                        stopProcess.StartInfo = stopServiceInfo;
                        stopProcess.Start();
                        await stopProcess.WaitForExitAsync(token);
                        if (stopProcess.ExitCode != 0)
                        {
                            string error = await stopProcess.StandardError.ReadToEndAsync();
                            Debug.WriteLine($"Error stopping Windows Update service: {error}");
                        }
                    }

                    if (Directory.Exists(updateCachePath))
                    {
                        var dirInfo = new DirectoryInfo(updateCachePath);
                        freedSpace = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                            .Sum(f => f.Length);

                        foreach (var file in Directory.GetFiles(updateCachePath, "*", SearchOption.AllDirectories))
                        {
                            token.ThrowIfCancellationRequested();
                            try
                            {
                                File.Delete(file);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error deleting Windows Update cache file {file}: {ex.Message}");
                            }
                        }

                        foreach (var dir in Directory.GetDirectories(updateCachePath, "*", SearchOption.AllDirectories))
                        {
                            token.ThrowIfCancellationRequested();
                            try
                            {
                                Directory.Delete(dir, true);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error deleting Windows Update cache directory {dir}: {ex.Message}");
                            }
                        }
                    }

                    ProcessStartInfo startServiceInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/c net start wuauserv",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using (Process startProcess = new Process())
                    {
                        startProcess.StartInfo = startServiceInfo;
                        startProcess.Start();
                        await startProcess.WaitForExitAsync(token);
                        if (startProcess.ExitCode != 0)
                        {
                            string error = await startProcess.StandardError.ReadToEndAsync();
                            Debug.WriteLine($"Error starting Windows Update service: {error}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error cleaning Windows Update cache: {ex.Message}");
                }

                return freedSpace;
            }, token);
        }

        private async Task<long> CleanThumbnailsAsync(CancellationToken token)
        {
            return await Task.Run(async () =>
            {
                long freedSpace = 0;
                string thumbnailsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Microsoft\Windows\Explorer");

                try
                {
                    token.ThrowIfCancellationRequested();

                    if (Directory.Exists(thumbnailsPath))
                    {
                        var dirInfo = new DirectoryInfo(thumbnailsPath);
                        freedSpace = dirInfo.EnumerateFiles("thumbcache_*.db", SearchOption.TopDirectoryOnly)
                            .Sum(f => f.Length);

                        foreach (var file in Directory.GetFiles(thumbnailsPath, "thumbcache_*.db", SearchOption.TopDirectoryOnly))
                        {
                            token.ThrowIfCancellationRequested();
                            try
                            {
                                File.Delete(file);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error deleting thumbnail file {file}: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error cleaning thumbnails: {ex.Message}");
                }

                return freedSpace;
            }, token);
        }

        private async Task<long> CleanDeliveryOptAsync(CancellationToken token)
        {
            return await Task.Run(() =>
            {
                long freedSpace = 0;
                string deliveryOptPath = @"C:\Windows\SoftwareDistribution\DeliveryOptimization";

                try
                {
                    token.ThrowIfCancellationRequested();

                    if (Directory.Exists(deliveryOptPath))
                    {
                        var dirInfo = new DirectoryInfo(deliveryOptPath);
                        freedSpace = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                            .Sum(f => f.Length);

                        foreach (var file in Directory.GetFiles(deliveryOptPath, "*", SearchOption.AllDirectories))
                        {
                            token.ThrowIfCancellationRequested();
                            try
                            {
                                File.Delete(file);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error deleting Delivery Opt file {file}: {ex.Message}");
                            }
                        }

                        foreach (var dir in Directory.GetDirectories(deliveryOptPath, "*", SearchOption.AllDirectories))
                        {
                            token.ThrowIfCancellationRequested();
                            try
                            {
                                Directory.Delete(dir, true);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error deleting Delivery Opt directory {dir}: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error cleaning Delivery Optimization: {ex.Message}");
                }

                return freedSpace;
            }, token);
        }

        private async Task<long> CleanSystemLogsAsync(CancellationToken token)
        {
            return await Task.Run(() =>
            {
                long freedSpace = 0;
                string logsPath = @"C:\Windows\Logs";

                try
                {
                    token.ThrowIfCancellationRequested();

                    if (Directory.Exists(logsPath))
                    {
                        var dirInfo = new DirectoryInfo(logsPath);
                        freedSpace = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                            .Sum(f => f.Length);

                        foreach (var file in Directory.GetFiles(logsPath, "*", SearchOption.AllDirectories))
                        {
                            token.ThrowIfCancellationRequested();
                            try
                            {
                                File.Delete(file);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error deleting System Log file {file}: {ex.Message}");
                            }
                        }

                        foreach (var dir in Directory.GetDirectories(logsPath, "*", SearchOption.AllDirectories))
                        {
                            token.ThrowIfCancellationRequested();
                            try
                            {
                                Directory.Delete(dir, true);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error deleting System Log directory {dir}: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error cleaning System Logs: {ex.Message}");
                }

                return freedSpace;
            }, token);
        }

        private async Task<long> CleanOldWinFilesAsync(CancellationToken token)
        {
            return await Task.Run(() =>
            {
                long freedSpace = 0;
                string[] oldWinPaths = { @"C:\Windows.old", @"C:\$Windows.~WS" };

                try
                {
                    token.ThrowIfCancellationRequested();

                    foreach (var path in oldWinPaths)
                    {
                        if (Directory.Exists(path))
                        {
                            var dirInfo = new DirectoryInfo(path);
                            freedSpace += dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                                .Sum(f => f.Length);

                            foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                            {
                                token.ThrowIfCancellationRequested();
                                try
                                {
                                    File.Delete(file);
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Error deleting Old Win file {file}: {ex.Message}");
                                }
                            }

                            foreach (var dir in Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
                            {
                                token.ThrowIfCancellationRequested();
                                try
                                {
                                    Directory.Delete(dir, true);
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Error deleting Old Win directory {dir}: {ex.Message}");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error cleaning Old Windows Files: {ex.Message}");
                }

                return freedSpace;
            }, token);
        }

        private async Task<long> CleanAppCacheAsync(CancellationToken token)
        {
            return await Task.Run(() =>
            {
                long freedSpace = 0;
                string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Packages");

                try
                {
                    token.ThrowIfCancellationRequested();

                    if (Directory.Exists(appDataPath))
                    {
                        var dirInfo = new DirectoryInfo(appDataPath);
                        freedSpace = dirInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly)
                            .SelectMany(d => d.EnumerateFiles("*", SearchOption.AllDirectories))
                            .Sum(f => f.Length);

                        foreach (var dir in Directory.GetDirectories(appDataPath, "*", SearchOption.TopDirectoryOnly))
                        {
                            token.ThrowIfCancellationRequested();
                            try
                            {
                                Directory.Delete(dir, true);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error deleting App Cache directory {dir}: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error cleaning App Cache: {ex.Message}");
                }

                return freedSpace;
            }, token);
        }

        private string FormatSize(long bytes)
        {
            double mb = bytes / 1024.0 / 1024.0;
            if (mb >= 1024)
            {
                double gb = mb / 1024.0;
                return $"{gb:F2} GB";
            }
            return $"{mb:F2} MB";
        }

        private async Task UpdateStatusAsync(string message)
        {
            await Task.Run(() => DispatcherQueue.TryEnqueue(() =>
            {
                StatusText.Text = message;
                StatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
            }));
        }

        private class FileInfoModel
        {
            public string? Name { get; set; }
            public string? Size { get; set; }
            public string? Path { get; set; }
        }
    }
}