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
        private const string StartAllBackUrl = "https://www.startallback.com/download.php";
        private const string StartAllBackExePath = @"C:\Program Files\StartAllBack\StartAllBackCfg.exe";
        private bool isStartAllBackInstalled;

        public ExplorerPage()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("ExplorerPage: Начало инициализации...");
                this.InitializeComponent();
                System.Diagnostics.Debug.WriteLine("ExplorerPage: InitializeComponent завершён.");
                LoadCurrentSettings();
                System.Diagnostics.Debug.WriteLine("ExplorerPage: LoadCurrentSettings завершён.");
                System.Diagnostics.Debug.WriteLine("ExplorerPage успешно инициализирован.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ExplorerPage: Ошибка при инициализации: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private async void InstallStartAllBackButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("InstallStartAllBackButton_Click: Начало выполнения...");
                ProgressPanel.Visibility = Visibility.Visible;
                ApplyButton.IsEnabled = false;
                InstallStartAllBackButton.IsEnabled = false;
                StatusText.Text = "Подготовка...";
                ProgressBar.Value = 0;
                await Task.Delay(100);

                if (isStartAllBackInstalled)
                {
                    System.Diagnostics.Debug.WriteLine("InstallStartAllBackButton_Click: Удаление StartAllBack...");
                    await UninstallStartAllBack();
                    isStartAllBackInstalled = false;
                    InstallStartAllBackButton.Content = "Установить StartAllBack";
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("InstallStartAllBackButton_Click: Установка StartAllBack...");
                    await DownloadAndInstallStartAllBack();
                    isStartAllBackInstalled = true;
                    InstallStartAllBackButton.Content = "Удалить StartAllBack";
                }
                System.Diagnostics.Debug.WriteLine("InstallStartAllBackButton_Click: Завершено успешно.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InstallStartAllBackButton_Click: Ошибка: {ex.Message}\nStackTrace: {ex.StackTrace}");
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
                ProgressPanel.Visibility = Visibility.Collapsed;
                ApplyButton.IsEnabled = true;
                InstallStartAllBackButton.IsEnabled = true;
            }
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("ApplyButton_Click: Начало применения настроек...");
                ProgressPanel.Visibility = Visibility.Visible;
                ApplyButton.IsEnabled = false;
                InstallStartAllBackButton.IsEnabled = false;
                StatusText.Text = "Подготовка...";
                ProgressBar.Value = 0;
                await Task.Delay(100);

                string regContent = "Windows Registry Editor Version 5.00\n\n";

                // Твик: Показывать скрытые файлы
                bool showHiddenFiles = ShowHiddenFiles.IsChecked ?? false; // Используем ?? для значения по умолчанию
                regContent += $"[HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced]\n" +
                              $"\"Hidden\"=dword:{(showHiddenFiles ? "00000001" : "00000000")}\n" +
                              $"\"ShowSuperHidden\"=dword:{(showHiddenFiles ? "00000001" : "00000000")}\n\n";

                // Твик: Уменьшение кнопок Закрыть/Свернуть/Развернуть
                bool useSmallCaptions = UseSmallCaptions.IsChecked ?? false; // Используем ?? для значения по умолчанию
                string captionHeightValue = useSmallCaptions ? "-180" : "-330";
                regContent += $"[HKEY_CURRENT_USER\\Control Panel\\Desktop\\WindowMetrics]\n" +
                              $"\"CaptionHeight\"=\"{captionHeightValue}\"\n\n";

                StatusText.Text = "Сохранение изменений в реестре...";
                ProgressBar.Value = 90;
                await Task.Delay(100);
                string tempRegPath = Path.Combine(Path.GetTempPath(), "PWin11Tweaker.reg");
                File.WriteAllText(tempRegPath, regContent, Encoding.Unicode);
                System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Создан .reg файл: {tempRegPath}");

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
                System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Создан .bat файл: {tempBatPath}");

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
                using (Process? process = Process.Start(batProcess))
                {
                    if (process != null)
                    {
                        process.WaitForExit(5000);
                        if (process.ExitCode == 0)
                        {
                            System.Diagnostics.Debug.WriteLine("ApplyButton_Click: Настройки успешно применены!");
                            success = true;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Произошла ошибка при выполнении .bat, код: {process.ExitCode}. Проверь лог: {tempLogPath}");
                            success = false;
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("ApplyButton_Click: Не удалось запустить процесс .bat.");
                        success = false;
                    }

                    if (File.Exists(tempLogPath))
                    {
                        try
                        {
                            string logContent = File.ReadAllText(tempLogPath);
                            System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Лог выполнения:\n{logContent}");
                        }
                        catch (IOException ioEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Не удалось прочитать лог: {ioEx.Message}. Продолжаем...");
                        }
                    }
                }

                try
                {
                    if (File.Exists(tempRegPath)) File.Delete(tempRegPath);
                    if (File.Exists(tempBatPath)) File.Delete(tempBatPath);
                    if (File.Exists(tempLogPath)) File.Delete(tempLogPath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Ошибка при удалении временных файлов: {ex.Message}");
                }

                if (success)
                {
                    try
                    {
                        StatusText.Text = "Перезапуск Проводника...";
                        ProgressBar.Value = 100;
                        await Task.Delay(100);
                        System.Diagnostics.Debug.WriteLine("ApplyButton_Click: Перезапускаем Проводник...");
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
                            System.Diagnostics.Debug.WriteLine("ApplyButton_Click: Процесс explorer.exe успешно завершён.");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("ApplyButton_Click: Ошибка: Не удалось запустить taskkill для завершения explorer.exe.");
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
                            System.Diagnostics.Debug.WriteLine("ApplyButton_Click: Проводник успешно запущен заново.");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("ApplyButton_Click: Ошибка: Не удалось запустить explorer.exe.");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Ошибка при перезапуске Проводника: {ex.Message}");
                    }

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
                    System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Не удалось применить настройки. Проверьте лог: {tempLogPath}");
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
                System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Общая ошибка: {ex.Message}\nStackTrace: {ex.StackTrace}");
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
                ProgressPanel.Visibility = Visibility.Collapsed;
                ApplyButton.IsEnabled = true;
                InstallStartAllBackButton.IsEnabled = true;
            }
        }

        private async Task DownloadAndInstallStartAllBack()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("DownloadAndInstallStartAllBack: Начало выполнения...");
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
                System.Diagnostics.Debug.WriteLine($"DownloadAndInstallStartAllBack: StartAllBack успешно скачан: {tempInstallerPath}");
                ProgressBar.Value = 40;
                await Task.Delay(100);

                StatusText.Text = "Установка StartAllBack...";
                ProcessStartInfo installProcess = new ProcessStartInfo
                {
                    FileName = tempInstallerPath,
                    Arguments = "/silent",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (Process? process = Process.Start(installProcess))
                {
                    if (process != null)
                    {
                        process.WaitForExit(30000);
                        if (process.ExitCode == 0)
                        {
                            System.Diagnostics.Debug.WriteLine("DownloadAndInstallStartAllBack: StartAllBack успешно установлен.");
                            ProgressBar.Value = 70;
                            await Task.Delay(100);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"DownloadAndInstallStartAllBack: Ошибка установки StartAllBack, код: {process.ExitCode}");
                            throw new Exception("Не удалось установить StartAllBack.");
                        }
                    }
                    else
                    {
                        throw new Exception("Не удалось запустить процесс установки StartAllBack.");
                    }
                }

                StatusText.Text = "Очистка временных файлов...";
                if (File.Exists(tempInstallerPath))
                {
                    File.Delete(tempInstallerPath);
                    System.Diagnostics.Debug.WriteLine("DownloadAndInstallStartAllBack: Установочный файл StartAllBack удалён.");
                }
                ProgressBar.Value = 80;
                await Task.Delay(100);

                StatusText.Text = "Применение настроек StartAllBack...";
                if (File.Exists(StartAllBackExePath))
                {
                    ProcessStartInfo configProcess = new ProcessStartInfo
                    {
                        FileName = StartAllBackExePath,
                        Arguments = "--apply-style Remastered7",
                        UseShellExecute = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    using (Process? configProc = Process.Start(configProcess))
                    {
                        if (configProc != null)
                        {
                            configProc.WaitForExit(5000);
                            System.Diagnostics.Debug.WriteLine("DownloadAndInstallStartAllBack: Настройки StartAllBack применены.");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("DownloadAndInstallStartAllBack: Не удалось запустить процесс применения настроек StartAllBack.");
                        }
                    }
                }
                ProgressBar.Value = 90;
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DownloadAndInstallStartAllBack: Ошибка: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private async Task UninstallStartAllBack()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("UninstallStartAllBack: Начало выполнения...");
                StatusText.Text = "Завершение процессов StartAllBack...";
                ProgressBar.Value = 10;
                await Task.Delay(100);

                string[] processNames = { "StartAllBackCfg", "StartAllBackX64", "StartAllBack" };
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
                                process.WaitForExit(5000);
                                System.Diagnostics.Debug.WriteLine($"UninstallStartAllBack: Процесс {processName} (PID: {process.Id}) завершён.");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"UninstallStartAllBack: Процесс {processName} не найден.");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"UninstallStartAllBack: Ошибка при завершении процесса {processName}: {ex.Message}");
                    }
                }
                ProgressBar.Value = 30;
                await Task.Delay(100);

                StatusText.Text = "Попытка удаления StartAllBack...";
                bool uninstallSuccess = false;
                if (File.Exists(StartAllBackExePath))
                {
                    ProcessStartInfo uninstallProcess = new ProcessStartInfo
                    {
                        FileName = StartAllBackExePath,
                        Arguments = "/uninstall /silent",
                        UseShellExecute = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    try
                    {
                        using (Process? process = Process.Start(uninstallProcess))
                        {
                            if (process != null)
                            {
                                process.WaitForExit(30000);
                                if (process.ExitCode == 0)
                                {
                                    System.Diagnostics.Debug.WriteLine("UninstallStartAllBack: StartAllBack успешно удалён через команду /uninstall.");
                                    uninstallSuccess = true;
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"UninstallStartAllBack: Ошибка удаления StartAllBack через /uninstall, код: {process.ExitCode}");
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("UninstallStartAllBack: Не удалось запустить процесс удаления StartAllBack.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"UninstallStartAllBack: Ошибка при выполнении команды /uninstall: {ex.Message}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("UninstallStartAllBack: Файл StartAllBackCfg.exe не найден, пропускаем удаление через /uninstall.");
                }
                ProgressBar.Value = 50;
                await Task.Delay(100);

                if (!uninstallSuccess)
                {
                    StatusText.Text = "Поиск команды удаления в реестре...";
                    string? uninstallString = FindUninstallString();
                    if (!string.IsNullOrEmpty(uninstallString))
                    {
                        try
                        {
                            uninstallString = uninstallString.Trim('"');
                            ProcessStartInfo registryUninstallProcess = new ProcessStartInfo
                            {
                                FileName = "cmd.exe",
                                Arguments = $"/C \"{uninstallString}\" /silent",
                                UseShellExecute = true,
                                CreateNoWindow = true,
                                WindowStyle = ProcessWindowStyle.Hidden
                            };

                            using (Process? process = Process.Start(registryUninstallProcess))
                            {
                                if (process != null)
                                {
                                    process.WaitForExit(30000);
                                    if (process.ExitCode == 0)
                                    {
                                        System.Diagnostics.Debug.WriteLine("UninstallStartAllBack: StartAllBack успешно удалён через реестр.");
                                        uninstallSuccess = true;
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine($"UninstallStartAllBack: Ошибка удаления StartAllBack через реестр, код: {process.ExitCode}");
                                    }
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine("UninstallStartAllBack: Не удалось запустить процесс удаления через реестр.");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"UninstallStartAllBack: Ошибка при удалении через реестр: {ex.Message}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("UninstallStartAllBack: Не удалось найти команду удаления в реестре.");
                    }
                }
                ProgressBar.Value = 60;
                await Task.Delay(100);

                StatusText.Text = "Очистка оставшихся файлов...";
                string startAllBackFolder = Path.GetDirectoryName(StartAllBackExePath) ?? string.Empty;
                if (Directory.Exists(startAllBackFolder))
                {
                    try
                    {
                        Directory.Delete(startAllBackFolder, true);
                        System.Diagnostics.Debug.WriteLine("UninstallStartAllBack: Папка StartAllBack удалена.");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"UninstallStartAllBack: Ошибка при удалении папки StartAllBack: {ex.Message}");
                        await Task.Delay(2000);
                        try
                        {
                            Directory.Delete(startAllBackFolder, true);
                            System.Diagnostics.Debug.WriteLine("UninstallStartAllBack: Папка StartAllBack удалена после повторной попытки.");
                        }
                        catch (Exception ex2)
                        {
                            System.Diagnostics.Debug.WriteLine($"UninstallStartAllBack: Повторная ошибка при удалении папки StartAllBack: {ex2.Message}");
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("UninstallStartAllBack: Папка StartAllBack уже отсутствует.");
                }
                ProgressBar.Value = 80;
                await Task.Delay(100);

                StatusText.Text = "Обновление интерфейса...";
                InstallStartAllBackButton.Content = "Установить StartAllBack (минималистичный стиль Windows 7)";
                ProgressBar.Value = 90;
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UninstallStartAllBack: Ошибка: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private string? FindUninstallString()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("FindUninstallString: Начало поиска команды удаления...");
                using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
                {
                    if (key != null)
                    {
                        foreach (string subKeyName in key.GetSubKeyNames())
                        {
                            using (RegistryKey? subKey = key.OpenSubKey(subKeyName))
                            {
                                string? displayName = subKey?.GetValue("DisplayName") as string;
                                if (displayName != null && displayName.Contains("StartAllBack"))
                                {
                                    string? uninstallString = subKey?.GetValue("UninstallString") as string;
                                    if (!string.IsNullOrEmpty(uninstallString))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"FindUninstallString: Найдена команда удаления в реестре: {uninstallString}");
                                        return uninstallString;
                                    }
                                }
                            }
                        }
                    }
                }

                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall"))
                {
                    if (key != null)
                    {
                        foreach (string subKeyName in key.GetSubKeyNames())
                        {
                            using (RegistryKey? subKey = key.OpenSubKey(subKeyName))
                            {
                                string? displayName = subKey?.GetValue("DisplayName") as string;
                                if (displayName != null && displayName.Contains("StartAllBack"))
                                {
                                    string? uninstallString = subKey?.GetValue("UninstallString") as string;
                                    if (!string.IsNullOrEmpty(uninstallString))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"FindUninstallString: Найдена команда удаления в реестре: {uninstallString}");
                                        return uninstallString;
                                    }
                                }
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine("FindUninstallString: Команда удаления не найдена.");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FindUninstallString: Ошибка: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return null;
            }
        }

        private void LoadCurrentSettings()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("LoadCurrentSettings: Начало загрузки настроек...");
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
                if (key != null)
                {
                    ShowHiddenFiles.IsChecked = (int?)key.GetValue("Hidden", 0) == 1;
                    System.Diagnostics.Debug.WriteLine("LoadCurrentSettings: ShowHiddenFiles загружен.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("LoadCurrentSettings: Не удалось открыть ключ реестра для ShowHiddenFiles.");
                    ShowHiddenFiles.IsChecked = false;
                }

                using RegistryKey? windowMetricsKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop\WindowMetrics");
                if (windowMetricsKey != null)
                {
                    string? captionHeight = windowMetricsKey.GetValue("CaptionHeight", "-330") as string;
                    if (int.TryParse(captionHeight, out int height) && height > -330)
                    {
                        UseSmallCaptions.IsChecked = true;
                    }
                    else
                    {
                        UseSmallCaptions.IsChecked = false;
                    }
                    System.Diagnostics.Debug.WriteLine("LoadCurrentSettings: UseSmallCaptions загружен.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("LoadCurrentSettings: Не удалось открыть ключ реестра для UseSmallCaptions.");
                    UseSmallCaptions.IsChecked = false;
                }

                isStartAllBackInstalled = File.Exists(StartAllBackExePath);
                InstallStartAllBackButton.Content = isStartAllBackInstalled
                    ? "Удалить StartAllBack"
                    : "Установить StartAllBack";
                System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: StartAllBack установлен: {isStartAllBackInstalled}");

                System.Diagnostics.Debug.WriteLine("LoadCurrentSettings: Текущие настройки успешно загружены.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: Ошибка: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }
    }
}