using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using PWin11_Tweaker_s.Script;
using Microsoft.Windows.ApplicationModel.Resources; // Для работы локализации

namespace PWin11_Tweaker_s
{
    public sealed partial class SystemPage : Microsoft.UI.Xaml.Controls.Page
    {
        //Для локализации
        private readonly ResourceLoader resourceLoader;

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
                System.Diagnostics.Debug.WriteLine($"SystemPage: Ошибка при инициализации: {ex.Message}\nStackTrace: {ex.StackTrace}");
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

                // Проверяем, что элементы управления инициализированы
                if (DisableServicesCheckBox == null || DisableUACCheckBox == null ||
                    DisableClipboardCheckBox == null || SpeedUpWindowsCheckBox == null)
                {
                    System.Diagnostics.Debug.WriteLine("LoadCurrentSettings: Ошибка: Один из CheckBox не инициализирован.");
                    return;
                }

                // Отключение ненужных служб (проверяем одну из служб, например WSearch)
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\WSearch"))
                {
                    if (key != null)
                    {
                        int? startValue = key.GetValue("Start") as int?;
                        System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: WSearch Start = {startValue}");
                        DisableServicesCheckBox.IsChecked = startValue == 4; // 4 = отключено
                    }
                }

                // UAC
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    if (key != null)
                    {
                        int? enableLUA = key.GetValue("EnableLUA") as int?;
                        System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: EnableLUA = {enableLUA}");
                        DisableUACCheckBox.IsChecked = enableLUA == 0;
                    }
                }

                // История буфера обмена
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Clipboard"))
                {
                    if (key != null)
                    {
                        int? clipboardHistory = key.GetValue("EnableClipboardHistory") as int?;
                        System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: EnableClipboardHistory = {clipboardHistory}");
                        DisableClipboardCheckBox.IsChecked = clipboardHistory == 0;
                    }
                }

                // Ускорение Windows (проверяем StartupDelayInMSec)
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
                System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: Ошибка: {ex.Message}\nStackTrace: {ex.StackTrace}");
                ShowError($"Ошибка загрузки настроек: {ex.Message}");
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Отключаем кнопки на время применения изменений
                ApplyButton.IsEnabled = false;
                DisableServicesCheckBox.IsEnabled = false;
                DisableUACCheckBox.IsEnabled = false;
                DisableClipboardCheckBox.IsEnabled = false;
                SpeedUpWindowsCheckBox.IsEnabled = false;

                // Показываем статус
                StatusText.Text = resourceLoader.GetString("Preparation");
                StatusText.Visibility = Visibility.Visible;

                // Отключение ненужных служб
                bool disableServices = DisableServicesCheckBox.IsChecked ?? false;
                if (disableServices)
                {
                    string[] services = { "WSearch", "Fax", "Spooler", "RemoteRegistry", "WaaSMedicSvc" };
                    foreach (var service in services)
                    {
                        try
                        {
                            // Устанавливаем значение в реестре (4 = отключено)
                            Registry.SetValue(
                                $@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\{service}",
                                "Start",
                                4,
                                RegistryValueKind.DWord
                            );
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Ошибка при отключении службы {service}: {ex.Message}");
                        }
                    }
                    TweakStatus.IsServicesDisabled = true;
                }
                else
                {
                    // Включаем службы обратно (2 = автоматический запуск)
                    string[] services = { "WSearch", "Fax", "Spooler", "RemoteRegistry", "WaaSMedicSvc" };
                    foreach (var service in services)
                    {
                        try
                        {
                            Registry.SetValue(
                                $@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\{service}",
                                "Start",
                                2,
                                RegistryValueKind.DWord
                            );
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Ошибка при включении службы {service}: {ex.Message}");
                        }
                    }
                    TweakStatus.IsServicesDisabled = false;
                }

                // Отключение UAC
                bool disableUAC = DisableUACCheckBox.IsChecked ?? false;
                try
                {
                    Registry.SetValue(
                        @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
                        "EnableLUA",
                        disableUAC ? 0 : 1,
                        RegistryValueKind.DWord
                    );
                    TweakStatus.IsUACDisabled = disableUAC;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Ошибка при изменении UAC: {ex.Message}");
                }

                // Отключение истории буфера обмена
                bool disableClipboard = DisableClipboardCheckBox.IsChecked ?? false;
                try
                {
                    Registry.SetValue(
                        @"HKEY_CURRENT_USER\Software\Microsoft\Clipboard",
                        "EnableClipboardHistory",
                        disableClipboard ? 0 : 1,
                        RegistryValueKind.DWord
                    );
                    TweakStatus.IsClipboardHistoryDisabled = disableClipboard;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Ошибка при изменении истории буфера обмена: {ex.Message}");
                }

                // Ускорение Windows
                bool speedUpWindows = SpeedUpWindowsCheckBox.IsChecked ?? false;
                if (speedUpWindows)
                {
                    try
                    {
                        Registry.SetValue(
                            @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects",
                            "VisualFXSetting",
                            2,
                            RegistryValueKind.DWord
                        );
                        Registry.SetValue(
                            @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize",
                            "StartupDelayInMSec",
                            0,
                            RegistryValueKind.DWord
                        );
                        TweakStatus.IsWindowsSpeedUpApplied = true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Ошибка при ускорении Windows: {ex.Message}");
                    }
                }
                else
                {
                    try
                    {
                        Registry.SetValue(
                            @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects",
                            "VisualFXSetting",
                            1,
                            RegistryValueKind.DWord
                        );
                        Registry.SetValue(
                            @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize",
                            "StartupDelayInMSec",
                            200,
                            RegistryValueKind.DWord
                        );
                        TweakStatus.IsWindowsSpeedUpApplied = false;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Ошибка при возврате настроек производительности: {ex.Message}");
                    }
                }

                // Перезапускаем проводник для применения некоторых изменений
                try
                {
                    Process.Start("taskkill", "/f /im explorer.exe").WaitForExit();
                    Process.Start("explorer.exe");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Ошибка при перезапуске проводника: {ex.Message}");
                }

                // Показываем статус "Готово!"
                StatusText.Text = resourceLoader.GetString("Success");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Ошибка: {ex.Message}\nStackTrace: {ex.StackTrace}");
                StatusText.Text = $"Ошибка: {ex.Message}";
            }
            finally
            {
                // Включаем кнопки обратно
                ApplyButton.IsEnabled = true;
                DisableServicesCheckBox.IsEnabled = true;
                DisableUACCheckBox.IsEnabled = true;
                DisableClipboardCheckBox.IsEnabled = true;
                SpeedUpWindowsCheckBox.IsEnabled = true;
            }
        }

        private void ShowError(string message)
        {
            StatusText.Text = message;
            StatusText.Visibility = Visibility.Visible;
        }
    }
}