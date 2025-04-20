using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Management.Deployment;
using Microsoft.UI.Dispatching;
using PWin11_Tweaker_s.Script;
using Windows.ApplicationModel;

namespace PWin11_Tweaker_s.Bloatware
{
    public sealed partial class BloatwareRemoverPage : Page
    {
        private readonly Dictionary<string, string> _recommendations = new Dictionary<string, string>
        {
            { "Microsoft.XboxApp", "Безопасно удалить, если не играете в Xbox-игры." },
            { "Microsoft.YourPhone", "Безопасно удалить, если не используете синхронизацию телефона." },
            { "Microsoft.Mail", "Безопасно удалить, если используете сторонний почтовый клиент." },
            { "McAfee", "Рекомендуется удалить, используйте Windows Defender." },
            { "HP Support Assistant", "Безопасно удалить, если не используете техподдержку HP." }
        };

        private readonly string _logFilePath = "bloatware.log";
        private CancellationTokenSource _cts;

        public BloatwareRemoverPage()
        {
            this.InitializeComponent();
            Log("BloatwareRemoverPage initialized.");
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Log("BloatwareRemoverPage OnNavigatedTo.");

            if (!IsAdministrator())
            {
                await ShowWarningAsync("Запустите приложение от имени администратора для полной функциональности.");
            }

            await LoadBloatwareAsync();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            _cts?.Cancel();
            _cts?.Dispose();
        }

        private async Task LoadBloatwareAsync()
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                Log("Loading bloatware...");
                await UpdateStatusAsync("Загрузка приложений...");

                var bloatwareItems = new List<BloatwareItem>();

                // Загрузка UWP-приложений
                await LoadUwpAppsAsync(bloatwareItems, token);

                // Загрузка OEM-приложений
                await LoadOemAppsAsync(bloatwareItems, token);

                Log($"Loaded {bloatwareItems.Count} bloatware items.");
                DispatcherQueue.TryEnqueue(() =>
                {
                    BloatwareList.ItemsSource = FilterItems(bloatwareItems);
                    Log($"BloatwareList ItemsSource set: {bloatwareItems.Count} items.");
                });

                await UpdateStatusAsync($"Загружено {bloatwareItems.Count} приложений.");
            }
            catch (OperationCanceledException)
            {
                Log("Loading bloatware was canceled.");
                await UpdateStatusAsync("Загрузка отменена.");
            }
            catch (Exception ex)
            {
                Log($"Error loading bloatware: {ex.Message}");
                await ShowErrorAsync($"Ошибка загрузки: {ex.Message}");
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
            }
        }

        private async Task LoadUwpAppsAsync(List<BloatwareItem> items, CancellationToken token)
        {
            await Task.Run(async () =>
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    var packageManager = new PackageManager();
                    var packages = packageManager.FindPackagesForUser("");

                    foreach (var package in packages)
                    {
                        token.ThrowIfCancellationRequested();

                        string name = package.Id.Name ?? "";
                        string packageFullName = package.Id.FullName ?? "";
                        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(packageFullName))
                            continue;

                        // Пропускаем системные пакеты
                        if (package.IsFramework || package.IsResourcePackage || package.SignatureKind == PackageSignatureKind.System)
                            continue;

                        long size = 0;
                        try
                        {
                            string installLocation = package.InstalledLocation?.Path ?? "";
                            if (!string.IsNullOrEmpty(installLocation) && System.IO.Directory.Exists(installLocation))
                            {
                                var dirInfo = new System.IO.DirectoryInfo(installLocation);
                                size = await Task.Run(() =>
                                    dirInfo.EnumerateFiles("*", System.IO.SearchOption.AllDirectories)
                                        .Take(1000) // Ограничение для предотвращения перегрузки
                                        .Sum(f => f.Length), token);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"Error calculating size for UWP {name}: {ex.Message}");
                        }

                        items.Add(new BloatwareItem
                        {
                            Name = name,
                            Type = "UWP",
                            Size = size,
                            Recommendation = _recommendations.ContainsKey(name) ? _recommendations[name] : "Неизвестно, проверьте назначение.",
                            PackageName = packageFullName
                        });

                        Log($"Loaded UWP app: {name}, Size: {size}, Package: {packageFullName}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error loading UWP apps: {ex.Message}");
                    throw;
                }
            }, token);
        }

        private async Task LoadOemAppsAsync(List<BloatwareItem> items, CancellationToken token)
        {
            await Task.Run(async () =>
            {
                try
                {
                    token.ThrowIfCancellationRequested();

                    // Поиск через реестр
                    using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
                    {
                        if (key == null)
                        {
                            Log("Registry key not found: SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall");
                            return;
                        }

                        foreach (var subKeyName in key.GetSubKeyNames())
                        {
                            token.ThrowIfCancellationRequested();

                            using (var subKey = key.OpenSubKey(subKeyName))
                            {
                                string name = subKey?.GetValue("DisplayName")?.ToString() ?? "";
                                if (string.IsNullOrEmpty(name) || items.Any(i => i.Name == name))
                                    continue;

                                string identifyingNumber = subKeyName;
                                long size = 0;
                                try
                                {
                                    string estimatedSize = subKey?.GetValue("EstimatedSize")?.ToString() ?? "0";
                                    size = Convert.ToInt64(estimatedSize) * 1024; // KB to bytes
                                }
                                catch (Exception ex)
                                {
                                    Log($"Error parsing size for OEM (registry) {name}: {ex.Message}");
                                }

                                items.Add(new BloatwareItem
                                {
                                    Name = name,
                                    Type = "OEM",
                                    Size = size,
                                    Recommendation = _recommendations.ContainsKey(name) ? _recommendations[name] : "Неизвестно, проверьте назначение.",
                                    PackageName = identifyingNumber
                                });

                                Log($"Loaded OEM app (registry): {name}, Size: {size}, ID: {identifyingNumber}");
                            }
                        }
                    }

                    // Дополнительно проверим 32-битные приложения на 64-битной системе
                    using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"))
                    {
                        if (key == null)
                            return;

                        foreach (var subKeyName in key.GetSubKeyNames())
                        {
                            token.ThrowIfCancellationRequested();

                            using (var subKey = key.OpenSubKey(subKeyName))
                            {
                                string name = subKey?.GetValue("DisplayName")?.ToString() ?? "";
                                if (string.IsNullOrEmpty(name) || items.Any(i => i.Name == name))
                                    continue;

                                string identifyingNumber = subKeyName;
                                long size = 0;
                                try
                                {
                                    string estimatedSize = subKey?.GetValue("EstimatedSize")?.ToString() ?? "0";
                                    size = Convert.ToInt64(estimatedSize) * 1024; // KB to bytes
                                }
                                catch (Exception ex)
                                {
                                    Log($"Error parsing size for OEM (WOW6432Node) {name}: {ex.Message}");
                                }

                                items.Add(new BloatwareItem
                                {
                                    Name = name,
                                    Type = "OEM",
                                    Size = size,
                                    Recommendation = _recommendations.ContainsKey(name) ? _recommendations[name] : "Неизвестно, проверьте назначение.",
                                    PackageName = identifyingNumber
                                });

                                Log($"Loaded OEM app (WOW6432Node): {name}, Size: {size}, ID: {identifyingNumber}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error loading OEM apps via registry: {ex.Message}");
                    throw;
                }
            }, token);
        }

        private IEnumerable<BloatwareItem> FilterItems(List<BloatwareItem> items)
        {
            string selectedFilter = (FilterComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Все приложения";
            switch (selectedFilter)
            {
                case "UWP приложения":
                    return items.Where(i => i.Type == "UWP");
                case "OEM приложения":
                    return items.Where(i => i.Type == "OEM");
                default:
                    return items;
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadBloatwareAsync();
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (BloatwareList.ItemsSource is IEnumerable<BloatwareItem> items)
            {
                foreach (var item in items)
                {
                    item.IsSelected = true;
                }
            }
        }

        private void DeselectAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (BloatwareList.ItemsSource is IEnumerable<BloatwareItem> items)
            {
                foreach (var item in items)
                {
                    item.IsSelected = false;
                }
            }
        }

        private async void RemoveSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                if (BloatwareList.ItemsSource is not IEnumerable<BloatwareItem> items)
                {
                    Log("No items available for removal.");
                    await UpdateStatusAsync("Список приложений пуст.");
                    return;
                }

                var selectedItems = items.Where(i => i.IsSelected).ToList();
                if (selectedItems.Count == 0)
                {
                    Log("No items selected for removal.");
                    await UpdateStatusAsync("Выберите приложения для удаления.");
                    return;
                }

                // Подтверждение удаления
                if (!await ConfirmRemovalAsync(selectedItems.Count))
                    return;

                DispatcherQueue.TryEnqueue(() =>
                {
                    ProgressBar.Visibility = Visibility.Visible;
                    ProgressBar.IsIndeterminate = true;
                });

                int removedCount = 0;
                foreach (var item in selectedItems)
                {
                    token.ThrowIfCancellationRequested();

                    Log($"Removing {item.Type} app: {item.Name}");
                    if (item.Type == "UWP")
                    {
                        if (await RemoveUwpAppAsync(item.PackageName))
                            removedCount++;
                    }
                    else if (item.Type == "OEM")
                    {
                        if (await RemoveOemAppAsync(item.PackageName, item.Name))
                            removedCount++;
                    }
                }

                await LoadBloatwareAsync();
                await UpdateStatusAsync($"Удалено {removedCount} приложений.");
            }
            catch (OperationCanceledException)
            {
                Log("Removal operation was canceled.");
                await UpdateStatusAsync("Операция удаления отменена.");
            }
            catch (Exception ex)
            {
                Log($"Error during removal: {ex.Message}");
                await ShowErrorAsync($"Ошибка удаления: {ex.Message}");
            }
            finally
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    ProgressBar.Visibility = Visibility.Collapsed;
                });
                _cts?.Dispose();
                _cts = null;
            }
        }

        private async Task<bool> RemoveUwpAppAsync(string packageName)
        {
            if (string.IsNullOrEmpty(packageName))
            {
                Log("UWP package name is empty.");
                return false;
            }

            try
            {
                var packageManager = new PackageManager();
                var result = await packageManager.RemovePackageAsync(packageName, RemovalOptions.None);

                bool isRemoved = !result.IsRegistered;
                string errorText = result.ErrorText ?? "";
                string errorCode = result.ExtendedErrorCode != null ? result.ExtendedErrorCode.HResult.ToString("X") : "None";

                if (isRemoved)
                {
                    Log($"Successfully removed UWP app: {packageName}");
                }
                else
                {
                    Log($"Failed to remove UWP app: {packageName}. Error: {errorText}, Code: {errorCode}");
                }

                return isRemoved;
            }
            catch (Exception ex)
            {
                Log($"Error removing UWP app {packageName}: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> RemoveOemAppAsync(string identifyingNumber, string appName)
        {
            if (string.IsNullOrEmpty(identifyingNumber))
            {
                Log("OEM identifying number is empty.");
                return false;
            }

            bool success = false;
            try
            {
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "msiexec.exe",
                        Arguments = $"/x {identifyingNumber} /qn",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                await Task.Run(() =>
                {
                    process.Start();
                    process.WaitForExit();
                    success = process.ExitCode == 0;
                    if (success)
                    {
                        Log($"Removed OEM app: {identifyingNumber}");
                    }
                    else
                    {
                        Log($"Failed to remove OEM app: {identifyingNumber}. Exit code: {process.ExitCode}");
                    }
                });

                // Очистка остаточных файлов
                if (success)
                {
                    string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                    string appFolder = System.IO.Path.Combine(programFiles, appName);
                    if (System.IO.Directory.Exists(appFolder))
                    {
                        System.IO.Directory.Delete(appFolder, true);
                        Log($"Deleted residual folder: {appFolder}");
                    }

                    string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                    appFolder = System.IO.Path.Combine(programFilesX86, appName);
                    if (System.IO.Directory.Exists(appFolder))
                    {
                        System.IO.Directory.Delete(appFolder, true);
                        Log($"Deleted residual folder (x86): {appFolder}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error removing OEM app {identifyingNumber}: {ex.Message}");
            }
            return success;
        }

        private async void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                if (!IsAdministrator())
                {
                    await ShowWarningAsync("Для восстановления UWP-приложений запустите приложение от имени администратора.");
                    return;
                }

                DispatcherQueue.TryEnqueue(() =>
                {
                    ProgressBar.Visibility = Visibility.Visible;
                    ProgressBar.IsIndeterminate = true;
                });

                int restoredCount = 0;
                await Task.Run(async () =>
                {
                    // Шаг 1: Выполняем восстановление UWP-приложений
                    using (var psRestore = new Process())
                    {
                        psRestore.StartInfo.FileName = "powershell.exe";
                        psRestore.StartInfo.Arguments = "Get-AppxPackage -AllUsers | ForEach-Object { Add-AppxPackage -Register -DisableDevelopmentMode -Path $_.InstallLocation }";
                        psRestore.StartInfo.UseShellExecute = false;
                        psRestore.StartInfo.CreateNoWindow = true;
                        psRestore.StartInfo.RedirectStandardOutput = true;
                        psRestore.StartInfo.RedirectStandardError = true;

                        psRestore.Start();
                        await psRestore.WaitForExitAsync(token);

                        if (psRestore.ExitCode != 0)
                        {
                            string error = await psRestore.StandardError.ReadToEndAsync();
                            Log($"Error restoring UWP apps: {error}");
                            throw new Exception(error);
                        }
                    }

                    // Шаг 2: Получаем количество восстановленных приложений
                    using (var psCount = new Process())
                    {
                        psCount.StartInfo.FileName = "powershell.exe";
                        psCount.StartInfo.Arguments = "Get-AppxPackage -AllUsers | Measure-Object | Select-Object -ExpandProperty Count";
                        psCount.StartInfo.UseShellExecute = false;
                        psCount.StartInfo.CreateNoWindow = true;
                        psCount.StartInfo.RedirectStandardOutput = true;
                        psCount.StartInfo.RedirectStandardError = true;

                        psCount.Start();
                        string output = await psCount.StandardOutput.ReadToEndAsync();
                        await psCount.WaitForExitAsync(token);

                        if (psCount.ExitCode == 0)
                        {
                            if (int.TryParse(output.Trim(), out int count))
                            {
                                restoredCount = count;
                                Log($"Restored {restoredCount} UWP apps.");
                            }
                            else
                            {
                                Log($"Failed to parse UWP app count: {output}");
                                throw new Exception("Не удалось определить количество восстановленных приложений.");
                            }
                        }
                        else
                        {
                            string error = await psCount.StandardError.ReadToEndAsync();
                            Log($"Error counting UWP apps: {error}");
                            throw new Exception(error);
                        }
                    }
                }, token);

                await LoadBloatwareAsync();
                await UpdateStatusAsync($"Восстановлено {restoredCount} UWP-приложений.");
            }
            catch (OperationCanceledException)
            {
                Log("Restore operation was canceled.");
                await UpdateStatusAsync("Операция восстановления отменена.");
            }
            catch (Exception ex)
            {
                Log($"Error restoring UWP apps: {ex.Message}");
                await ShowErrorAsync($"Ошибка восстановления: {ex.Message}");
            }
            finally
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    ProgressBar.Visibility = Visibility.Collapsed;
                });
                _cts?.Dispose();
                _cts = null;
            }
        }

        private async Task<bool> ConfirmRemovalAsync(int count)
        {
            var dialog = new ContentDialog
            {
                Title = "Подтверждение удаления",
                Content = $"Вы уверены, что хотите удалить {count} приложений?",
                PrimaryButtonText = "Удалить",
                CloseButtonText = "Отмена",
                XamlRoot = this.XamlRoot
            };
            return await dialog.ShowAsync() == ContentDialogResult.Primary;
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

        private async Task ShowWarningAsync(string message)
        {
            await Task.Run(() => DispatcherQueue.TryEnqueue(() =>
            {
                StatusText.Text = message;
                StatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Orange);
            }));
        }

        private void Log(string message)
        {
            string logEntry = $"[{DateTime.Now}] {message}";
            Debug.WriteLine(logEntry);
            try
            {
                System.IO.File.AppendAllText(_logFilePath, logEntry + "\n");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }

        private bool IsAdministrator()
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
    }

    public class SizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is long size)
            {
                return $"{(size / 1024.0 / 1024.0):F2} MB";
            }
            return "0 MB";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}