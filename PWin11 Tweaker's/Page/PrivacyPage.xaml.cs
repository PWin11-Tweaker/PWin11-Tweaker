using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Windows.ApplicationModel.Resources;
using PWin11_Tweaker_s.Script;

namespace PWin11_Tweaker_s
{
    public sealed partial class PrivacyPage : Microsoft.UI.Xaml.Controls.Page
    {
        private readonly ResourceLoader resourceLoader;

        public PrivacyPage()
        {
            this.InitializeComponent();
            resourceLoader = new ResourceLoader();
            Debug.WriteLine("PrivacyPage: Инициализация страницы завершена.");
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            try
            {
                Debug.WriteLine("LoadCurrentSettings: Загрузка текущих настроек начата.");

                // Телеметрия (Исправлено)
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection", false))
                {
                    int? telemetry = key?.GetValue("AllowTelemetry") as int? ?? 3; // 3 - значение по умолчанию, если ключа нет
                    DisableTelemetryToggle.IsChecked = telemetry == 0;
                    Debug.WriteLine($"LoadCurrentSettings: Телеметрия - AllowTelemetry = {telemetry}, Toggle = {DisableTelemetryToggle.IsChecked}");
                }

                // Рекламный ID (Работает)
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo"))
                {
                    int? adId = key?.GetValue("Enabled") as int? ?? 1; // 1 - значение по умолчанию, если ключа нет
                    DisableAdvertisingIdToggle.IsChecked = adId == 0;
                    Debug.WriteLine($"LoadCurrentSettings: Рекламный ID - Enabled = {adId}, Toggle = {DisableAdvertisingIdToggle.IsChecked}");
                }

                // Местоположение (Исправлено)
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", false))
                {
                    int? location = key?.GetValue("DisableLocation") as int? ?? 0; // 0 - значение по умолчанию, если ключа нет
                    DisableLocationToggle.IsChecked = location == 1;
                    Debug.WriteLine($"LoadCurrentSettings: Местоположение - DisableLocation = {location}, Toggle = {DisableLocationToggle.IsChecked}");
                }

                // Cortana (Исправлено)
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search", false))
                {
                    int? cortana = key?.GetValue("AllowCortana") as int? ?? 1; // 1 - значение по умолчанию, если ключа нет
                    DisableCortanaToggle.IsChecked = cortana == 0;
                    Debug.WriteLine($"LoadCurrentSettings: Cortana - AllowCortana = {cortana}, Toggle = {DisableCortanaToggle.IsChecked}");
                }

                // Фоновые приложения (Работает)
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications"))
                {
                    int? backgroundApps = key?.GetValue("GlobalUserDisabled") as int? ?? 0; // 0 - значение по умолчанию, если ключа нет
                    DisableBackgroundAppsToggle.IsChecked = backgroundApps == 1;
                    Debug.WriteLine($"LoadCurrentSettings: Фоновые приложения - GlobalUserDisabled = {backgroundApps}, Toggle = {DisableBackgroundAppsToggle.IsChecked}");
                }

                // Облачный контент (Работает)
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\CloudExperienceHost"))
                {
                    int? cloudContent = key?.GetValue("DisableCloudOptimizedContent") as int? ?? 0; // 0 - значение по умолчанию, если ключа нет
                    DisableCloudContentToggle.IsChecked = cloudContent == 1;
                    Debug.WriteLine($"LoadCurrentSettings: Облачный контент - DisableCloudOptimizedContent = {cloudContent}, Toggle = {DisableCloudContentToggle.IsChecked}");
                }

                // Find My Device (Исправлено)
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\FindMyDevice", false))
                {
                    int? findMyDevice = key?.GetValue("AllowFindMyDevice") as int? ?? 1; // 1 - значение по умолчанию, если ключа нет
                    DisableFindMyDeviceToggle.IsChecked = findMyDevice == 0;
                    Debug.WriteLine($"LoadCurrentSettings: Find My Device - AllowFindMyDevice = {findMyDevice}, Toggle = {DisableFindMyDeviceToggle.IsChecked}");
                }

                // Windows Insider Program телеметрия (Исправлено)
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\PreviewBuilds", false))
                {
                    int? insiderTelemetry = key?.GetValue("AllowBuildPreview") as int? ?? 1; // 1 - значение по умолчанию, если ключа нет
                    DisableInsiderTelemetryToggle.IsChecked = insiderTelemetry == 0;
                    Debug.WriteLine($"LoadCurrentSettings: Windows Insider Telemetry - AllowBuildPreview = {insiderTelemetry}, Toggle = {DisableInsiderTelemetryToggle.IsChecked}");
                }

                // Сбор данных Microsoft Edge (Исправлено)
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Edge", false))
                {
                    int? edgeDiagnostics = key?.GetValue("DiagnosticData") as int? ?? 1; // 1 - значение по умолчанию, если ключа нет
                    DisableEdgeDiagnosticsToggle.IsChecked = edgeDiagnostics == 0;
                    Debug.WriteLine($"LoadCurrentSettings: Microsoft Edge Diagnostics - DiagnosticData = {edgeDiagnostics}, Toggle = {DisableEdgeDiagnosticsToggle.IsChecked}");
                }

                // Suggested Content (Работает)
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"))
                {
                    int? suggestedContent = key?.GetValue("SubscribedContent-338393Enabled") as int? ?? 1; // 1 - значение по умолчанию, если ключа нет
                    DisableSuggestedContentToggle.IsChecked = suggestedContent == 0;
                    Debug.WriteLine($"LoadCurrentSettings: Suggested Content - SubscribedContent-338393Enabled = {suggestedContent}, Toggle = {DisableSuggestedContentToggle.IsChecked}");
                }

                Debug.WriteLine("LoadCurrentSettings: Загрузка текущих настроек завершена.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadCurrentSettings: Ошибка: {ex.Message}");
            }
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine("ApplyButton_Click: Начало применения настроек.");
                ProgressPanel.Visibility = Visibility.Visible;
                ApplyButton.IsEnabled = false;
                ResetButton.IsEnabled = false;
                StatusText.Text = resourceLoader.GetString("Preparation");
                ProgressBar.Value = 0;
                await Task.Delay(100);

                string regContent = "Windows Registry Editor Version 5.00\n\n";
                string batContent = "@echo off\n";

                // Телеметрия
                bool disableTelemetry = DisableTelemetryToggle.IsChecked ?? false;
                Debug.WriteLine($"ApplyButton_Click: Телеметрия - Устанавливаем AllowTelemetry = {(disableTelemetry ? 0 : 1)}");
                regContent += @"[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection]" + "\n" +
                              $"\"AllowTelemetry\"=dword:0000000{(disableTelemetry ? 0 : 1)}\n\n";
                if (disableTelemetry)
                {
                    Debug.WriteLine("ApplyButton_Click: Отключаем службу DiagTrack.");
                    regContent += @"[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\DiagTrack]" + "\n" +
                                  "\"Start\"=dword:00000004\n\n";
                    batContent += "sc stop DiagTrack >nul 2>&1\n";
                }

                // Рекламный ID
                bool disableAdId = DisableAdvertisingIdToggle.IsChecked ?? false;
                Debug.WriteLine($"ApplyButton_Click: Рекламный ID - Устанавливаем Enabled = {(disableAdId ? 0 : 1)}");
                regContent += @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo]" + "\n" +
                              $"\"Enabled\"=dword:0000000{(disableAdId ? 0 : 1)}\n\n";

                // Местоположение
                bool disableLocation = DisableLocationToggle.IsChecked ?? false;
                Debug.WriteLine($"ApplyButton_Click: Местоположение - Устанавливаем DisableLocation = {(disableLocation ? 1 : 0)}");
                regContent += @"[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors]" + "\n" +
                              $"\"DisableLocation\"=dword:0000000{(disableLocation ? 1 : 0)}\n" +
                              $"\"DisableLocationForAllUsers\"=dword:0000000{(disableLocation ? 1 : 0)}\n\n";
                if (disableLocation)
                {
                    batContent += "sc stop lfsvc >nul 2>&1\n"; // Остановка службы геолокации
                }

                // Cortana
                bool disableCortana = DisableCortanaToggle.IsChecked ?? false;
                Debug.WriteLine($"ApplyButton_Click: Cortana - Устанавливаем AllowCortana = {(disableCortana ? 0 : 1)}");
                regContent += @"[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Search]" + "\n" +
                              $"\"AllowCortana\"=dword:0000000{(disableCortana ? 0 : 1)}\n\n";
                if (disableCortana)
                {
                    batContent += "sc stop Cortana >nul 2>&1\n"; // Остановка службы Cortana
                }

                // Фоновые приложения
                bool disableBackgroundApps = DisableBackgroundAppsToggle.IsChecked ?? false;
                Debug.WriteLine($"ApplyButton_Click: Фоновые приложения - Устанавливаем GlobalUserDisabled = {(disableBackgroundApps ? 1 : 0)}");
                regContent += @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications]" + "\n" +
                              $"\"GlobalUserDisabled\"=dword:0000000{(disableBackgroundApps ? 1 : 0)}\n\n";

                // Облачный контент
                bool disableCloudContent = DisableCloudContentToggle.IsChecked ?? false;
                Debug.WriteLine($"ApplyButton_Click: Облачный контент - Устанавливаем DisableCloudOptimizedContent = {(disableCloudContent ? 1 : 0)}");
                regContent += @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudExperienceHost]" + "\n" +
                              $"\"DisableCloudOptimizedContent\"=dword:0000000{(disableCloudContent ? 1 : 0)}\n" +
                              @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager]" + "\n" +
                              $"\"SystemPaneSuggestionsEnabled\"=dword:0000000{(disableCloudContent ? 0 : 1)}\n\n";

                // Find My Device
                bool disableFindMyDevice = DisableFindMyDeviceToggle.IsChecked ?? false;
                Debug.WriteLine($"ApplyButton_Click: Find My Device - Устанавливаем AllowFindMyDevice = {(disableFindMyDevice ? 0 : 1)}");
                regContent += @"[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\FindMyDevice]" + "\n" +
                              $"\"AllowFindMyDevice\"=dword:0000000{(disableFindMyDevice ? 0 : 1)}\n\n";
                if (disableFindMyDevice)
                {
                    batContent += "sc stop OneSyncSvc >nul 2>&1\n"; // Остановка службы синхронизации
                }

                // Windows Insider Program телеметрия
                bool disableInsiderTelemetry = DisableInsiderTelemetryToggle.IsChecked ?? false;
                Debug.WriteLine($"ApplyButton_Click: Windows Insider Telemetry - Устанавливаем AllowBuildPreview = {(disableInsiderTelemetry ? 0 : 1)}");
                regContent += @"[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\PreviewBuilds]" + "\n" +
                              $"\"AllowBuildPreview\"=dword:0000000{(disableInsiderTelemetry ? 0 : 1)}\n\n";

                // Сбор данных Microsoft Edge
                bool disableEdgeDiagnostics = DisableEdgeDiagnosticsToggle.IsChecked ?? false;
                Debug.WriteLine($"ApplyButton_Click: Microsoft Edge Diagnostics - Устанавливаем DiagnosticData = {(disableEdgeDiagnostics ? 0 : 1)}");
                regContent += @"[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge]" + "\n" +
                              $"\"DiagnosticData\"=dword:0000000{(disableEdgeDiagnostics ? 0 : 1)}\n\n";

                // Suggested Content
                bool disableSuggestedContent = DisableSuggestedContentToggle.IsChecked ?? false;
                Debug.WriteLine($"ApplyButton_Click: Suggested Content - Устанавливаем SubscribedContent-338393Enabled = {(disableSuggestedContent ? 0 : 1)}");
                regContent += @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager]" + "\n" +
                              $"\"SubscribedContent-338393Enabled\"=dword:0000000{(disableSuggestedContent ? 0 : 1)}\n\n";

                // Применение изменений
                StatusText.Text = resourceLoader.GetString("Apply_Change");
                ProgressBar.Value = 50;
                Debug.WriteLine("ApplyButton_Click: Создание временных файлов для применения изменений.");
                await Task.Delay(100);

                string tempRegPath = Path.Combine(Path.GetTempPath(), "PrivacyTweaks.reg");
                File.WriteAllText(tempRegPath, regContent, Encoding.Unicode);
                Debug.WriteLine($"ApplyButton_Click: Создан REG-файл: {tempRegPath}");

                string tempBatPath = Path.Combine(Path.GetTempPath(), "PrivacyTweaks.bat");
                batContent += $"reg import \"{tempRegPath}\" >nul 2>&1\n" +
                              "if %ERRORLEVEL% NEQ 0 (exit /b %ERRORLEVEL%)\n" +
                              $"del \"{tempRegPath}\" >nul 2>&1\n" +
                              "taskkill /f /im explorer.exe >nul 2>&1\n" +
                              "start explorer.exe\n" +
                              "exit /b 0";
                File.WriteAllText(tempBatPath, batContent);
                Debug.WriteLine($"ApplyButton_Click: Создан BAT-файл: {tempBatPath}");

                StatusText.Text = resourceLoader.GetString("Apply_Change");
                ProgressBar.Value = 75;
                Debug.WriteLine("ApplyButton_Click: Запуск команды для применения изменений.");
                await Task.Delay(100);

                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C \"{tempBatPath}\"",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (Process process = Process.Start(processInfo))
                {
                    process.WaitForExit(5000);
                    if (process.ExitCode != 0)
                    {
                        Debug.WriteLine($"ApplyButton_Click: Ошибка применения настроек, код: {process.ExitCode}");
                        throw new Exception($"Ошибка применения настроек, код: {process.ExitCode}");
                    }
                    Debug.WriteLine($"ApplyButton_Click: Команда выполнена успешно, код выхода: {process.ExitCode}");
                }

                // Сохранение состояния твиков в TweakStatus
                TweakStatus.IsTelemetryDisabled = disableTelemetry;
                TweakStatus.IsAdvertisingIdDisabled = disableAdId;
                TweakStatus.IsLocationTrackingDisabled = disableLocation;
                TweakStatus.IsCortanaDisabled = disableCortana;
                TweakStatus.IsBackgroundAppsDisabled = disableBackgroundApps;
                TweakStatus.IsCloudContentDisabled = disableCloudContent;
                TweakStatus.IsFindMyDeviceDisabled = disableFindMyDevice;
                TweakStatus.IsInsiderTelemetryDisabled = disableInsiderTelemetry;
                TweakStatus.IsEdgeDiagnosticsDisabled = disableEdgeDiagnostics;
                TweakStatus.IsSuggestedContentDisabled = disableSuggestedContent;
                TweakStatus.SaveSettings(); // Сохраняем изменения
                Debug.WriteLine("ApplyButton_Click: Состояние твиков сохранено в TweakStatus.");

                StatusText.Text = resourceLoader.GetString("Success");
                ProgressBar.Value = 100;
                Debug.WriteLine("ApplyButton_Click: Настройки успешно применены.");
                await Task.Delay(500);

                var dialog = new ContentDialog
                {
                    Title = resourceLoader.GetString("Success"),
                    Content = resourceLoader.GetString("Success_Title_Privacy"),
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
                Debug.WriteLine("ApplyButton_Click: Отображено сообщение об успешном применении.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApplyButton_Click: Ошибка: {ex.Message}");
                var dialog = new ContentDialog
                {
                    Title = resourceLoader.GetString("Dialog_Error_Title"),
                    Content = $"{ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
                Debug.WriteLine("ApplyButton_Click: Отображено сообщение об ошибке.");
            }
            finally
            {
                ProgressPanel.Visibility = Visibility.Collapsed;
                ApplyButton.IsEnabled = true;
                ResetButton.IsEnabled = true;
                Debug.WriteLine("ApplyButton_Click: Завершение обработки, восстановление состояния UI.");
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("ResetButton_Click: Сброс всех настроек.");
            DisableTelemetryToggle.IsChecked = false;
            Debug.WriteLine("ResetButton_Click: Телеметрия - Сброшено (unchecked).");
            DisableAdvertisingIdToggle.IsChecked = false;
            Debug.WriteLine("ResetButton_Click: Рекламный ID - Сброшено (unchecked).");
            DisableLocationToggle.IsChecked = false;
            Debug.WriteLine("ResetButton_Click: Местоположение - Сброшено (unchecked).");
            DisableCortanaToggle.IsChecked = false;
            Debug.WriteLine("ResetButton_Click: Cortana - Сброшено (unchecked).");
            DisableBackgroundAppsToggle.IsChecked = false;
            Debug.WriteLine("ResetButton_Click: Фоновые приложения - Сброшено (unchecked).");
            DisableCloudContentToggle.IsChecked = false;
            Debug.WriteLine("ResetButton_Click: Облачный контент - Сброшено (unchecked).");
            DisableFindMyDeviceToggle.IsChecked = false;
            Debug.WriteLine("ResetButton_Click: Find My Device - Сброшено (unchecked).");
            DisableInsiderTelemetryToggle.IsChecked = false;
            Debug.WriteLine("ResetButton_Click: Windows Insider Telemetry - Сброшено (unchecked).");
            DisableEdgeDiagnosticsToggle.IsChecked = false;
            Debug.WriteLine("ResetButton_Click: Microsoft Edge Diagnostics - Сброшено (unchecked).");
            DisableSuggestedContentToggle.IsChecked = false;
            Debug.WriteLine("ResetButton_Click: Suggested Content - Сброшено (unchecked).");
            Debug.WriteLine("ResetButton_Click: Сброс настроек завершен.");
        }
    }
}