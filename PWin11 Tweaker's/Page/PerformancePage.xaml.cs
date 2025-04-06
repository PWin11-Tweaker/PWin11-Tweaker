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
    public sealed partial class PerformancePage : Page
    {

        //Для локализации
        private readonly ResourceLoader resourceLoader;

        public PerformancePage()
        {
            this.InitializeComponent();
            resourceLoader = new ResourceLoader();
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            try
            {
                // Визуальные эффекты
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects"))
                {
                    int? visualEffects = key?.GetValue("VisualFXSetting") as int?;
                    DisableVisualEffectsToggle.IsChecked = visualEffects == 2; // 2 = отключены
                }

                // Windows Search
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\WSearch"))
                {
                    int? startValue = key?.GetValue("Start") as int?;
                    DisableWindowsSearchToggle.IsChecked = startValue == 4; // 4 = отключена
                }

                // SysMain
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\SysMain"))
                {
                    int? startValue = key?.GetValue("Start") as int?;
                    DisableSysMainToggle.IsChecked = startValue == 4; // 4 = отключена
                }

                // План электропитания
                Process powercfg = Process.Start(new ProcessStartInfo
                {
                    FileName = "powercfg",
                    Arguments = "/getactivescheme",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                string output = powercfg.StandardOutput.ReadToEnd();
                powercfg.WaitForExit();
                if (output.Contains("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"))
                    PowerPlanCombo.SelectedIndex = 0; // High Performance
                else if (output.Contains("381b4222-f694-41f0-9685-ff5bb260df2e"))
                    PowerPlanCombo.SelectedIndex = 1; // Balanced
                else if (output.Contains("a1841308-3541-4fab-bc81-f71556f20b4a"))
                    PowerPlanCombo.SelectedIndex = 2; // Power Saver
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: Ошибка: {ex.Message}");
            }
        }

        private void PowerPlanCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Можно сразу применять, но оставим для кнопки "Применить"
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProgressPanel.Visibility = Visibility.Visible;
                ApplyButton.IsEnabled = false;
                StatusText.Text = resourceLoader.GetString("Preparation");
                ProgressBar.Value = 0;
                await Task.Delay(100);

                string regContent = "Windows Registry Editor Version 5.00\n\n";
                string batContent = "@echo off\n";

                // Визуальные эффекты
                bool disableEffects = DisableVisualEffectsToggle.IsChecked ?? false;
                regContent += @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects]" + "\n" +
                              $"\"VisualFXSetting\"=dword:0000000{(disableEffects ? 2 : 1)}\n\n";
                if (disableEffects)
                {
                    regContent += @"[HKEY_CURRENT_USER\Control Panel\Desktop]" + "\n" +
                                  "\"UserPreferencesMask\"=hex:90,12,03,80,10,00,00,00\n\n";
                }

                // Windows Search
                bool disableSearch = DisableWindowsSearchToggle.IsChecked ?? false;
                regContent += @"[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WSearch]" + "\n" +
                              $"\"Start\"=dword:0000000{(disableSearch ? 4 : 3)}\n\n";
                if (disableSearch)
                    batContent += "sc stop WSearch >nul 2>&1\n";

                // SysMain
                bool disableSysMain = DisableSysMainToggle.IsChecked ?? false;
                regContent += @"[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SysMain]" + "\n" +
                              $"\"Start\"=dword:0000000{(disableSysMain ? 4 : 3)}\n\n";
                if (disableSysMain)
                    batContent += "sc stop SysMain >nul 2>&1\n";

                // План электропитания
                string powerPlanGuid = PowerPlanCombo.SelectedItem is ComboBoxItem item ? item.Tag.ToString() switch
                {
                    "HighPerformance" => "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c",
                    "Balanced" => "381b4222-f694-41f0-9685-ff5bb260df2e",
                    "PowerSaver" => "a1841308-3541-4fab-bc81-f71556f20b4a",
                    _ => "381b4222-f694-41f0-9685-ff5bb260df2e"
                } : "381b4222-f694-41f0-9685-ff5bb260df2e";
                batContent += $"powercfg /setactive {powerPlanGuid} >nul 2>&1\n";

                // Сохранение и применение
                StatusText.Text = resourceLoader.GetString("Apply_Change");
                ProgressBar.Value = 50;
                await Task.Delay(100);

                string tempRegPath = Path.Combine(Path.GetTempPath(), "PerformanceTweaks.reg");
                File.WriteAllText(tempRegPath, regContent, Encoding.Unicode);

                string tempBatPath = Path.Combine(Path.GetTempPath(), "PerformanceTweaks.bat");
                batContent += $"reg import \"{tempRegPath}\" >nul 2>&1\n" +
                              "if %ERRORLEVEL% NEQ 0 (exit /b %ERRORLEVEL%)\n" +
                              $"del \"{tempRegPath}\" >nul 2>&1\n" +
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
                    Content = resourceLoader.GetString("Success_Title_Performance"),
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
            }
        }
    }
}