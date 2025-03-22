using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PWin11_Tweaker_s
{
    public sealed partial class ExplorerPage : Page
    {
        private const string StartAllBackUrl = "https://www.startallback.com/download.php"; // URL для скачивания StartAllBack
        private const string StartAllBackExePath = @"C:\Program Files\StartAllBack\StartAllBackCfg.exe"; // Путь к установленной программе
        private bool isStartAllBackInstalled; // Переменная для отслеживания состояния установки

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

        private async void InstallStartAllBackButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Показываем прогресс-бар и отключаем кнопки
                ProgressPanel.Visibility = Visibility.Visible;
                ApplyButton.IsEnabled = false;
                InstallStartAllBackButton.IsEnabled = false;
                StatusText.Text = "Подготовка...";
                ProgressBar.Value = 0;
                await Task.Delay(100);

                if (isStartAllBackInstalled)
                {
                    // Удаляем StartAllBack
                    await UninstallStartAllBack();
                    isStartAllBackInstalled = false;
                    InstallStartAllBackButton.Content = "Установить StartAllBack";
                }
                else
                {
                    // Устанавливаем StartAllBack
                    await DownloadAndInstallStartAllBack();
                    isStartAllBackInstalled = true;
                    InstallStartAllBackButton.Content = "Удалить StartAllBack";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при установке/удалении StartAllBack: {ex.Message}");
                ContentDialog errorDialog = new()
                {
                    Title = "Ошибка",
                    Content = $"Произошла ошибка: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
            finally
            {
                // Скрываем прогресс-бар и включаем кнопки
                ProgressPanel.Visibility = Visibility.Collapsed;
                ApplyButton.IsEnabled = true;
                InstallStartAllBackButton.IsEnabled = true;
            }
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Начало применения настроек...");

                // Показываем прогресс-бар и отключаем кнопки
                ProgressPanel.Visibility = Visibility.Visible;
                ApplyButton.IsEnabled = false;
                InstallStartAllBackButton.IsEnabled = false;
                StatusText.Text = "Подготовка...";
                ProgressBar.Value = 0;
                await Task.Delay(100);

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
                StatusText.Text = "Сохранение изменений в реестре...";
                ProgressBar.Value = 90;
                await Task.Delay(100);
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
                StatusText.Text = "Применение изменений в реестре...";
                ProgressBar.Value = 95;
                await Task.Delay(100);
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
                        StatusText.Text = "Перезапуск Проводника...";
                        ProgressBar.Value = 100;
                        await Task.Delay(100);
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

                    // Показываем уведомление об успехе
                    ContentDialog successDialog = new()
                    {
                        Title = "Успех",
                        Content = "Настройки успешно применены! Проводник перезапущен.\nДля применения уменьшения кнопок управления окном и стиля StartAllBack может потребоваться перезапуск системы.",
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
            finally
            {
                // Скрываем прогресс-бар и включаем кнопки
                ProgressPanel.Visibility = Visibility.Collapsed;
                ApplyButton.IsEnabled = true;
                InstallStartAllBackButton.IsEnabled = true;
            }
        }

        private async Task DownloadAndInstallStartAllBack()
        {
            try
            {
                // Этап 1: Скачивание StartAllBack
                StatusText.Text = "Скачивание StartAllBack...";
                ProgressBar.Value = 10;
                await Task.Delay(100);
                string tempInstallerPath = Path.Combine(Path.GetTempPath(), "StartAllBackSetup.exe");
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                    var response = await client.GetAsync(StartAllBackUrl);
                    response.EnsureSuccessStatusCode();
                    using (var fs = new FileStream(tempInstallerPath, FileMode.Create, FileAccess.Write))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                }
                System.Diagnostics.Debug.WriteLine($"StartAllBack успешно скачан: {tempInstallerPath}");
                ProgressBar.Value = 40;
                await Task.Delay(100);

                // Этап 2: Установка StartAllBack
                StatusText.Text = "Установка StartAllBack...";
                ProcessStartInfo installProcess = new ProcessStartInfo
                {
                    FileName = tempInstallerPath,
                    Arguments = "/silent", // Тихая установка
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (Process process = Process.Start(installProcess))
                {
                    process.WaitForExit(30000); // Ждём до 30 секунд
                    if (process.ExitCode == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("StartAllBack успешно установлен.");
                        ProgressBar.Value = 70;
                        await Task.Delay(100);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка установки StartAllBack, код: {process.ExitCode}");
                        throw new Exception("Не удалось установить StartAllBack.");
                    }
                }

                // Этап 3: Удаление установочного файла
                StatusText.Text = "Очистка временных файлов...";
                if (File.Exists(tempInstallerPath))
                {
                    File.Delete(tempInstallerPath);
                    System.Diagnostics.Debug.WriteLine("Установочный файл StartAllBack удалён.");
                }
                ProgressBar.Value = 80;
                await Task.Delay(100);

                // Этап 4: Применение настроек StartAllBack
                StatusText.Text = "Применение настроек StartAllBack...";
                if (File.Exists(StartAllBackExePath))
                {
                    ProcessStartInfo configProcess = new ProcessStartInfo
                    {
                        FileName = StartAllBackExePath,
                        Arguments = "--apply-style Remastered7", // Применяем стиль Windows 7
                        UseShellExecute = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    using (Process configProc = Process.Start(configProcess))
                    {
                        configProc.WaitForExit(5000);
                        System.Diagnostics.Debug.WriteLine("Настройки StartAllBack применены.");
                    }
                }
                ProgressBar.Value = 90;
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при установке StartAllBack: {ex.Message}");
                throw;
            }
        }

        private async Task UninstallStartAllBack()
        {
            try
            {
                // Этап 1: Завершение всех процессов StartAllBack
                StatusText.Text = "Завершение процессов StartAllBack...";
                ProgressBar.Value = 10;
                await Task.Delay(100);

                string[] processNames = { "StartAllBackCfg", "StartAllBackX64", "StartAllBack" }; // Возможные имена процессов
                foreach (var processName in processNames)
                {
                    try
                    {
                        Process[] processes = Process.GetProcessesByName(processName);
                        if (processes.Length > 0)
                        {
                            foreach (var process in processes)
                            {
                                process.Kill();
                                process.WaitForExit(5000); // Ждём до 5 секунд
                                System.Diagnostics.Debug.WriteLine($"Процесс {processName} (PID: {process.Id}) завершён.");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Процесс {processName} не найден.");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка при завершении процесса {processName}: {ex.Message}");
                    }
                }
                ProgressBar.Value = 30;
                await Task.Delay(100);

                // Этап 2: Попытка удаления через команду /uninstall
                StatusText.Text = "Попытка удаления StartAllBack...";
                bool uninstallSuccess = false;
                if (File.Exists(StartAllBackExePath))
                {
                    ProcessStartInfo uninstallProcess = new ProcessStartInfo
                    {
                        FileName = StartAllBackExePath,
                        Arguments = "/uninstall /silent", // Тихое удаление
                        UseShellExecute = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    try
                    {
                        using (Process process = Process.Start(uninstallProcess))
                        {
                            process.WaitForExit(30000); // Ждём до 30 секунд
                            if (process.ExitCode == 0)
                            {
                                System.Diagnostics.Debug.WriteLine("StartAllBack успешно удалён через команду /uninstall.");
                                uninstallSuccess = true;
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"Ошибка удаления StartAllBack через /uninstall, код: {process.ExitCode}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка при выполнении команды /uninstall: {ex.Message}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Файл StartAllBackCfg.exe не найден, пропускаем удаление через /uninstall.");
                }
                ProgressBar.Value = 50;
                await Task.Delay(100);

                // Этап 3: Альтернативное удаление через реестр, если команда /uninstall не сработала
                if (!uninstallSuccess)
                {
                    StatusText.Text = "Поиск команды удаления в реестре...";
                    string uninstallString = FindUninstallString();
                    if (!string.IsNullOrEmpty(uninstallString))
                    {
                        try
                        {
                            // Убираем кавычки из uninstallString, если они есть
                            uninstallString = uninstallString.Trim('"');
                            ProcessStartInfo registryUninstallProcess = new ProcessStartInfo
                            {
                                FileName = "cmd.exe",
                                Arguments = $"/C \"{uninstallString}\" /silent",
                                UseShellExecute = true,
                                CreateNoWindow = true,
                                WindowStyle = ProcessWindowStyle.Hidden
                            };

                            using (Process process = Process.Start(registryUninstallProcess))
                            {
                                process.WaitForExit(30000); // Ждём до 30 секунд
                                if (process.ExitCode == 0)
                                {
                                    System.Diagnostics.Debug.WriteLine("StartAllBack успешно удалён через реестр.");
                                    uninstallSuccess = true;
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"Ошибка удаления StartAllBack через реестр, код: {process.ExitCode}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Ошибка при удалении через реестр: {ex.Message}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Не удалось найти команду удаления в реестре.");
                    }
                }
                ProgressBar.Value = 60;
                await Task.Delay(100);

                // Этап 4: Удаление оставшихся файлов (если остались)
                StatusText.Text = "Очистка оставшихся файлов...";
                string startAllBackFolder = Path.GetDirectoryName(StartAllBackExePath);
                if (Directory.Exists(startAllBackFolder))
                {
                    try
                    {
                        Directory.Delete(startAllBackFolder, true);
                        System.Diagnostics.Debug.WriteLine("Папка StartAllBack удалена.");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка при удалении папки StartAllBack: {ex.Message}");
                        // Если не удалось удалить, пробуем ещё раз после небольшой задержки
                        await Task.Delay(2000);
                        try
                        {
                            Directory.Delete(startAllBackFolder, true);
                            System.Diagnostics.Debug.WriteLine("Папка StartAllBack удалена после повторной попытки.");
                        }
                        catch (Exception ex2)
                        {
                            System.Diagnostics.Debug.WriteLine($"Повторная ошибка при удалении папки StartAllBack: {ex2.Message}");
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Папка StartAllBack уже отсутствует.");
                }
                ProgressBar.Value = 80;
                await Task.Delay(100);

                // Этап 5: Обновление текста кнопки
                StatusText.Text = "Обновление интерфейса...";
                InstallStartAllBackButton.Content = "Установить StartAllBack";
                ProgressBar.Value = 90;
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при удалении StartAllBack: {ex.Message}");
                throw;
            }
        }

        private string FindUninstallString()
        {
            try
            {
                // Проверяем реестр в HKEY_LOCAL_MACHINE
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
                {
                    if (key != null)
                    {
                        foreach (string subKeyName in key.GetSubKeyNames())
                        {
                            using (RegistryKey subKey = key.OpenSubKey(subKeyName))
                            {
                                string displayName = subKey?.GetValue("DisplayName") as string;
                                if (displayName != null && displayName.Contains("StartAllBack"))
                                {
                                    string uninstallString = subKey.GetValue("UninstallString") as string;
                                    if (!string.IsNullOrEmpty(uninstallString))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Найдена команда удаления в реестре: {uninstallString}");
                                        return uninstallString;
                                    }
                                }
                            }
                        }
                    }
                }

                // Проверяем реестр в HKEY_CURRENT_USER
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall"))
                {
                    if (key != null)
                    {
                        foreach (string subKeyName in key.GetSubKeyNames())
                        {
                            using (RegistryKey subKey = key.OpenSubKey(subKeyName))
                            {
                                string displayName = subKey?.GetValue("DisplayName") as string;
                                if (displayName != null && displayName.Contains("StartAllBack"))
                                {
                                    string uninstallString = subKey.GetValue("UninstallString") as string;
                                    if (!string.IsNullOrEmpty(uninstallString))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Найдена команда удаления в реестре: {uninstallString}");
                                        return uninstallString;
                                    }
                                }
                            }
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при поиске команды удаления в реестре: {ex.Message}");
                return null;
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

                // Загрузка состояния StartAllBack
                isStartAllBackInstalled = File.Exists(StartAllBackExePath);
                InstallStartAllBackButton.Content = isStartAllBackInstalled
                    ? "Удалить StartAllBack"
                    : "Установить StartAllBack";

                System.Diagnostics.Debug.WriteLine("Текущие настройки успешно загружены.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки настроек: {ex.Message}");
            }
        }
    }
}