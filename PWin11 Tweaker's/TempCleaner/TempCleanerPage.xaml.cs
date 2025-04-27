using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Dispatching;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace PWin11_Tweaker_s.TempCleaner
{
    public sealed partial class TempCleanerPage : Page
    {
        private readonly string _logFilePath = "tempcleaner.log";
        private CancellationTokenSource _cts;
        private long _tempFilesSize;
        private long _recycleBinSize;
        private long _browserCacheSize;

        public TempCleanerPage()
        {
            try
            {
                Log("Attempting to initialize TempCleanerPage...");
                this.InitializeComponent();
                Log("TempCleanerPage initialized successfully.");
                LoadSizesAsync();
            }
            catch (Exception ex)
            {
                Log($"Failed to initialize TempCleanerPage: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw; // Для отладки, чтобы увидеть исключение в IDE
            }
        }

        private async void LoadSizesAsync()
        {
            try
            {
                Log("Loading sizes for categories...");
                // Подсчет размера для каждой категории
                _tempFilesSize = await CalculateTempFilesSizeAsync();
                Log($"Temp files size: {_tempFilesSize} bytes");
                _recycleBinSize = await CalculateRecycleBinSizeAsync();
                Log($"Recycle Bin size: {_recycleBinSize} bytes");
                _browserCacheSize = await CalculateBrowserCacheSizeAsync();
                Log($"Browser cache size: {_browserCacheSize} bytes");

                // Обновление UI
                DispatcherQueue.TryEnqueue(() =>
                {
                    TempFilesSizeText.Text = FormatSize(_tempFilesSize);
                    RecycleBinSizeText.Text = FormatSize(_recycleBinSize);
                    BrowserCacheSizeText.Text = FormatSize(_browserCacheSize);
                    UpdateTotalSize();
                    Log("UI updated with category sizes.");
                });
            }
            catch (Exception ex)
            {
                Log($"Error loading sizes: {ex.Message}\nStackTrace: {ex.StackTrace}");
                await ShowErrorAsync($"Ошибка при подсчете размеров: {ex.Message}");
            }
        }

        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateTotalSize();
                Log("Total size updated after checkbox change.");
            }
            catch (Exception ex)
            {
                Log($"Error in CheckBox_Changed: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private void UpdateTotalSize()
        {
            long totalSize = 0;

            if (TempFilesCheckBox.IsChecked == true)
                totalSize += _tempFilesSize;
            if (RecycleBinCheckBox.IsChecked == true)
                totalSize += _recycleBinSize;
            if (BrowserCacheCheckBox.IsChecked == true)
                totalSize += _browserCacheSize;

            TotalSizeText.Text = $"Всего: {FormatSize(totalSize)}";
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

                // Очистка временных файлов
                if (TempFilesCheckBox.IsChecked == true)
                {
                    long freedSpace = await CleanTempFilesAsync(token);
                    totalFreedSpace += freedSpace;
                    Log($"Freed {freedSpace} bytes from %TEMP%.");
                    _tempFilesSize = 0;
                    DispatcherQueue.TryEnqueue(() => TempFilesSizeText.Text = "0 MB");
                }

                // Очистка корзины
                if (RecycleBinCheckBox.IsChecked == true)
                {
                    long freedSpace = await CleanRecycleBinAsync(token);
                    totalFreedSpace += freedSpace;
                    Log($"Freed {freedSpace} bytes from Recycle Bin.");
                    _recycleBinSize = 0;
                    DispatcherQueue.TryEnqueue(() => RecycleBinSizeText.Text = "0 MB");
                }

                // Очистка кэша браузеров
                if (BrowserCacheCheckBox.IsChecked == true)
                {
                    long freedSpace = await CleanBrowserCacheAsync(token);
                    totalFreedSpace += freedSpace;
                    Log($"Freed {freedSpace} bytes from browser cache.");
                    _browserCacheSize = 0;
                    DispatcherQueue.TryEnqueue(() => BrowserCacheSizeText.Text = "0 MB");
                }

                DispatcherQueue.TryEnqueue(() => UpdateTotalSize());
                await UpdateStatusAsync($"Освобождено {FormatSize(totalFreedSpace)}.");
            }
            catch (OperationCanceledException)
            {
                Log("Cleaning operation was canceled.");
                await UpdateStatusAsync("Очистка отменена.");
            }
            catch (Exception ex)
            {
                Log($"Error during cleaning: {ex.Message}\nStackTrace: {ex.StackTrace}");
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
                    Log($"Error calculating %TEMP% size: {ex.Message}");
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
                            Log($"Error calculating Recycle Bin size: {error}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error calculating Recycle Bin size: {ex.Message}");
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
                    // Microsoft Edge
                    string edgeCachePath = Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Cache");
                    if (Directory.Exists(edgeCachePath))
                    {
                        var dirInfo = new DirectoryInfo(edgeCachePath);
                        size += dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                            .Take(10000)
                            .Sum(f => f.Length);
                    }

                    // Google Chrome
                    string chromeCachePath = Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Cache");
                    if (Directory.Exists(chromeCachePath))
                    {
                        var dirInfo = new DirectoryInfo(chromeCachePath);
                        size += dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                            .Take(10000)
                            .Sum(f => f.Length);
                    }

                    // Firefox
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
                    Log($"Error calculating browser cache size: {ex.Message}");
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
                                Log($"Error deleting temp file {file}: {ex.Message}");
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
                                Log($"Error deleting temp directory {dir}: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error cleaning %TEMP%: {ex.Message}");
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
                            Log($"Error cleaning Recycle Bin: {error}");
                        }
                        else
                        {
                            freedSpace = initialSize;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error cleaning Recycle Bin: {ex.Message}");
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

                    // Очистка кэша Microsoft Edge
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
                                Log($"Error deleting Edge cache file {file}: {ex.Message}");
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
                                Log($"Error deleting Edge cache directory {dir}: {ex.Message}");
                            }
                        }
                    }

                    // Очистка кэша Google Chrome
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
                                Log($"Error deleting Chrome cache file {file}: {ex.Message}");
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
                                Log($"Error deleting Chrome cache directory {dir}: {ex.Message}");
                            }
                        }
                    }

                    // Очистка кэша Firefox
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
                                        Log($"Error deleting Firefox cache file {file}: {ex.Message}");
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
                                        Log($"Error deleting Firefox cache directory {dir}: {ex.Message}");
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
                                        Log($"Error deleting Firefox cache2 file {file}: {ex.Message}");
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
                                        Log($"Error deleting Firefox cache2 directory {dir}: {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error cleaning browser cache: {ex.Message}");
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

        private async Task ShowErrorAsync(string message)
        {
            await Task.Run(() => DispatcherQueue.TryEnqueue(() =>
            {
                StatusText.Text = message;
                StatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
            }));
        }

        private void Log(string message)
        {
            string logEntry = $"[{DateTime.Now}] {message}";
            Debug.WriteLine(logEntry);
            try
            {
                File.AppendAllText(_logFilePath, logEntry + "\n");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }
    }
}