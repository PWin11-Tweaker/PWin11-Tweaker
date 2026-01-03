using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using Microsoft.Windows.ApplicationModel.Resources;
using PWin11_Tweaker_s.Script;
using PWin11_Tweaker_s.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PWin11_Tweaker_s
{
    public sealed partial class PerformancePage : Microsoft.UI.Xaml.Controls.Page
    {
        private readonly ResourceLoader resourceLoader;
        private const string ProcessMonitorPath = @"Assets\ProcMon\ProcessMonitorPortable.exe";
        private CancellationTokenSource? _cts;

        public PerformancePage()
        {
            try
            {
                Debug.WriteLine("PerformancePage: Начало инициализации...");
                this.InitializeComponent();
                Debug.WriteLine("PerformancePage: InitializeComponent завершён.");
                resourceLoader = new ResourceLoader();
                LoadCurrentSettings();
                UpdateProcessMonitorButton();
                Debug.WriteLine("PerformancePage: LoadCurrentSettings и UpdateProcessMonitorButton завершены.");
                Debug.WriteLine("PerformancePage успешно инициализирован.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PerformancePage: Ошибка при инициализации: {ex.Message} StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private void LoadCurrentSettings()
        {
            try
            {
                Debug.WriteLine("LoadCurrentSettings: Начало загрузки настроек...");

                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search"))
                {
                    int? allowIndexing = key?.GetValue("AllowIndexingEncryptedStores") as int?;
                    using (var serviceKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\WSearch"))
                    {
                        int? startValue = serviceKey?.GetValue("Start") as int?;
                        DisableSearchIndexingToggle.IsChecked = startValue == 4 || allowIndexing == 0;
                    }
                }

                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects"))
                {
                    int? visualEffects = key != null && key.GetValue("VisualFXSetting") is int value ? value : null;
                    DisableVisualEffectsToggle.IsChecked = visualEffects == 2;
                }
                DisableVisualEffectsToggle.IsChecked = TweakStatus.IsVisualEffectsDisabled;

                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\WSearch"))
                {
                    int? startValue = key?.GetValue("Start") as int?;
                    DisableWindowsSearchToggle.IsChecked = startValue == 4;
                }
                DisableWindowsSearchToggle.IsChecked = TweakStatus.IsWindowsSearchDisabled;

                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\SysMain"))
                {
                    int? startValue = key?.GetValue("Start") as int?;
                    DisableSysMainToggle.IsChecked = startValue == 4;
                }
                DisableSysMainToggle.IsChecked = TweakStatus.IsSysMainDisabled;

                // План электропитания
                var psi = new ProcessStartInfo
                {
                    FileName = "powercfg",
                    Arguments = "/getactivescheme",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var cts = new CancellationTokenSource(5000);
                using (var process = Process.Start(psi))
                {
                    if (process == null)
                    {
                        Debug.WriteLine("Не удалось запустить процесс powercfg.");
                        return;
                    }

                    string output = string.Empty;
                    try
                    {
                        output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit(5000);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"LoadCurrentSettings: Ошибка при чтении вывода powercfg: {ex.Message}");
                    }

                    if (output.Contains("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"))
                        PowerPlanCombo.SelectedIndex = 0; // High Performance
                    else if (output.Contains("381b4222-f694-41f0-9685-ff5bb260df2e"))
                        PowerPlanCombo.SelectedIndex = 1; // Balanced
                    else if (output.Contains("a1841308-3541-4fab-bc81-f71556f20b4a"))
                        PowerPlanCombo.SelectedIndex = 2; // Power Saver
                    else
                        PowerPlanCombo.SelectedIndex = 1; // Balanced по умолчанию
                }

                if (TweakStatus.CurrentPowerPlan == "HighPerformance") PowerPlanCombo.SelectedIndex = 0;
                else if (TweakStatus.CurrentPowerPlan == "Balanced") PowerPlanCombo.SelectedIndex = 1;
                else if (TweakStatus.CurrentPowerPlan == "PowerSaver") PowerPlanCombo.SelectedIndex = 2;

                Debug.WriteLine("LoadCurrentSettings: Текущие настройки успешно загружены.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadCurrentSettings: Ошибка: {ex.Message} StackTrace: {ex.StackTrace}");
            }
        }

        private void PowerPlanCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Оставим для кнопки "Применить"
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            var dispatcher = this.DispatcherQueue;
            long lastUiUpdate = 0;
            void UpdateUI(Action a)
            {
                var now = Environment.TickCount;
                if (now - lastUiUpdate > 100)
                {
                    lastUiUpdate = now;
                    AsyncHelpers.RunOnUI(dispatcher, a);
                }
            }

            try
            {
                UpdateUI(() =>
                {
                    ProgressPanel.Visibility = Visibility.Visible;
                    ApplyButton.IsEnabled = false;
                    StatusText.Text = resourceLoader.GetString("Preparation");
                    ProgressBar.Value = 0;
                });

                await Task.Delay(100, ct);

                bool disableIndexing = DisableSearchIndexingToggle.IsChecked ?? false;
                bool disableEffects = DisableVisualEffectsToggle.IsChecked ?? false;
                bool disableSearch = DisableWindowsSearchToggle.IsChecked ?? false;
                bool disableSysMain = DisableSysMainToggle.IsChecked ?? false;

                // Выполняем записи в реестре параллельно, но с ограничением
                var tasks = new System.Collections.Generic.List<Task>();

                // Windows Search policy
                tasks.Add(AsyncHelpers.SetRegistryValueAsync(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowIndexingEncryptedStores", disableIndexing ? 0 : 1, RegistryValueKind.DWord, ct));

                // WSearch service
                tasks.Add(AsyncHelpers.SetRegistryValueAsync(RegistryHive.LocalMachine,
                    @"SYSTEM\CurrentControlSet\Services\WSearch", "Start", disableIndexing ? 4 : 2, RegistryValueKind.DWord, ct));

                // Visual effects
                tasks.Add(AsyncHelpers.SetRegistryValueAsync(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", disableEffects ? 2 : 1, RegistryValueKind.DWord, ct));

                // UserPreferencesMask only when disabling effects
                if (disableEffects)
                {
                    tasks.Add(AsyncHelpers.SetRegistryValueAsync(RegistryHive.CurrentUser,
                        @"Control Panel\Desktop", "UserPreferencesMask", new byte[] { 0x90, 0x12, 0x03, 0x80, 0x10, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary, ct));
                }

                // WSearch service start (again for general toggle)
                tasks.Add(AsyncHelpers.SetRegistryValueAsync(RegistryHive.LocalMachine,
                    @"SYSTEM\CurrentControlSet\Services\WSearch", "Start", disableSearch ? 4 : 3, RegistryValueKind.DWord, ct));

                // SysMain
                tasks.Add(AsyncHelpers.SetRegistryValueAsync(RegistryHive.LocalMachine,
                    @"SYSTEM\CurrentControlSet\Services\SysMain", "Start", disableSysMain ? 4 : 3, RegistryValueKind.DWord, ct));

                UpdateUI(() => ProgressBar.Value = 30);

                await Task.WhenAll(tasks).ConfigureAwait(false);

                UpdateUI(() => ProgressBar.Value = 60);

                // Stop services if requested (serially, short timeouts)
                if (disableIndexing || disableSearch || disableSysMain)
                {
                    try
                    {
                        var stopTasks = new System.Collections.Generic.List<Task<int>>();
                        if (disableIndexing || disableSearch)
                        {
                            var stopInfo = new ProcessStartInfo { FileName = "sc", Arguments = "stop WSearch", UseShellExecute = true, CreateNoWindow = true };
                            stopTasks.Add(AsyncHelpers.RunProcessAsync(stopInfo, 5000, ct));
                        }

                        if (disableSysMain)
                        {
                            var stopInfo2 = new ProcessStartInfo { FileName = "sc", Arguments = "stop SysMain", UseShellExecute = true, CreateNoWindow = true };
                            stopTasks.Add(AsyncHelpers.RunProcessAsync(stopInfo2, 5000, ct));
                        }

                        await Task.WhenAll(stopTasks).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"ApplyButton_Click: Ошибка при остановке служб: {ex.Message}");
                    }
                }

                UpdateUI(() => ProgressBar.Value = 80);

                // Power plan
                string powerPlanGuid = PowerPlanCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag ? tag switch
                {
                    "HighPerformance" => "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c",
                    "Balanced" => "381b4222-f694-41f0-9685-ff5bb260df2e",
                    "PowerSaver" => "a1841308-3541-4fab-bc81-f71556f20b4a",
                    _ => "381b4222-f694-41f0-9685-ff5bb260df2e"
                } : "381b4222-f694-41f0-9685-ff5bb260df2e";

                var pinfo = new ProcessStartInfo { FileName = "powercfg", Arguments = $"/setactive {powerPlanGuid}", UseShellExecute = true, CreateNoWindow = true };
                try
                {
                    await AsyncHelpers.RunProcessAsync(pinfo, 5000, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ApplyButton_Click: Ошибка при установке плана электропитания: {ex.Message}");
                }

                // Сохранение состояния после применения
                TweakStatus.IsVisualEffectsDisabled = disableEffects;
                TweakStatus.IsWindowsSearchDisabled = disableSearch;
                TweakStatus.IsSysMainDisabled = disableSysMain;
                TweakStatus.CurrentPowerPlan = PowerPlanCombo.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is string selectedTag ? selectedTag : "Balanced";
                TweakStatus.SaveSettings();

                UpdateUI(() => ProgressBar.Value = 100);
                UpdateUI(() => StatusText.Text = resourceLoader.GetString("Success"));
                await Task.Delay(500, ct);

                UpdateUI(async () =>
                {
                    var dialog = new ContentDialog
                    {
                        Title = resourceLoader.GetString("Success"),
                        Content = resourceLoader.GetString("Success_Title_Performance"),
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await dialog.ShowAsync();
                });
            }
            catch (OperationCanceledException)
            {
                AsyncHelpers.RunOnUI(this.DispatcherQueue, () => StatusText.Text = resourceLoader.GetString("Dialog_Error_Title"));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApplyButton_Click: Ошибка: {ex.Message} StackTrace: {ex.StackTrace}");
                AsyncHelpers.RunOnUI(this.DispatcherQueue, () =>
                {
                    var dialog = new ContentDialog
                    {
                        Title = resourceLoader.GetString("Dialog_Error_Title"),
                        Content = ex.Message,
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    _ = dialog.ShowAsync();
                });
            }
            finally
            {
                AsyncHelpers.RunOnUI(this.DispatcherQueue, () =>
                {
                    ProgressPanel.Visibility = Visibility.Collapsed;
                    ApplyButton.IsEnabled = true;
                });
            }
        }

        private void UpdateProcessMonitorButton()
        {
            string fullPath = Path.Combine(AppContext.BaseDirectory, ProcessMonitorPath);
            ProcessMonitorButton.Content = File.Exists(fullPath)
                ? resourceLoader.GetString("Open_ProcessMonitor")
                : resourceLoader.GetString("Install_ProcessMonitor");
            Debug.WriteLine($"UpdateProcessMonitorButton: Статус кнопки обновлён, Process Monitor {(File.Exists(fullPath) ? "доступен" : "не доступен")}" );
        }

        private async void ProcessMonitorButton_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            try
            {
                AsyncHelpers.RunOnUI(this.DispatcherQueue, () =>
                {
                    ProgressPanel.Visibility = Visibility.Visible;
                    ProcessMonitorButton.IsEnabled = false;
                    StatusText.Text = resourceLoader.GetString("Preparation");
                    ProgressBar.Value = 0;
                });

                await Task.Delay(100, ct);

                string fullPath = Path.Combine(AppContext.BaseDirectory, ProcessMonitorPath);

                if (File.Exists(fullPath))
                {
                    Debug.WriteLine("ProcessMonitorButton_Click: Начало открытия Process Monitor...");
                    AsyncHelpers.RunOnUI(this.DispatcherQueue, () =>
                    {
                        StatusText.Text = resourceLoader.GetString("Opening_ProcessMonitor");
                        ProgressBar.Value = 50;
                    });

                    var psi = new ProcessStartInfo
                    {
                        FileName = fullPath,
                        Verb = "runas",
                        UseShellExecute = true,
                        CreateNoWindow = false
                    };

                    try
                    {
                        await AsyncHelpers.RunProcessAsync(psi, 10000, ct).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"ProcessMonitorButton_Click: Ошибка при запуске Process Monitor: {ex.Message}");
                    }
                }
                else
                {
                    AsyncHelpers.RunOnUI(this.DispatcherQueue, () =>
                    {
                        StatusText.Text = resourceLoader.GetString("Error_ProcessMonitorNotFound");
                        ProgressBar.Value = 100;
                    });

                    await Task.Delay(500, ct);

                    AsyncHelpers.RunOnUI(this.DispatcherQueue, async () =>
                    {
                        var dialog = new ContentDialog
                        {
                            Title = resourceLoader.GetString("Dialog_Error_Title"),
                            Content = resourceLoader.GetString("Error_ProcessMonitorNotFound_Message"),
                            CloseButtonText = "OK",
                            XamlRoot = this.XamlRoot
                        };
                        await dialog.ShowAsync();
                    });
                }

                AsyncHelpers.RunOnUI(this.DispatcherQueue, () =>
                {
                    StatusText.Text = resourceLoader.GetString("Success");
                    ProgressBar.Value = 100;
                });

                await Task.Delay(500, ct);
            }
            catch (OperationCanceledException)
            {
                AsyncHelpers.RunOnUI(this.DispatcherQueue, () => StatusText.Text = resourceLoader.GetString("Dialog_Error_Title"));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ProcessMonitorButton_Click: Ошибка: {ex.Message} StackTrace: {ex.StackTrace}");
                AsyncHelpers.RunOnUI(this.DispatcherQueue, () =>
                {
                    var dialog = new ContentDialog
                    {
                        Title = resourceLoader.GetString("Dialog_Error_Title"),
                        Content = ex.Message,
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    _ = dialog.ShowAsync();
                });
            }
            finally
            {
                AsyncHelpers.RunOnUI(this.DispatcherQueue, () =>
                {
                    ProgressPanel.Visibility = Visibility.Collapsed;
                    ProcessMonitorButton.IsEnabled = true;
                });
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _cts?.Cancel();
                Debug.WriteLine("CancelButton_Click: Операция отменена пользователем.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CancelButton_Click: Ошибка при попытке отмены: {ex.Message}");
            }
        }
    }
}