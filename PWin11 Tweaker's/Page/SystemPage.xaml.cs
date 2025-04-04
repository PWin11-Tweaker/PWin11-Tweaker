using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Security.Principal;
using PWin11_Tweaker_s.Script;

namespace PWin11_Tweaker_s
{
    public sealed partial class SystemPage : Page
    {
        private bool disableServicesRequested = false;
        private bool speedUpWindowsRequested = false;

        public SystemPage()
        {
            this.InitializeComponent();
            LoadCurrentSettings();

            if (!IsUserAdministrator())
            {
                var dialog = new ContentDialog
                {
                    Title = "Требуются права администратора",
                    Content = "Для изменения системных настроек запустите приложение от имени администратора.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                _ = dialog.ShowAsync();
                ApplyButton.IsEnabled = false;
                DisableServicesButton.IsEnabled = false;
                SpeedUpWindowsButton.IsEnabled = false;
            }

            DisableServicesButton.Click += (s, e) => disableServicesRequested = true;
            SpeedUpWindowsButton.Click += (s, e) => speedUpWindowsRequested = true;
        }

        private void LoadCurrentSettings()
        {
            try
            {
                // UAC
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    int? enableLUA = key?.GetValue("EnableLUA") as int?;
                    DisableUACCheckBox.IsChecked = enableLUA == 0;
                }

                // История буфера обмена
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Clipboard"))
                {
                    int? clipboardHistory = key?.GetValue("EnableClipboardHistory") as int?;
                    DisableClipboardCheckBox.IsChecked = clipboardHistory == 0;
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
                DisableServicesButton.IsEnabled = false;
                SpeedUpWindowsButton.IsEnabled = false;
                StatusText.Text = "Подготовка...";
                ProgressBar.Value = 0;
                await Task.Delay(100);

                string regContent = "Windows Registry Editor Version 5.00\n\n";
                string batContent = "@echo off\n";

                // Отключение ненужных служб
                if (disableServicesRequested)
                {
                    string[] services = { "WSearch", "Fax", "Spooler", "RemoteRegistry", "WaaSMedicSvc" };
                    foreach (var service in services)
                    {
                        regContent += $"[HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\{service}]\n" +
                                      "\"Start\"=dword:00000004\n\n";
                        batContent += $"sc stop {service} >nul 2>&1\n";
                    }
                    TweakStatus.IsServicesDisabled = true; // Предполагается свойство в TweakStatus
                }

                // Отключение UAC
                bool disableUAC = DisableUACCheckBox.IsChecked ?? false;
                regContent += @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System]" + "\n" +
                              $"\"EnableLUA\"=dword:0000000{(disableUAC ? 0 : 1)}\n\n";
                TweakStatus.IsUACDisabled = disableUAC;

                // Отключение истории буфера обмена
                bool disableClipboard = DisableClipboardCheckBox.IsChecked ?? false;
                regContent += @"[HKEY_CURRENT_USER\Software\Microsoft\Clipboard]" + "\n" +
                              $"\"EnableClipboardHistory\"=dword:0000000{(disableClipboard ? 0 : 1)}\n\n";
                TweakStatus.IsClipboardHistoryDisabled = disableClipboard;

                // Ускорение Windows
                if (speedUpWindowsRequested)
                {
                    regContent += @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects]" + "\n" +
                                  "\"VisualFXSetting\"=dword:00000002\n" +
                                  @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize]" + "\n" +
                                  "\"StartupDelayInMSec\"=dword:00000000\n\n";
                    TweakStatus.IsWindowsSpeedUpApplied = true; // Предполагается свойство в TweakStatus
                }

                // Применение изменений
                StatusText.Text = "Сохранение изменений...";
                ProgressBar.Value = 50;
                await Task.Delay(100);

                string tempRegPath = Path.Combine(Path.GetTempPath(), "SystemTweaks.reg");
                File.WriteAllText(tempRegPath, regContent, Encoding.Unicode);

                string tempBatPath = Path.Combine(Path.GetTempPath(), "SystemTweaks.bat");
                batContent += $"reg import \"{tempRegPath}\" >nul 2>&1\n" +
                              "if %ERRORLEVEL% NEQ 0 (exit /b %ERRORLEVEL%)\n" +
                              $"del \"{tempRegPath}\" >nul 2>&1\n" +
                              "taskkill /f /im explorer.exe >nul 2>&1\n" +
                              "start explorer.exe\n" +
                              "exit /b 0";
                File.WriteAllText(tempBatPath, batContent);

                StatusText.Text = "Применение изменений...";
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

                StatusText.Text = "Готово!";
                ProgressBar.Value = 100;
                await Task.Delay(500);

                var dialog = new ContentDialog
                {
                    Title = "Успех",
                    Content = "Настройки системы успешно применены! Для некоторых изменений может потребоваться перезагрузка.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();

                // Сброс флагов после успешного применения
                disableServicesRequested = false;
                speedUpWindowsRequested = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Ошибка: {ex.Message}");
                var dialog = new ContentDialog
                {
                    Title = "Ошибка",
                    Content = $"Не удалось применить настройки: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
            finally
            {
                ProgressPanel.Visibility = Visibility.Collapsed;
                ApplyButton.IsEnabled = IsUserAdministrator();
                DisableServicesButton.IsEnabled = IsUserAdministrator();
                SpeedUpWindowsButton.IsEnabled = IsUserAdministrator();
            }
        }

        private bool IsUserAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}