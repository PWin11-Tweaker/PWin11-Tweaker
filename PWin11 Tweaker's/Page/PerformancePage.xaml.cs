using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Security.Principal;
using Windows.ApplicationModel.Resources;

namespace PWin11_Tweaker_s
{
    public sealed partial class PerformancePage : Page
    {
        private ResourceLoader _resourceLoader;

        public PerformancePage()
        {
            try
            {
                this.InitializeComponent();
                System.Diagnostics.Debug.WriteLine("PerformancePage: InitializeComponent завершён.");

                // Инициализация ResourceLoader для текущего языка
                _resourceLoader = ResourceLoader.GetForViewIndependentUse($"Strings/{LocalizationManager.CurrentLanguage}/Resources");

                // Проверка прав администратора
                if (!IsAdministrator())
                {
                    ShowErrorDialog(LocalizationManager.GetString("AdminRightsRequired"));
                    ApplyButton.IsEnabled = false;
                }

                LoadCurrentSettings();
                System.Diagnostics.Debug.WriteLine("PerformancePage: LoadCurrentSettings завершён.");

                // Подписываемся на событие смены языка
                LocalizationManager.LanguageChanged += LocalizationManager_LanguageChanged;

                // Инициализация текста элементов (для первого запуска)
                UpdateUIText();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PerformancePage: Ошибка при инициализации: {ex.Message}\nStackTrace: {ex.StackTrace}");
                ShowErrorDialog($"{LocalizationManager.GetString("ErrorDialog.Content")} {ex.Message}");
            }
        }

        private bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private async void ShowErrorDialog(string message)
        {
            var dialog = new ContentDialog
            {
                Title = LocalizationManager.GetString("ErrorDialog.Title"),
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private void LocalizationManager_LanguageChanged(object sender, EventArgs e)
        {
            // Обновляем ResourceLoader для нового языка
            _resourceLoader = ResourceLoader.GetForViewIndependentUse($"Strings/{LocalizationManager.CurrentLanguage}/Resources");

            // Обновляем текст UI-элементов
            UpdateUIText();

            System.Diagnostics.Debug.WriteLine("PerformancePage: UI обновлён после смены языка.");
        }

        private void UpdateUIText()
        {
            // Обновляем текст всех элементов вручную, используя ResourceLoader
            // Заголовок страницы
            var titleTextBlock = this.FindName("TitleTextBlock") as TextBlock;
            if (titleTextBlock != null)
            {
                titleTextBlock.Text = _resourceLoader.GetString("PerformancePage.Title");
            }

            // CheckBox: Отключение визуальных эффектов
            DisableVisualEffectsToggle.Content = _resourceLoader.GetString("DisableVisualEffectsToggle.Content");

            // CheckBox: Отключение Windows Search
            DisableWindowsSearchToggle.Content = _resourceLoader.GetString("DisableWindowsSearchToggle.Content");

            // CheckBox: Отключение SysMain
            DisableSysMainToggle.Content = _resourceLoader.GetString("DisableSysMainToggle.Content");

            // Метка для плана электропитания
            var powerPlanLabel = this.FindName("PowerPlanLabel") as TextBlock;
            if (powerPlanLabel != null)
            {
                powerPlanLabel.Text = _resourceLoader.GetString("PowerPlanLabel.Text");
            }

            // Элементы ComboBox для плана электропитания
            foreach (ComboBoxItem item in PowerPlanCombo.Items)
            {
                if (item.Tag.ToString() == "HighPerformance")
                    item.Content = _resourceLoader.GetString("PowerPlanCombo.HighPerformance");
                else if (item.Tag.ToString() == "Balanced")
                    item.Content = _resourceLoader.GetString("PowerPlanCombo.Balanced");
                else if (item.Tag.ToString() == "PowerSaver")
                    item.Content = _resourceLoader.GetString("PowerPlanCombo.PowerSaver");
            }

            // Кнопка "Применить"
            ApplyButton.Content = _resourceLoader.GetString("ApplyButton.Content");
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
                StatusText.Text = LocalizationManager.GetString("StatusText.Preparing");
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
                StatusText.Text = LocalizationManager.GetString("StatusText.SavingChanges");
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

                StatusText.Text = LocalizationManager.GetString("StatusText.ApplyingChanges");
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
                        throw new Exception($"Error applying settings, code: {process.ExitCode}");
                    }
                }

                StatusText.Text = LocalizationManager.GetString("StatusText.Completed");
                ProgressBar.Value = 100;
                await Task.Delay(500);

                var dialog = new ContentDialog
                {
                    Title = LocalizationManager.GetString("SuccessDialog.Title"),
                    Content = LocalizationManager.GetString("PerformanceSuccessDialog.Content"),
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
                    Title = LocalizationManager.GetString("ErrorDialog.Title"),
                    Content = $"{LocalizationManager.GetString("ErrorDialog.Content")} {ex.Message}",
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