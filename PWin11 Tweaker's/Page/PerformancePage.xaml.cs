using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using Microsoft.Windows.ApplicationModel.Resources;
using PWin11_Tweaker_s.Script;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PWin11_Tweaker_s
{
    public sealed partial class PerformancePage : Microsoft.UI.Xaml.Controls.Page
    {
        private readonly ResourceLoader resourceLoader;
        private const string ProcessMonitorUrl = "https://download.sysinternals.com/files/ProcessMonitor.zip";
        private const string InstallPath = @"Assets\ProcessMonitor";
        private const string ExeName = "Procmon.exe";

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
                Debug.WriteLine($"PerformancePage: Ошибка при инициализации: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private void LoadCurrentSettings()
        {
            try
            {
                Debug.WriteLine("LoadCurrentSettings: Начало загрузки настроек...");

                // Отключение индексации поиска
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search"))
                {
                    int? allowIndexing = key?.GetValue("AllowIndexingEncryptedStores") as int?;
                    using (var serviceKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\WSearch"))
                    {
                        int? startValue = serviceKey?.GetValue("Start") as int?;
                        DisableSearchIndexingToggle.IsChecked = startValue == 4 || allowIndexing == 0;
                    }
                }

                // Визуальные эффекты
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects"))
                {
                    int? visualEffects = key != null && key.GetValue("VisualFXSetting") is int value ? value : null;
                    DisableVisualEffectsToggle.IsChecked = visualEffects == 2;
                }
                DisableVisualEffectsToggle.IsChecked = TweakStatus.IsVisualEffectsDisabled;

                // Windows Search
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\WSearch"))
                {
                    int? startValue = key?.GetValue("Start") as int?;
                    DisableWindowsSearchToggle.IsChecked = startValue == 4;
                }
                DisableWindowsSearchToggle.IsChecked = TweakStatus.IsWindowsSearchDisabled;

                // SysMain
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\SysMain"))
                {
                    int? startValue = key?.GetValue("Start") as int?;
                    DisableSysMainToggle.IsChecked = startValue == 4;
                }
                DisableSysMainToggle.IsChecked = TweakStatus.IsSysMainDisabled;

                // План электропитания
                Process? powercfg = Process.Start(new ProcessStartInfo
                {
                    FileName = "powercfg",
                    Arguments = "/getactivescheme",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (powercfg == null)
                {
                    Debug.WriteLine("Не удалось запустить процесс powercfg.");
                    return;
                }

                string output = powercfg.StandardOutput.ReadToEnd();
                powercfg.WaitForExit();
                if (output.Contains("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"))
                    PowerPlanCombo.SelectedIndex = 0; // High Performance
                else if (output.Contains("381b4222-f694-41f0-9685-ff5bb260df2e"))
                    PowerPlanCombo.SelectedIndex = 1; // Balanced
                else if (output.Contains("a1841308-3541-4fab-bc81-f71556f20b4a"))
                    PowerPlanCombo.SelectedIndex = 2; // Power Saver
                else
                    PowerPlanCombo.SelectedIndex = 1; // Balanced по умолчанию

                if (TweakStatus.CurrentPowerPlan == "HighPerformance") PowerPlanCombo.SelectedIndex = 0;
                else if (TweakStatus.CurrentPowerPlan == "Balanced") PowerPlanCombo.SelectedIndex = 1;
                else if (TweakStatus.CurrentPowerPlan == "PowerSaver") PowerPlanCombo.SelectedIndex = 2;

                Debug.WriteLine("LoadCurrentSettings: Текущие настройки успешно загружены.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadCurrentSettings: Ошибка: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private void PowerPlanCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Оставим для кнопки "Применить"
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

                // Отключение индексации поиска
                bool disableIndexing = DisableSearchIndexingToggle.IsChecked ?? false;
                regContent += @"[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Search]" + "\n" +
                              $"\"AllowIndexingEncryptedStores\"=dword:0000000{(disableIndexing ? 0 : 1)}\n\n";
                regContent += @"[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WSearch]" + "\n" +
                              $"\"Start\"=dword:0000000{(disableIndexing ? 4 : 2)}\n\n";
                if (disableIndexing)
                    batContent += "sc stop WSearch >nul 2>&1\n";

                // Визуальные эффекты
                bool disableEffects = DisableVisualEffectsToggle.IsChecked ?? false;
                regContent += @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects]" + "\n" +
                              $"\"VisualFXSetting\"=dword:0000000{(disableEffects ? 2 : 1)}\n\n";
                if (disableEffects)
                {
                    regContent += @"[HKEY_CURRENT_USER\Control Panel\Desktop]" + "\n" +
                                  "\"UserPreferencesMask\"=hex:90,12,03,80,10,00,00,00\n\n";
                }
                TweakStatus.IsVisualEffectsDisabled = disableEffects;

                // Windows Search
                bool disableSearch = DisableWindowsSearchToggle.IsChecked ?? false;
                regContent += @"[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WSearch]" + "\n" +
                              $"\"Start\"=dword:0000000{(disableSearch ? 4 : 3)}\n\n";
                if (disableSearch)
                    batContent += "sc stop WSearch >nul 2>&1\n";
                TweakStatus.IsWindowsSearchDisabled = disableSearch;

                // SysMain
                bool disableSysMain = DisableSysMainToggle.IsChecked ?? false;
                regContent += @"[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SysMain]" + "\n" +
                              $"\"Start\"=dword:0000000{(disableSysMain ? 4 : 3)}\n\n";
                if (disableSysMain)
                    batContent += "sc stop SysMain >nul 2>&1\n";
                TweakStatus.IsSysMainDisabled = disableSysMain;

                // План электропитания
                string powerPlanGuid = PowerPlanCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag ? tag switch
                {
                    "HighPerformance" => "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c",
                    "Balanced" => "381b4222-f694-41f0-9685-ff5bb260df2e",
                    "PowerSaver" => "a1841308-3541-4fab-bc81-f71556f20b4a",
                    _ => "381b4222-f694-41f0-9685-ff5bb260df2e"
                } : "381b4222-f694-41f0-9685-ff5bb260df2e";
                batContent += $"powercfg /setactive {powerPlanGuid} >nul 2>&1\n";
                TweakStatus.CurrentPowerPlan = PowerPlanCombo.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is string selectedTag ? selectedTag : "Balanced";

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

                Process? process = Process.Start(processInfo);
                if (process == null)
                {
                    throw new Exception("Не удалось запустить процесс для применения настроек.");
                }

                using (process)
                {
                    process.WaitForExit(5000);
                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"Ошибка применения настроек, код: {process.ExitCode}");
                    }
                }

                // Сохранение состояния после применения
                TweakStatus.SaveSettings();

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
                Debug.WriteLine($"ApplyButton_Click: Ошибка: {ex.Message}\nStackTrace: {ex.StackTrace}");
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

        private void UpdateProcessMonitorButton()
        {
            string processMonitorPath = Path.Combine(AppContext.BaseDirectory, InstallPath, ExeName);
            ProcessMonitorButton.Content = File.Exists(processMonitorPath)
                ? resourceLoader.GetString("Uninstall_ProcessMonitor")
                : resourceLoader.GetString("Install_ProcessMonitor");
            Debug.WriteLine($"UpdateProcessMonitorButton: Статус кнопки обновлён, Process Monitor {(File.Exists(processMonitorPath) ? "установлен" : "не установлен")}");
        }

        private async void ProcessMonitorButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProgressPanel.Visibility = Visibility.Visible;
                ProcessMonitorButton.IsEnabled = false;
                StatusText.Text = resourceLoader.GetString("Preparation");
                ProgressBar.Value = 0;
                await Task.Delay(100);

                string processMonitorPath = Path.Combine(AppContext.BaseDirectory, InstallPath, ExeName);

                if (File.Exists(processMonitorPath))
                {
                    // Удаление Process Monitor
                    Debug.WriteLine("ProcessMonitorButton_Click: Начало удаления Process Monitor...");
                    StatusText.Text = resourceLoader.GetString("Uninstalling_ProcessMonitor");
                    ProgressBar.Value = 30;
                    await Task.Delay(100);

                    string installDir = Path.Combine(AppContext.BaseDirectory, InstallPath);
                    if (Directory.Exists(installDir))
                    {
                        Directory.Delete(installDir, true);
                        Debug.WriteLine($"ProcessMonitorButton_Click: Папка {installDir} удалена.");
                    }

                    StatusText.Text = resourceLoader.GetString("Success");
                    ProgressBar.Value = 100;
                    await Task.Delay(500);
                }
                else
                {
                    // Установка Process Monitor
                    Debug.WriteLine("ProcessMonitorButton_Click: Начало установки Process Monitor...");
                    StatusText.Text = resourceLoader.GetString("Downloading_ProcessMonitor");
                    ProgressBar.Value = 10;
                    await Task.Delay(100);

                    string tempZipPath = Path.Combine(Path.GetTempPath(), "ProcessMonitor.zip");
                    using (var client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                        var response = await client.GetAsync(ProcessMonitorUrl);
                        if (!response.IsSuccessStatusCode)
                        {
                            throw new Exception($"Ошибка скачивания: {response.StatusCode}");
                        }

                        using (var fs = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write))
                        {
                            await response.Content.CopyToAsync(fs);
                        }
                    }
                    Debug.WriteLine($"ProcessMonitorButton_Click: Process Monitor скачан в {tempZipPath}");

                    StatusText.Text = resourceLoader.GetString("Installing_ProcessMonitor");
                    ProgressBar.Value = 40;
                    await Task.Delay(100);

                    string installDir = Path.Combine(AppContext.BaseDirectory, InstallPath);
                    if (Directory.Exists(installDir))
                        Directory.Delete(installDir, true);
                    Directory.CreateDirectory(installDir);

                    using (ZipArchive archive = ZipFile.OpenRead(tempZipPath))
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            string destinationPath = Path.Combine(installDir, entry.FullName);
                            if (entry.Name != "")
                            {
                                entry.ExtractToFile(destinationPath, true);
                            }
                        }
                    }
                    File.Delete(tempZipPath);
                    Debug.WriteLine($"ProcessMonitorButton_Click: Process Monitor установлен в {installDir}");

                    StatusText.Text = resourceLoader.GetString("Success");
                    ProgressBar.Value = 100;
                    await Task.Delay(500);
                }

                UpdateProcessMonitorButton();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ProcessMonitorButton_Click: Ошибка: {ex.Message} StackTrace: {ex.StackTrace}");
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
                ProcessMonitorButton.IsEnabled = true;
            }
        }
    }
}