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
            this.InitializeComponent();
            LoadCurrentSettings();
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Начало применения настроек...");

                // Создаём содержимое .reg файла
                string regContent = "Windows Registry Editor Version 5.00\n\n";

                // 1. Показывать расширения файлов
                bool showFileExtensions = ShowFileExtensions.IsChecked == true;
                regContent += $"[HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced]\n\"HideFileExt\"=dword:{(showFileExtensions ? "0" : "1")}\n\n";

                // 2. Показывать скрытые файлы
                bool showHiddenFiles = ShowHiddenFiles.IsChecked == true;
                regContent += $"[HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced]\n\"Hidden\"=dword:{(showHiddenFiles ? "00000001" : "00000000")}\n\"ShowSuperHidden\"=dword:{(showHiddenFiles ? "00000001" : "00000000")}\n\n";

                // 3. Показывать полный путь в заголовке
                bool showFullPath = ShowFullPath.IsChecked == true;
                regContent += $"[HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\CabinetState]\n\"FullPath\"=dword:{(showFullPath ? "00000001" : "00000000")}\n\n";

                // 4. Скрыть "Рекомендуемые" и "Недавние файлы"
                bool hideRecommendedAndRecent = HideRecommendedAndRecent.IsChecked == true;
                regContent += $"[HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer]\n\"ShowRecent\"=dword:{(hideRecommendedAndRecent ? "00000000" : "00000001")}\n\"ShowFrequent\"=dword:{(hideRecommendedAndRecent ? "00000000" : "00000001")}\n\n";

                // 5. Использовать классическое контекстное меню
                if (UseClassicContextMenu.IsChecked == true)
                {
                    regContent += "[HKEY_CURRENT_USER\\Software\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\\InprocServer32]\n@=\"\"\n\n";
                }
                else
                {
                    regContent += "[-HKEY_CURRENT_USER\\Software\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}]\n\n";
                }

                // 6. Скрыть папки из "Этот компьютер"
                if (CustomizeThisPC.IsChecked == true)
                {
                    string[] thisPCFolders = {
                        "{088e3905-0323-4b02-9826-5d99428e115f}", // 3D Objects
                        "{1CF1260C-4DD0-4ebb-811F-33C572699FDE}", // Music
                        "{3dfdf296-dbec-4fb4-81d1-6a3438bcf4de}", // Pictures
                        "{24ad3ad4-a569-4530-98e1-ab02f9417aa8}", // Videos
                        "{d3162b92-9360-467a-956b-92703aca08af}", // Documents
                        "{f86fa3ab-70d2-4fc7-9c99-fcbf05467f3a}"  // Downloads
                    };
                    foreach (var folder in thisPCFolders)
                    {
                        regContent += $"[-HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MyComputer\\NameSpace\\{folder}]\n";
                    }
                    regContent += "\n";
                }

                // 7. Скрыть значки быстрого доступа
                bool hideQuickAccessIcons = HideQuickAccessIcons.IsChecked == true;
                regContent += $"[HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer]\n\"HubMode\"=dword:{(hideQuickAccessIcons ? "00000001" : "00000000")}\n\n";

                // 8. Отключить анимации в проводнике
                bool disableAnimations = DisableAnimations.IsChecked == true;
                regContent += $"[HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced]\n\"ListviewAlphaSelect\"=dword:{(disableAnimations ? "00000000" : "00000001")}\n\n";

                // 9. Отключить миниатюры
                bool disableThumbnails = DisableThumbnails.IsChecked == true;
                regContent += $"[HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced]\n\"IconsOnly\"=dword:{(disableThumbnails ? "00000001" : "00000000")}\n\n";

                // Сохраняем .reg файл с кодировкой UTF-16 LE
                string tempRegPath = Path.Combine(Path.GetTempPath(), "PWin11Tweaker.reg");
                File.WriteAllText(tempRegPath, regContent, Encoding.Unicode);
                System.Diagnostics.Debug.WriteLine($"Создан .reg файл: {tempRegPath}");

                // Создаём .bat файл (без gpupdate, так как он не нужен для HKEY_CURRENT_USER)
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

                #region Запускаем .bat с правами администратора
                ProcessStartInfo batProcess = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C \"{tempBatPath}\"",
                    UseShellExecute = true,
                    Verb = "runas", // Запуск от имени администратора
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
                #endregion

                #region Очищаем временные файлы
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
                #endregion

                #region Перезапускаем проводник

                if (success)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("Перезапускаем проводник...");
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "taskkill",
                            Arguments = "/f /im explorer.exe",
                            UseShellExecute = true,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        }).WaitForExit(2000);

                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            UseShellExecute = true,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        });

                        System.Diagnostics.Debug.WriteLine("Проводник перезапущен!");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка при перезапуске проводника: {ex.Message}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Не удалось применить настройки. Проверьте лог: " + tempLogPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в коде: {ex.Message}");
            }
        }
        #endregion

                #region Открытья OldNewExplorer
        private void OpenOldNewExplorer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string appPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "OldNewExplorer", "OldNewExplorer.exe");
                if (File.Exists(appPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = appPath,
                        UseShellExecute = true
                    });
                    System.Diagnostics.Debug.WriteLine("OldNewExplorer запущен!");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Файл OldNewExplorer.exe не найден!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка запуска OldNewExplorer: {ex.Message}");
            }
        }
        #endregion

                #region Загрузка конкретных настроек 
        private void LoadCurrentSettings()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                {
                    if (key != null)
                    {
                        ShowFileExtensions.IsChecked = (int?)key.GetValue("HideFileExt", 1) == 0;
                        ShowHiddenFiles.IsChecked = (int?)key.GetValue("Hidden", 0) == 1;
                        DisableAnimations.IsChecked = (int?)key.GetValue("ListviewAlphaSelect", 1) == 0;
                        DisableThumbnails.IsChecked = (int?)key.GetValue("IconsOnly", 0) == 1;
                    }
                }
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\CabinetState"))
                {
                    if (key != null)
                    {
                        ShowFullPath.IsChecked = (int?)key.GetValue("FullPath", 0) == 1;
                    }
                }
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer"))
                {
                    if (key != null)
                    {
                        HideRecommendedAndRecent.IsChecked = (int?)key.GetValue("ShowRecent", 1) == 0 && (int?)key.GetValue("ShowFrequent", 1) == 0;
                        HideQuickAccessIcons.IsChecked = (int?)key.GetValue("HubMode", 0) == 1;
                    }
                }
                UseClassicContextMenu.IsChecked = Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}") != null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки настроек: {ex.Message}");
            }
        }
        #endregion
    }
}