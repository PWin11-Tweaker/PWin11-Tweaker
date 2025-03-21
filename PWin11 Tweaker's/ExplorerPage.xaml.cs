using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace PWin11_Tweaker_s
{
    public sealed partial class ExplorerPage : Page
    {
        public ExplorerPage()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Инициализация ExplorerPage...");
                this.InitializeComponent();
                LoadCurrentSettings();
                System.Diagnostics.Debug.WriteLine("ExplorerPage успешно инициализирован.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при инициализации ExplorerPage: {ex.Message}");
                throw; // Для отладки, чтобы увидеть ошибку
            }
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Начало применения настроек...");

                // Создаём содержимое .reg файла
                string regContent = "Windows Registry Editor Version 5.00\n\n";

                // Твик: Показывать скрытые файлы
                bool showHiddenFiles = ShowHiddenFiles.IsChecked == true;
                regContent += $"[HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced]\n" +
                              $"\"Hidden\"=dword:{(showHiddenFiles ? "00000001" : "00000000")}\n" +
                              $"\"ShowSuperHidden\"=dword:{(showHiddenFiles ? "00000001" : "00000000")}\n\n";

                // Твик: Уменьшение кнопок Закрыть/Свернуть/Развернуть
                bool useSmallCaptions = UseSmallCaptions.IsChecked == true;
                string captionHeightValue = useSmallCaptions ? "-180" : "-330"; // -180 для маленьких заголовков, -330 для стандартных
                regContent += $"[HKEY_CURRENT_USER\\Control Panel\\Desktop\\WindowMetrics]\n" +
                              $"\"CaptionHeight\"=\"{captionHeightValue}\"\n\n";

                // Сохраняем .reg файл с кодировкой UTF-16 LE
                string tempRegPath = Path.Combine(Path.GetTempPath(), "PWin11Tweaker.reg");
                File.WriteAllText(tempRegPath, regContent, Encoding.Unicode);
                System.Diagnostics.Debug.WriteLine($"Создан .reg файл: {tempRegPath}");

                // Создаём .bat файл для применения настроек
                string tempBatPath = Path.Combine(Path.GetTempPath(), "PWin11TweakerApply.bat");
                string tempLogPath = Path.Combine(Path.GetTempPath(), "PWin11TweakerLog.txt");
                string batContent = "@echo off\n" +
                                   $"echo Начало применения настроек > \"{tempLogPath}\"\n" +
                                   $"echo Выполняется: reg import \"{tempRegPath}\" >> \"{tempLogPath}\"\n" +
                                   $"reg import \"{tempRegPath}\" >> \"{tempLogPath}\" 2>&1\n" +
                                   "if %ERRORLEVEL% NEQ 0 (\n" +
                                   $"    echo Не удалось применить .reg файл, код ошибки: %ERRORLEVEL% >> \"{tempLogPath}\"\n" +
                                   "    exit /b %ERRORLEVEL%\n" +
                                   ")\n" +
                                   $"echo .reg файл успешно применён >> \"{tempLogPath}\"\n" +
                                   $"del \"{tempRegPath}\" >> \"{tempLogPath}\" 2>&1\n" +
                                   "exit /b 0";
                File.WriteAllText(tempBatPath, batContent);
                System.Diagnostics.Debug.WriteLine($"Создан .bat файл: {tempBatPath}");

                // Запускаем .bat файл
                ProcessStartInfo batProcess = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C \"{tempBatPath}\"",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                bool success = false;
                using (Process process = Process.Start(batProcess))
                {
                    process.WaitForExit(5000); // Ждём до 5 секунд
                    if (process.ExitCode == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("Настройки успешно применены!");
                        success = true;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Произошла ошибка при выполнении .bat, код: {process.ExitCode}. Проверь лог: {tempLogPath}");
                        success = false;
                    }

                    // Читаем лог
                    if (File.Exists(tempLogPath))
                    {
                        try
                        {
                            string logContent = File.ReadAllText(tempLogPath);
                            System.Diagnostics.Debug.WriteLine("Лог выполнения:\n" + logContent);
                        }
                        catch (IOException ioEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Не удалось прочитать лог: {ioEx.Message}. Продолжаем...");
                        }
                    }
                }

                // Очищаем временные файлы
                try
                {
                    if (File.Exists(tempRegPath)) File.Delete(tempRegPath);
                    if (File.Exists(tempBatPath)) File.Delete(tempBatPath);
                    if (File.Exists(tempLogPath)) File.Delete(tempLogPath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при удалении временных файлов: {ex.Message}");
                }

                // Перезапускаем Проводник автоматически
                if (success)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("Перезапускаем Проводник...");
                        ProcessStartInfo taskKillInfo = new()
                        {
                            FileName = "taskkill",
                            Arguments = "/f /im explorer.exe",
                            UseShellExecute = true,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        };
                        Process? taskKillProcess = Process.Start(taskKillInfo);
                        if (taskKillProcess != null)
                        {
                            taskKillProcess.WaitForExit(2000);
                            System.Diagnostics.Debug.WriteLine("Процесс explorer.exe успешно завершён.");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Ошибка: Не удалось запустить taskkill для завершения explorer.exe.");
                        }

                        ProcessStartInfo explorerInfo = new()
                        {
                            FileName = "explorer.exe",
                            UseShellExecute = true,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        };
                        Process? explorerProcess = Process.Start(explorerInfo);
                        if (explorerProcess != null)
                        {
                            System.Diagnostics.Debug.WriteLine("Проводник успешно запущен заново.");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Ошибка: Не удалось запустить explorer.exe.");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка при перезапуске Проводника: {ex.Message}");
                    }

                    // Показываем уведомление об успехе с предупреждением о необходимости перезапуска
                    ContentDialog successDialog = new()
                    {
                        Title = "Успех",
                        Content = "Настройки успешно применены! Проводник перезапущен.\nДля применения уменьшения кнопок управления окном может потребоваться перезапуск системы.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await successDialog.ShowAsync();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Не удалось применить настройки. Проверьте лог: " + tempLogPath);
                    ContentDialog errorDialog = new()
                    {
                        Title = "Ошибка",
                        Content = "Не удалось применить настройки. Проверьте лог: " + tempLogPath,
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Общая ошибка в ApplyButton_Click: {ex.Message}");
                ContentDialog errorDialog = new()
                {
                    Title = "Ошибка",
                    Content = $"Произошла ошибка: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }

        private void LoadCurrentSettings()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Загрузка текущих настроек...");
                // Загрузка для ShowHiddenFiles
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
                if (key != null)
                {
                    ShowHiddenFiles.IsChecked = (int?)key.GetValue("Hidden", 0) == 1;
                }

                // Загрузка для UseSmallCaptions
                using RegistryKey? windowMetricsKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop\WindowMetrics");
                if (windowMetricsKey != null)
                {
                    string? captionHeight = (string?)windowMetricsKey.GetValue("CaptionHeight", "-330");
                    // Если значение меньше -330 (например, -180), считаем, что твик включён
                    if (int.TryParse(captionHeight, out int height) && height > -330)
                    {
                        UseSmallCaptions.IsChecked = true;
                    }
                    else
                    {
                        UseSmallCaptions.IsChecked = false;
                    }
                }

                System.Diagnostics.Debug.WriteLine("Текущие настройки успешно загружены.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки настроек: {ex.Message}");
            }
        }
    }
}