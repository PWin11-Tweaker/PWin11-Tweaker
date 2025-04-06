using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Windows.ApplicationModel.Resources; // Для работы локализации

namespace PWin11_Tweaker_s
{
    public sealed partial class PrivacyPage : Page
    {
        //Для локализации
        private readonly ResourceLoader resourceLoader;

        public PrivacyPage()
        {
            this.InitializeComponent();
            //Инициализируем наши ресурсы для локализации
            resourceLoader = new ResourceLoader();
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            try
            {
                // Телеметрия
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection"))
                {
                    int? telemetry = key?.GetValue("AllowTelemetry") as int?;
                    DisableTelemetryToggle.IsChecked = telemetry == 0;
                }

                // Рекламный ID
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo"))
                {
                    int? adId = key?.GetValue("Enabled") as int?;
                    DisableAdvertisingIdToggle.IsChecked = adId == 0;
                }

                // Местоположение
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors"))
                {
                    int? location = key?.GetValue("DisableLocation") as int?;
                    DisableLocationToggle.IsChecked = location == 1;
                }

                // Cortana
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search"))
                {
                    int? cortana = key?.GetValue("AllowCortana") as int?;
                    DisableCortanaToggle.IsChecked = cortana == 0;
                }

                // Фоновые приложения
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications"))
                {
                    int? backgroundApps = key?.GetValue("GlobalUserDisabled") as int?;
                    DisableBackgroundAppsToggle.IsChecked = backgroundApps == 1;
                }

                // Облачный контент
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\CloudExperienceHost"))
                {
                    int? cloudContent = key?.GetValue("DisableCloudOptimizedContent") as int?;
                    DisableCloudContentToggle.IsChecked = cloudContent == 1;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: Ошибка: {ex.Message}");
            }
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
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
                regContent += @"[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection]" + "\n" +
                              $"\"AllowTelemetry\"=dword:0000000{(disableTelemetry ? 0 : 1)}\n\n";
                if (disableTelemetry)
                {
                    regContent += @"[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\DiagTrack]" + "\n" +
                                  "\"Start\"=dword:00000004\n\n"; // Отключение службы Diagnostics Tracking
                    batContent += "sc stop DiagTrack >nul 2>&1\n";
                }

                // Рекламный ID
                bool disableAdId = DisableAdvertisingIdToggle.IsChecked ?? false;
                regContent += @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo]" + "\n" +
                              $"\"Enabled\"=dword:0000000{(disableAdId ? 0 : 1)}\n\n";

                // Местоположение
                bool disableLocation = DisableLocationToggle.IsChecked ?? false;
                regContent += @"[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors]" + "\n" +
                              $"\"DisableLocation\"=dword:0000000{(disableLocation ? 1 : 0)}\n" +
                              $"\"DisableLocationForAllUsers\"=dword:0000000{(disableLocation ? 1 : 0)}\n\n";

                // Cortana
                bool disableCortana = DisableCortanaToggle.IsChecked ?? false;
                regContent += @"[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Search]" + "\n" +
                              $"\"AllowCortana\"=dword:0000000{(disableCortana ? 0 : 1)}\n\n";

                // Фоновые приложения
                bool disableBackgroundApps = DisableBackgroundAppsToggle.IsChecked ?? false;
                regContent += @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications]" + "\n" +
                              $"\"GlobalUserDisabled\"=dword:0000000{(disableBackgroundApps ? 1 : 0)}\n\n";

                // Облачный контент
                bool disableCloudContent = DisableCloudContentToggle.IsChecked ?? false;
                regContent += @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CloudExperienceHost]" + "\n" +
                              $"\"DisableCloudOptimizedContent\"=dword:0000000{(disableCloudContent ? 1 : 0)}\n" +
                              @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager]" + "\n" +
                              $"\"SystemPaneSuggestionsEnabled\"=dword:0000000{(disableCloudContent ? 0 : 1)}\n\n";

                // Применение изменений
                StatusText.Text = resourceLoader.GetString("Apply_Change");
                ProgressBar.Value = 50;
                await Task.Delay(100);

                string tempRegPath = Path.Combine(Path.GetTempPath(), "PrivacyTweaks.reg");
                File.WriteAllText(tempRegPath, regContent, Encoding.Unicode);

                string tempBatPath = Path.Combine(Path.GetTempPath(), "PrivacyTweaks.bat");
                batContent += $"reg import \"{tempRegPath}\" >nul 2>&1\n" +
                              "if %ERRORLEVEL% NEQ 0 (exit /b %ERRORLEVEL%)\n" +
                              $"del \"{tempRegPath}\" >nul 2>&1\n" +
                              "taskkill /f /im explorer.exe >nul 2>&1\n" +
                              "start explorer.exe\n" +
                              "exit /b 0";
                File.WriteAllText(tempBatPath, batContent);

                StatusText.Text = resourceLoader.GetString("Apply_Change");
                ProgressBar.Value = 75;
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
                        throw new Exception($"Ошибка применения настроек, код: {process.ExitCode}");
                    }
                }

                StatusText.Text = resourceLoader.GetString("Success");
                ProgressBar.Value = 100;
                await Task.Delay(500);

                var dialog = new ContentDialog
                {
                    Title = resourceLoader.GetString("Success"),
                    Content = resourceLoader.GetString("Success_Title_Privacy"),
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Ошибка: {ex.Message}");
                var dialog = new ContentDialog
                {
                    Title = resourceLoader.GetString("Dialog_Error_Title"),
                    Content = $"{ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
            finally
            {
                ProgressPanel.Visibility = Visibility.Collapsed;
                ApplyButton.IsEnabled = true;
                ResetButton.IsEnabled = true;
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            DisableTelemetryToggle.IsChecked = false;
            DisableAdvertisingIdToggle.IsChecked = false;
            DisableLocationToggle.IsChecked = false;
            DisableCortanaToggle.IsChecked = false;
            DisableBackgroundAppsToggle.IsChecked = false;
            DisableCloudContentToggle.IsChecked = false;
        }
    }
}