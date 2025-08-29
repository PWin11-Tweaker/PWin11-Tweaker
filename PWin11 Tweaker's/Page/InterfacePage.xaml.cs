using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using PWin11_Tweaker_s.Script;
using Microsoft.Windows.ApplicationModel.Resources; // Для работы локализации

namespace PWin11_Tweaker_s
{
    public sealed partial class InterfacePage : Microsoft.UI.Xaml.Controls.Page
    {
        //Для локализации
        private readonly ResourceLoader resourceLoader;

        public InterfacePage()
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
                // Выравнивание панели задач
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                {
                    int? alignment = key?.GetValue("TaskbarAl") as int?;
                    TaskbarAlignmentCombo.SelectedIndex = alignment == 0 ? 1 : 0; // 0 = слева, 1 = по центру
                }


                // Прозрачность панели задач
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    int? transparency = key?.GetValue("EnableTransparency") as int?;
                    TaskbarTransparencyToggle.IsChecked = transparency == 1;
                }

                // Скрытие кнопки поиска
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Search"))
                {
                    int? searchboxTaskbarMode = key?.GetValue("SearchboxTaskbarMode") as int?;
                    HideSearchButtonToggle.IsChecked = searchboxTaskbarMode == 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: Ошибка: {ex.Message}");
            }
        }

        private void TaskbarAlignmentCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Чтобы не было ошибок ^_- (Всё через кнопку применить)
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

                string regContent = "Windows Registry Editor Version 5.00";

                // Выравнивание панели задач
                int alignment = TaskbarAlignmentCombo.SelectedItem is ComboBoxItem item ? int.Parse(item.Tag.ToString()) : 1;
                regContent += @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced]" + "" +
                              $"\"TaskbarAl\"=dword:0000000{alignment}";

                // Прозрачность панели задач
                bool transparency = TaskbarTransparencyToggle.IsChecked ?? false;
                regContent += @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize]" + "" +
                              $"\"EnableTransparency\"=dword:0000000{(transparency ? 1 : 0)}";

                // Скрытие кнопки поиска
                bool hideSearch = HideSearchButtonToggle.IsChecked ?? false;
                regContent += @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search]" + "" +
                              $"\"SearchboxTaskbarMode\"=dword:0000000{(hideSearch ? 0 : 1)}";

                // Сохранение и применение изменений
                StatusText.Text = resourceLoader.GetString("Apply_Change");
                ProgressBar.Value = 50;
                await Task.Delay(100);

                string tempRegPath = Path.Combine(Path.GetTempPath(), "InterfaceTweaks.reg");
                File.WriteAllText(tempRegPath, regContent, Encoding.Unicode);

                string tempBatPath = Path.Combine(Path.GetTempPath(), "InterfaceTweaks.bat");
                string batContent = $"@echo off reg import \"{tempRegPath}\" >nul 2>&1" +
                                   "if %ERRORLEVEL% NEQ 0 (exit /b %ERRORLEVEL%)" +
                                   $"del \"{tempRegPath}\" >nul 2>&1" +
                                   "taskkill /f /im explorer.exe >nul 2>&1" +
                                   "start explorer.exe" +
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
                    Content = resourceLoader.GetString("Success_Title_Interface"),
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