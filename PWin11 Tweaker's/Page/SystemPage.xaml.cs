using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using PWin11_Tweaker_s.Script;
using Microsoft.Windows.ApplicationModel.Resources; // Для работы локализации
using PWin11_Tweaker_s.Helpers;
using System.Threading;
using System.Threading.Tasks;

namespace PWin11_Tweaker_s
{
    public sealed partial class SystemPage : Microsoft.UI.Xaml.Controls.Page
    {
        //Для локализации
        private readonly ResourceLoader resourceLoader;
        private CancellationTokenSource? _cts;

        public SystemPage()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("SystemPage: Начало инициализации...");
                this.InitializeComponent();
                System.Diagnostics.Debug.WriteLine("SystemPage: InitializeComponent завершён.");
                LoadCurrentSettings();
                System.Diagnostics.Debug.WriteLine("SystemPage: LoadCurrentSettings завершён.");
                System.Diagnostics.Debug.WriteLine("SystemPage: Инициализация завершена успешно.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SystemPage: Ошибка при инициализации: {ex.Message} StackTrace: {ex.StackTrace}");
                ShowError($"Ошибка инициализации: {ex.Message}");
            }
            //Инициализируем наши ресурсы для локализации
            resourceLoader = new ResourceLoader();
        }

        private void LoadCurrentSettings()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("LoadCurrentSettings: Начало загрузки настроек...");

                if (DisableServicesCheckBox == null || DisableUACCheckBox == null ||
                    DisableClipboardCheckBox == null || SpeedUpWindowsCheckBox == null)
                {
                    System.Diagnostics.Debug.WriteLine("LoadCurrentSettings: Ошибка: Один из CheckBox не инициализирован.");
                    return;
                }

                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\WSearch"))
                {
                    if (key != null)
                    {
                        int? startValue = key.GetValue("Start") as int?;
                        System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: WSearch Start = {startValue}");
                        DisableServicesCheckBox.IsChecked = startValue == 4; // 4 = отключено
                    }
                }

                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    if (key != null)
                    {
                        int? enableLUA = key.GetValue("EnableLUA") as int?;
                        System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: EnableLUA = {enableLUA}");
                        DisableUACCheckBox.IsChecked = enableLUA == 0;
                    }
                }

                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Clipboard"))
                {
                    if (key != null)
                    {
                        int? clipboardHistory = key.GetValue("EnableClipboardHistory") as int?;
                        System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: EnableClipboardHistory = {clipboardHistory}");
                        DisableClipboardCheckBox.IsChecked = clipboardHistory == 0;
                    }
                }

                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize"))
                {
                    if (key != null)
                    {
                        int? startupDelay = key.GetValue("StartupDelayInMSec") as int?;
                        System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: StartupDelayInMSec = {startupDelay}");
                        SpeedUpWindowsCheckBox.IsChecked = startupDelay == 0;
                    }
                }

                System.Diagnostics.Debug.WriteLine("LoadCurrentSettings: Настройки успешно загружены.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: Ошибка: {ex.Message} StackTrace: {ex.StackTrace}");
                ShowError($"Ошибка загрузки настроек: {ex.Message}");
            }
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
                    ApplyButton.IsEnabled = false;
                    DisableServicesCheckBox.IsEnabled = false;
                    DisableUACCheckBox.IsEnabled = false;
                    DisableClipboardCheckBox.IsEnabled = false;
                    SpeedUpWindowsCheckBox.IsEnabled = false;

                    StatusText.Text = resourceLoader.GetString("Preparation");
                    StatusText.Visibility = Visibility.Visible;
                });

                await Task.Delay(100, ct);

                bool disableServices = DisableServicesCheckBox.IsChecked ?? false;
                bool disableUAC = DisableUACCheckBox.IsChecked ?? false;
                bool disableClipboard = DisableClipboardCheckBox.IsChecked ?? false;
                bool speedUpWindows = SpeedUpWindowsCheckBox.IsChecked ?? false;

                var tasks = new System.Collections.Generic.List<Task>();

                if (disableServices)
                {
                    string[] services = { "WSearch", "Fax", "Spooler", "RemoteRegistry", "WaaSMedicSvc" };
                    foreach (var service in services)
                    {
                        tasks.Add(AsyncHelpers.SetRegistryValueAsync(RegistryHive.LocalMachine,
                            $"SYSTEM\\CurrentControlSet\\Services\\{service}", "Start", 4, RegistryValueKind.DWord, ct));
                    }
                }
                else
                {
                    string[] services = { "WSearch", "Fax", "Spooler", "RemoteRegistry", "WaaSMedicSvc" };
                    foreach (var service in services)
                    {
                        tasks.Add(AsyncHelpers.SetRegistryValueAsync(RegistryHive.LocalMachine,
                            $"SYSTEM\\CurrentControlSet\\Services\\{service}", "Start", 2, RegistryValueKind.DWord, ct));
                    }
                }

                tasks.Add(AsyncHelpers.SetRegistryValueAsync(RegistryHive.LocalMachine,
                    @"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "EnableLUA", disableUAC ? 0 : 1, RegistryValueKind.DWord, ct));

                tasks.Add(AsyncHelpers.SetRegistryValueAsync(RegistryHive.CurrentUser,
                    @"Software\\Microsoft\\Clipboard", "EnableClipboardHistory", disableClipboard ? 0 : 1, RegistryValueKind.DWord, ct));

                if (speedUpWindows)
                {
                    tasks.Add(AsyncHelpers.SetRegistryValueAsync(RegistryHive.CurrentUser,
                        @"Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects", "VisualFXSetting", 2, RegistryValueKind.DWord, ct));
                    tasks.Add(AsyncHelpers.SetRegistryValueAsync(RegistryHive.CurrentUser,
                        @"Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Serialize", "StartupDelayInMSec", 0, RegistryValueKind.DWord, ct));
                }
                else
                {
                    tasks.Add(AsyncHelpers.SetRegistryValueAsync(RegistryHive.CurrentUser,
                        @"Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects", "VisualFXSetting", 1, RegistryValueKind.DWord, ct));
                    tasks.Add(AsyncHelpers.SetRegistryValueAsync(RegistryHive.CurrentUser,
                        @"Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Serialize", "StartupDelayInMSec", 200, RegistryValueKind.DWord, ct));
                }

                UpdateUI(() => StatusText.Text = resourceLoader.GetString("Apply_Change"));

                await Task.WhenAll(tasks).ConfigureAwait(false);

                // Перезапускаем проводник для применения некоторых изменений
                try
                {
                    await AsyncHelpers.RestartExplorerAsync(5000, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ApplyButton_Click: Ошибка при перезапуске проводника: {ex.Message}");
                }

                // Сохраняем состояния
                TweakStatus.IsServicesDisabled = disableServices;
                TweakStatus.IsUACDisabled = disableUAC;
                TweakStatus.IsClipboardHistoryDisabled = disableClipboard;
                TweakStatus.IsWindowsSpeedUpApplied = speedUpWindows;

                UpdateUI(() => StatusText.Text = resourceLoader.GetString("Success"));
            }
            catch (OperationCanceledException)
            {
                UpdateUI(() => StatusText.Text = resourceLoader.GetString("Dialog_Error_Title"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Ошибка: {ex.Message} StackTrace: {ex.StackTrace}");
                UpdateUI(() => StatusText.Text = $"Ошибка: {ex.Message}");
            }
            finally
            {
                UpdateUI(() =>
                {
                    ApplyButton.IsEnabled = true;
                    DisableServicesCheckBox.IsEnabled = true;
                    DisableUACCheckBox.IsEnabled = true;
                    DisableClipboardCheckBox.IsEnabled = true;
                    SpeedUpWindowsCheckBox.IsEnabled = true;
                });
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _cts?.Cancel();
                Debug.WriteLine("CancelButton_Click: Операция отменена пользователем (SystemPage).");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CancelButton_Click: Ошибка при попытке отмены: {ex.Message}");
            }
        }

        private void ShowError(string message)
        {
            StatusText.Text = message;
            StatusText.Visibility = Visibility.Visible;
        }
    }
}