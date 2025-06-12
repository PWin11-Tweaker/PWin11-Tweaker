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
using Microsoft.Windows.ApplicationModel.Resources;

namespace PWin11_Tweaker_s
{
    public sealed partial class ExplorerPage : Page
    {
        private const string StartAllBackUrl = "https://www.startallback.com/download.php";
        private string StartAllBackExePath = string.Empty;
        private bool isStartAllBackInstalled;
        private readonly ResourceLoader resourceLoader;

        public ExplorerPage()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("ExplorerPage: Начало инициализации...");
                this.InitializeComponent();
                System.Diagnostics.Debug.WriteLine("ExplorerPage: InitializeComponent завершён.");
                resourceLoader = new ResourceLoader();
                StartAllBackExePath = FindStartAllBackPath();
                System.Diagnostics.Debug.WriteLine($"ExplorerPage: Путь к StartAllBack: {StartAllBackExePath}");

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

        private string FindStartAllBackPath()
        {
            try
            {
                string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string userPath = Path.Combine(localAppDataPath, @"StartAllBack\StartAllBackCfg.exe");
                if (File.Exists(userPath))
                {
                    System.Diagnostics.Debug.WriteLine($"FindStartAllBackPath: Найден StartAllBack по пути: {userPath}");
                    return userPath;
                }

                string[] possiblePaths = new[]
                {
                    @"C:\Program Files\StartAllBack\StartAllBackCfg.exe",
                    @"C:\Program Files (x86)\StartAllBack\StartAllBackCfg.exe"
                };

                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        System.Diagnostics.Debug.WriteLine($"FindStartAllBackPath: Найден StartAllBack по пути: {path}");
                        return path;
                    }
                }

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
                                    string? installLocation = subKey?.GetValue("InstallLocation") as string;
                                    if (!string.IsNullOrEmpty(installLocation))
                                    {
                                        string possiblePath = Path.Combine(installLocation, "StartAllBackCfg.exe");
                                        if (File.Exists(possiblePath))
                                        {
                                            System.Diagnostics.Debug.WriteLine($"FindStartAllBackPath: Найден StartAllBack через реестр: {possiblePath}");
                                            return possiblePath;
                                        }
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
                                    string? installLocation = subKey?.GetValue("InstallLocation") as string;
                                    if (!string.IsNullOrEmpty(installLocation))
                                    {
                                        string possiblePath = Path.Combine(installLocation, "StartAllBackCfg.exe");
                                        if (File.Exists(possiblePath))
                                        {
                                            System.Diagnostics.Debug.WriteLine($"FindStartAllBackPath: Найден StartAllBack через реестр (HKCU): {possiblePath}");
                                            return possiblePath;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine("FindStartAllBackPath: StartAllBack не найден. Используем путь по умолчанию.");
                return Path.Combine(localAppDataPath, @"StartAllBack\StartAllBackCfg.exe");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FindStartAllBackPath: Ошибка: {ex.Message}\nStackTrace: {ex.StackTrace}");
                string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(localAppDataPath, @"StartAllBack\StartAllBackCfg.exe");
            }
        }

        private bool CheckHomeFolderDisabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID\{f874310e-b6b7-47dc-bc84-b9e6b38f5903}");
                if (key != null)
                {
                    return (int?)key.GetValue("System.IsPinnedToNameSpaceTree", 1) == 0;
                }
                return false; // Если ключа нет, папка считается включённой
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckHomeFolderDisabled: Ошибка: {ex.Message}");
                return false;
            }
        }

        private bool CheckGalleryFolderDisabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}");
                if (key != null)
                {
                    return (int?)key.GetValue("System.IsPinnedToNameSpaceTree", 1) == 0;
                }
                return false; // Если ключа нет, папка считается включённой
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckGalleryFolderDisabled: Ошибка: {ex.Message}");
                return false;
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
                StatusText.Text = resourceLoader.GetString("StatusText_Preparing");
                ProgressBar.Value = 0;
                await Task.Delay(100);

                if (isStartAllBackInstalled)
                {
                    System.Diagnostics.Debug.WriteLine("InstallStartAllBackButton_Click: Удаление StartAllBack...");
                    await UninstallStartAllBack();
                    StartAllBackExePath = FindStartAllBackPath();
                    isStartAllBackInstalled = File.Exists(StartAllBackExePath);
                    TweakStatus.IsStartAllBackInstalled = isStartAllBackInstalled;
                    System.Diagnostics.Debug.WriteLine($"InstallStartAllBackButton_Click: После удаления StartAllBack установлен: {isStartAllBackInstalled}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("InstallStartAllBackButton_Click: Установка StartAllBack...");
                    await DownloadAndInstallStartAllBack();
                    StartAllBackExePath = FindStartAllBackPath();
                    isStartAllBackInstalled = File.Exists(StartAllBackExePath);
                    TweakStatus.IsStartAllBackInstalled = isStartAllBackInstalled;
                    System.Diagnostics.Debug.WriteLine($"InstallStartAllBackButton_Click: После установки StartAllBack установлен: {isStartAllBackInstalled}");
                }

                InstallStartAllBackButton.Content = isStartAllBackInstalled
                    ? resourceLoader.GetString("Content_Button_StartAllBack_Uninstall")
                    : resourceLoader.GetString("Content_Button_StartAllBack");
                System.Diagnostics.Debug.WriteLine($"InstallStartAllBackButton_Click: Текст кнопки обновлён: {InstallStartAllBackButton.Content}");
                System.Diagnostics.Debug.WriteLine("InstallStartAllBackButton_Click: Завершено успешно.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InstallStartAllBackButton_Click: Ошибка: {ex.Message}\nStackTrace: {ex.StackTrace}");
                ContentDialog errorDialog = new()
                {
                    Title = resourceLoader.GetString("Dialog_Error_Title"),
                    Content = $"{resourceLoader.GetString("Dialog_Error_Content")}: {ex.Message}",
                    CloseButtonText = resourceLoader.GetString("Dialog_CloseButton"),
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
                StatusText.Text = resourceLoader.GetString("StatusText_Preparing");
                ProgressBar.Value = 0;
                await Task.Delay(100);

                string regContent = "Windows Registry Editor Version 5.00\n\n";

                // Отключение папки "Главное"
                bool disableHomeFolder = DisableHomeFolder.IsChecked ?? false;
                if (disableHomeFolder)
                {
                    regContent += $"[HKEY_CURRENT_USER\\Software\\Classes\\CLSID\\{{f874310e-b6b7-47dc-bc84-b9e6b38f5903}}]\n" +
                                  "\"System.IsPinnedToNameSpaceTree\"=dword:00000000\n\n";
                }
                else
                {
                    regContent += $"[-HKEY_CURRENT_USER\\Software\\Classes\\CLSID\\{{f874310e-b6b7-47dc-bc84-b9e6b38f5903}}]\n\n";
                }
                TweakStatus.IsHomeFolderDisabled = disableHomeFolder;

                // Отключение папки "Галерея"
                bool disableGalleryFolder = DisableGalleryFolder.IsChecked ?? false;
                if (disableGalleryFolder)
                {
                    regContent += $"[HKEY_CURRENT_USER\\Software\\Classes\\CLSID\\{{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}}]\n" +
                                  "\"System.IsPinnedToNameSpaceTree\"=dword:00000000\n\n";
                }
                else
                {
                    regContent += $"[-HKEY_CURRENT_USER\\Software\\Classes\\CLSID\\{{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}}]\n\n";
                }
                TweakStatus.IsGalleryFolderDisabled = disableGalleryFolder;

                // Включить показ скрытых файлов 
                bool showHiddenFiles = ShowHiddenFiles.IsChecked ?? false;
                regContent += $"[HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced]\n" +
                              $"\"Hidden\"=dword:{(showHiddenFiles ? "00000001" : "00000000")}\n" +
                              $"\"ShowSuperHidden\"=dword:{(showHiddenFiles ? "00000001" : "00000000")}\n\n";
                TweakStatus.IsShowHiddenFilesEnabled = showHiddenFiles;

                bool useSmallCaptions = UseSmallCaptions.IsChecked ?? false;
                string captionHeightValue = useSmallCaptions ? "-180" : "-330";
                regContent += $"[HKEY_CURRENT_USER\\Control Panel\\Desktop\\WindowMetrics]\n" +
                              $"\"CaptionHeight\"=\"{captionHeightValue}\"\n\n";
                TweakStatus.IsSmallCaptionsEnabled = useSmallCaptions;

                // Инвертированный скролл через ScrollDirection
                bool invertScroll = InvertScrollToggle.IsChecked ?? false;
                regContent += @"[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\PointerIntegration]" + "\n" +
                              $"\"ScrollDirection\"=REG_SZ:{(invertScroll ? "1" : "0")}\n\n";

                // Уведомление о перезагрузке (добавляем в диалог успеха)
                if (invertScroll)
                {
                    TweakStatus.IsInvertScrollEnabled = true;
                    regContent += "; Перезапуск Проводника рекомендуется для применения инвертированного скролла\n";
                }
                else
                {
                    TweakStatus.IsInvertScrollEnabled = false;
                }

                bool applyClassicContextMenu = ClassicContextMenuToggle.IsChecked ?? false;
                if (applyClassicContextMenu)
                {
                    regContent += $"[HKEY_CURRENT_USER\\Software\\Classes\\CLSID\\{{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}}\\InprocServer32]\n" +
                          $"@=\"\"\n\n";
                }
                else
                {
                    regContent += $"[-HKEY_CURRENT_USER\\Software\\Classes\\CLSID\\{{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}}]\n\n";
                }
                TweakStatus.IsClassicContextMenuEnabled = applyClassicContextMenu;

                StatusText.Text = resourceLoader.GetString("StatusText_SavingRegistryChanges");
                ProgressBar.Value = 90;
                await Task.Delay(100);

                string tempRegPath = Path.Combine(Path.GetTempPath(), "PWin11Tweaker.reg");
                System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Планируется запись .reg файла в: {tempRegPath}");

                // Проверка прав доступа и создание файла
                try
                {
                    if (!Directory.Exists(Path.GetTempPath()))
                    {
                        Directory.CreateDirectory(Path.GetTempPath());
                        System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Временная папка создана: {Path.GetTempPath()}");
                    }

                    // Проверка возможности записи
                    using (FileStream fs = File.Create(tempRegPath, 4096, FileOptions.DeleteOnClose))
                    {
                        byte[] contentBytes = Encoding.Unicode.GetBytes(regContent);
                        fs.Write(contentBytes, 0, contentBytes.Length);
                        fs.Flush();
                    }
                    System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: .reg файл успешно создан и записан: {tempRegPath}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Нет прав доступа: {ex.Message}");
                    throw new Exception($"Нет прав для записи в {tempRegPath}. Запустите приложение от имени администратора.");
                }
                catch (IOException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Ошибка ввода-вывода: {ex.Message}");
                    throw new Exception($"Не удалось создать файл {tempRegPath}. Проверьте доступ к папке.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Общая ошибка при создании файла: {ex.Message}");
                    throw;
                }

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

                StatusText.Text = resourceLoader.GetString("StatusText_ApplyingRegistryChanges");
                ProgressBar.Value = 95;
                await Task.Delay(100);
                ProcessStartInfo batProcess = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C \"{tempBatPath}\"",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Verb = "runas" // Запуск с повышенными привилегиями
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
                            System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Лог выполнения: {logContent}");
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
                        StatusText.Text = resourceLoader.GetString("StatusText_RestartingExplorer");
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
                        using (Process? taskKillProcess = Process.Start(taskKillInfo))
                        {
                            taskKillProcess?.WaitForExit(2000);
                            System.Diagnostics.Debug.WriteLine("ApplyButton_Click: Процесс explorer.exe успешно завершён.");
                        }

                        ProcessStartInfo explorerInfo = new()
                        {
                            FileName = "explorer.exe",
                            UseShellExecute = true,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        };
                        using (Process? explorerProcess = Process.Start(explorerInfo))
                        {
                            System.Diagnostics.Debug.WriteLine("ApplyButton_Click: Проводник успешно запущен заново.");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Ошибка при перезапуске Проводника: {ex.Message}");
                    }

                    // Уведомление о необходимости перезагрузки, если включён инвертированный скролл
                    string successMessage = resourceLoader.GetString("Dialog_Success_Content");
                    if (invertScroll)
                    {
                        successMessage += "\n" + resourceLoader.GetString("Dialog_RestartRequired_Content");
                    }

                    ContentDialog successDialog = new()
                    {
                        Title = resourceLoader.GetString("Dialog_Success_Title"),
                        Content = successMessage,
                        CloseButtonText = resourceLoader.GetString("Dialog_CloseButton"),
                        XamlRoot = this.XamlRoot
                    };
                    await successDialog.ShowAsync();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Не удалось применить настройки. Проверьте лог: {tempLogPath}");
                    ContentDialog errorDialog = new()
                    {
                        Title = resourceLoader.GetString("Dialog_Error_Title"),
                        Content = $"{resourceLoader.GetString("Dialog_FailedApplySettings_Content")} {tempLogPath}",
                        CloseButtonText = resourceLoader.GetString("Dialog_CloseButton"),
                        XamlRoot = this.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Общая ошибка: {ex.Message} StackTrace: {ex.StackTrace}");
                ContentDialog errorDialog = new()
                {
                    Title = resourceLoader.GetString("Dialog_Error_Title"),
                    Content = $"{resourceLoader.GetString("Dialog_Error_Content")}: {ex.Message}",
                    CloseButtonText = resourceLoader.GetString("Dialog_CloseButton"),
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

        private async void OpenOldNewExplorerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string oldNewExplorerPath = Path.Combine(AppContext.BaseDirectory, "Assets", "OldNewExplorer", "OldNewExplorer.exe");
                if (File.Exists(oldNewExplorerPath))
                {
                    ProcessStartInfo processInfo = new ProcessStartInfo
                    {
                        FileName = oldNewExplorerPath,
                        UseShellExecute = true,
                        CreateNoWindow = false
                    };
                    Process.Start(processInfo);
                    System.Diagnostics.Debug.WriteLine($"OpenOldNewExplorerButton_Click: OldNewExplorer запущен по пути: {oldNewExplorerPath}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"OpenOldNewExplorerButton_Click: Файл OldNewExplorer.exe не найден по пути: {oldNewExplorerPath}");
                    ContentDialog errorDialog = new()
                    {
                        Title = resourceLoader.GetString("Dialog_Error_Title"),
                        Content = resourceLoader.GetString("Dialog_OldNewExplorerNotFound_Content"),
                        CloseButtonText = resourceLoader.GetString("Dialog_CloseButton"),
                        XamlRoot = this.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OpenOldNewExplorerButton_Click: Ошибка: {ex.Message}\nStackTrace: {ex.StackTrace}");
                ContentDialog errorDialog = new()
                {
                    Title = resourceLoader.GetString("Dialog_Error_Title"),
                    Content = $"{resourceLoader.GetString("Dialog_Error_Content")}: {ex.Message}",
                    CloseButtonText = resourceLoader.GetString("Dialog_CloseButton"),
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }

        private async Task DownloadAndInstallStartAllBack()
        {
            bool installationSuccessful = false;
            try
            {
                System.Diagnostics.Debug.WriteLine("DownloadAndInstallStartAllBack: Начало выполнения...");
                StatusText.Text = resourceLoader.GetString("StatusText_DownloadingStartAllBack");
                ProgressBar.Value = 10;
                await Task.Delay(100);
                string tempInstallerPath = Path.Combine(Path.GetTempPath(), "StartAllBackSetup.exe");
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                    System.Diagnostics.Debug.WriteLine($"DownloadAndInstallStartAllBack: Скачивание с URL: {StartAllBackUrl}");
                    var response = await client.GetAsync(StartAllBackUrl);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"{resourceLoader.GetString("Error_DownloadFailed")}: {response.StatusCode}");
                    }
                    using (var fs = new FileStream(tempInstallerPath, FileMode.Create, FileAccess.Write))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                }
                System.Diagnostics.Debug.WriteLine($"DownloadAndInstallStartAllBack: StartAllBack успешно скачан: {tempInstallerPath}");
                ProgressBar.Value = 40;
                await Task.Delay(100);

                StatusText.Text = resourceLoader.GetString("StatusText_InstallingStartAllBack");
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
                            installationSuccessful = true;
                            ProgressBar.Value = 70;
                            await Task.Delay(100);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"DownloadAndInstallStartAllBack: Ошибка установки StartAllBack, код: {process.ExitCode}");
                            throw new Exception($"{resourceLoader.GetString("Error_InstallFailed")}: {process.ExitCode}");
                        }
                    }
                    else
                    {
                        throw new Exception(resourceLoader.GetString("Error_InstallProcessFailed"));
                    }
                }

                StatusText.Text = resourceLoader.GetString("StatusText_CleaningTempFiles");
                if (File.Exists(tempInstallerPath))
                {
                    File.Delete(tempInstallerPath);
                    System.Diagnostics.Debug.WriteLine("DownloadAndInstallStartAllBack: Установочный файл StartAllBack удалён.");
                }
                ProgressBar.Value = 80;
                await Task.Delay(100);

                StatusText.Text = resourceLoader.GetString("StatusText_ApplyingStartAllBackSettings");
                StartAllBackExePath = FindStartAllBackPath();
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
                else
                {
                    System.Diagnostics.Debug.WriteLine($"DownloadAndInstallStartAllBack: Файл {StartAllBackExePath} не найден после установки.");
                    installationSuccessful = false;
                }
                ProgressBar.Value = 90;
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DownloadAndInstallStartAllBack: Ошибка: {ex.Message}\nStackTrace: {ex.StackTrace}");
                installationSuccessful = false;
                throw;
            }
            finally
            {
                StartAllBackExePath = FindStartAllBackPath();
                bool isInstalled = File.Exists(StartAllBackExePath);
                System.Diagnostics.Debug.WriteLine($"DownloadAndInstallStartAllBack: Проверка после установки: StartAllBack установлен: {isInstalled}");
                if (!isInstalled)
                {
                    installationSuccessful = false;
                }
                isStartAllBackInstalled = installationSuccessful;
                TweakStatus.IsStartAllBackInstalled = installationSuccessful;
            }
        }

        private async Task UninstallStartAllBack()
        {
            bool uninstallSuccessful = false;
            try
            {
                System.Diagnostics.Debug.WriteLine("UninstallStartAllBack: Начало выполнения...");
                StatusText.Text = resourceLoader.GetString("StatusText_TerminatingStartAllBack");
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

                StatusText.Text = resourceLoader.GetString("StatusText_UninstallingStartAllBack");
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
                    StatusText.Text = resourceLoader.GetString("StatusText_SearchingUninstallString");
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

                StatusText.Text = resourceLoader.GetString("StatusText_CleaningRemainingFiles");
                string startAllBackFolder = Path.GetDirectoryName(StartAllBackExePath) ?? string.Empty;
                if (Directory.Exists(startAllBackFolder))
                {
                    try
                    {
                        Directory.Delete(startAllBackFolder, true);
                        System.Diagnostics.Debug.WriteLine("UninstallStartAllBack: Папка StartAllBack удалена.");
                        uninstallSuccessful = true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"UninstallStartAllBack: Ошибка при удалении папки StartAllBack: {ex.Message}");
                        await Task.Delay(2000);
                        try
                        {
                            Directory.Delete(startAllBackFolder, true);
                            System.Diagnostics.Debug.WriteLine("UninstallStartAllBack: Папка StartAllBack удалена после повторной попытки.");
                            uninstallSuccessful = true;
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
                    uninstallSuccessful = true;
                }
                ProgressBar.Value = 80;
                await Task.Delay(100);

                StatusText.Text = resourceLoader.GetString("StatusText_UpdatingUI");
                ProgressBar.Value = 90;
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UninstallStartAllBack: Ошибка: {ex.Message} StackTrace: {ex.StackTrace}");
                uninstallSuccessful = false;
                throw;
            }
            finally
            {
                StartAllBackExePath = FindStartAllBackPath();
                bool isInstalled = File.Exists(StartAllBackExePath);
                System.Diagnostics.Debug.WriteLine($"UninstallStartAllBack: Проверка после удаления: StartAllBack установлен: {isInstalled}");
                if (isInstalled)
                {
                    uninstallSuccessful = false;
                }
                isStartAllBackInstalled = !uninstallSuccessful;
                TweakStatus.IsStartAllBackInstalled = !uninstallSuccessful;
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
                                        System.Diagnostics.Debug.WriteLine($"FindUninstallString: Найдена команда удаления в реестре (HKCU): {uninstallString}");
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
                System.Diagnostics.Debug.WriteLine($"FindUninstallString: Ошибка: {ex.Message} StackTrace: {ex.StackTrace}");
                return null;
            }
        }

        private void LoadCurrentSettings()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("LoadCurrentSettings: Начало загрузки настроек из TweakStatus...");

                ShowHiddenFiles.IsChecked = TweakStatus.IsShowHiddenFilesEnabled;
                System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: ShowHiddenFiles установлен в {TweakStatus.IsShowHiddenFilesEnabled}");

                UseSmallCaptions.IsChecked = TweakStatus.IsSmallCaptionsEnabled;
                System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: UseSmallCaptions установлен в {TweakStatus.IsSmallCaptionsEnabled}");

                ClassicContextMenuToggle.IsChecked = TweakStatus.IsClassicContextMenuEnabled;
                System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: ClassicContextMenuToggle установлен в {TweakStatus.IsClassicContextMenuEnabled}");

                StartAllBackExePath = FindStartAllBackPath();
                isStartAllBackInstalled = File.Exists(StartAllBackExePath);
                TweakStatus.IsStartAllBackInstalled = isStartAllBackInstalled;
                InstallStartAllBackButton.Content = isStartAllBackInstalled
                    ? resourceLoader.GetString("Content_Button_StartAllBack_Uninstall")
                    : resourceLoader.GetString("Content_Button_StartAllBack");
                System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: StartAllBack установлен: {isStartAllBackInstalled} (проверка через File.Exists)");
                System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: Путь к StartAllBack: {StartAllBackExePath}");
                System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: Текст кнопки: {InstallStartAllBackButton.Content}");

                System.Diagnostics.Debug.WriteLine("LoadCurrentSettings: Текущие настройки успешно загружены из TweakStatus.");

                // Загрузка состояния для "Главное"
                DisableHomeFolder.IsChecked = CheckHomeFolderDisabled();
                TweakStatus.IsHomeFolderDisabled = DisableHomeFolder.IsChecked ?? false;
                System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: DisableHomeFolder установлен в {TweakStatus.IsHomeFolderDisabled}");

                // Загрузка состояния для "Галерея"
                DisableGalleryFolder.IsChecked = CheckGalleryFolderDisabled();
                TweakStatus.IsGalleryFolderDisabled = DisableGalleryFolder.IsChecked ?? false;
                System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: DisableGalleryFolder установлен в {TweakStatus.IsGalleryFolderDisabled}");

                // Инвертированный скролл через ScrollDirection
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\PointerIntegration"))
                {
                    if (key != null)
                    {
                        string? scrollDirection = key.GetValue("ScrollDirection") as string;
                        InvertScrollToggle.IsChecked = scrollDirection == "1";
                        System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: InvertScrollToggle установлен в {scrollDirection == "1"} (ScrollDirection: {scrollDirection})");
                    }
                    else
                    {
                        InvertScrollToggle.IsChecked = false;
                        System.Diagnostics.Debug.WriteLine("LoadCurrentSettings: Ключ PointerIntegration не найден, InvertScrollToggle установлен в false");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: Ошибка: {ex.Message} StackTrace: {ex.StackTrace}");
                throw;
            }
        }
    }
}