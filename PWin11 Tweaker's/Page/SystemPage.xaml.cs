using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Text;

namespace PWin11_Tweaker_s
{
    public sealed partial class SystemPage : Page
    {
        private bool disableServicesRequested = false;
        private bool speedUpWindowsRequested = false;

        public SystemPage()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("SystemPage: Начало инициализации...");
                this.InitializeComponent();
                System.Diagnostics.Debug.WriteLine("SystemPage: InitializeComponent завершён.");
                
                System.Diagnostics.Debug.WriteLine("SystemPage: LoadCurrentSettings завершён.");

                

                DisableServicesButton.Click += (s, e) => disableServicesRequested = true;
                SpeedUpWindowsButton.Click += (s, e) => speedUpWindowsRequested = true;
                ApplyButton.Click += ApplyButton_Click;
                System.Diagnostics.Debug.WriteLine("SystemPage: События кнопок привязаны.");
                System.Diagnostics.Debug.WriteLine("SystemPage: Инициализация завершена успешно.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SystemPage: Ошибка при инициализации: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw; // Бросаем исключение, чтобы увидеть его в отладчике
            }
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ApplyButton.IsEnabled = false;
                DisableServicesButton.IsEnabled = false;
                SpeedUpWindowsButton.IsEnabled = false;

                string regContent = "Windows Registry Editor Version 5.00\n\n";
                string batContent = "@echo off\n";

                if (disableServicesRequested)
                {
                    string[] services = { "WSearch", "Fax", "Spooler", "RemoteRegistry", "WaaSMedicSvc" };
                    foreach (var service in services)
                    {
                        regContent += $"[HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\{service}]\n" +
                                      "\"Start\"=dword:00000004\n\n";
                        batContent += $"sc stop {service} >nul 2>&1\n";
                    }
                    
                }

                bool disableUAC = DisableUACCheckBox.IsChecked ?? false;
                regContent += @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System]" + "\n" +
                              $"\"EnableLUA\"=dword:0000000{(disableUAC ? 0 : 1)}\n\n";
               

                bool disableClipboard = DisableClipboardCheckBox.IsChecked ?? false;
                regContent += @"[HKEY_CURRENT_USER\Software\Microsoft\Clipboard]" + "\n" +
                              $"\"EnableClipboardHistory\"=dword:0000000{(disableClipboard ? 0 : 1)}\n\n";
                

                if (speedUpWindowsRequested)
                {
                    regContent += @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects]" + "\n" +
                                  "\"VisualFXSetting\"=dword:00000002\n" +
                                  @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize]" + "\n" +
                                  "\"StartupDelayInMSec\"=dword:00000000\n\n";
                    
                }

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

                disableServicesRequested = false;
                speedUpWindowsRequested = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Ошибка: {ex.Message}");
            }
            finally
            {
            }
        }
    }
}