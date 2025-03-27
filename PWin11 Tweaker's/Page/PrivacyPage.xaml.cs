using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using PWin11_Tweaker_s.Script;

namespace PWin11_Tweaker_s
{
    public sealed partial class PrivacyPage : Page
    {
        public PrivacyPage()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("PrivacyPage: Начало инициализации...");
                this.InitializeComponent();
                System.Diagnostics.Debug.WriteLine("PrivacyPage: InitializeComponent завершён.");
                LoadCurrentSettings();
                System.Diagnostics.Debug.WriteLine("PrivacyPage: LoadCurrentSettings завершён.");
                System.Diagnostics.Debug.WriteLine("PrivacyPage успешно инициализирован.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PrivacyPage: Ошибка при инициализации: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("ApplyButton_Click: Начало применения настроек...");
                ProgressPanel.Visibility = Visibility.Visible;
                ApplyButton.IsEnabled = false;
                StatusText.Text = "Подготовка...";
                ProgressBar.Value = 0;
                await Task.Delay(100);

                string regContent = "Windows Registry Editor Version 5.00\n\n";

                // Твик 1: Отключение отправки данных о вводе
                bool disableInputDataCollection = DisableInputDataCollection.IsChecked ?? false;
                System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Состояние чекбокса DisableInputDataCollection: {disableInputDataCollection}");
                regContent += $"[HKEY_CURRENT_USER\\Software\\Microsoft\\InputPersonalization]\n" +
                              $"\"RestrictImplicitTextCollection\"=dword:{(disableInputDataCollection ? "00000001" : "00000000")}\n" +
                              $"\"RestrictImplicitInkCollection\"=dword:{(disableInputDataCollection ? "00000001" : "00000000")}\n" +
                              $"\"EnableInkingWithTouch\"=dword:{(disableInputDataCollection ? "00000000" : "00000001")}\n\n";
                TweakStatus.IsInputDataCollectionDisabled = disableInputDataCollection;
                System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Установлено TweakStatus.IsInputDataCollectionDisabled: {TweakStatus.IsInputDataCollectionDisabled}");

                // Твик 2: Отключение слежки через реестр
                bool disableTelemetry = DisableTelemetry.IsChecked ?? false;
                System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Состояние чекбокса DisableTelemetry: {disableTelemetry}");
                if (disableTelemetry)
                {
                    regContent += $"[HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection]\n" +
                                  $"\"AllowTelemetry\"=dword:00000000\n\n";
                }
                else
                {
                    regContent += $"[-HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection]\n\n";
                }
                TweakStatus.IsTelemetryDisabled = disableTelemetry;
                System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Установлено TweakStatus.IsTelemetryDisabled: {TweakStatus.IsTelemetryDisabled}");

                StatusText.Text = "Сохранение изменений в реестре...";
                ProgressBar.Value = 90;
                await Task.Delay(100);
                string tempRegPath = Path.Combine(Path.GetTempPath(), "PWin11TweakerPrivacy.reg");
                File.WriteAllText(tempRegPath, regContent, Encoding.Unicode);
                System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Создан .reg файл: {tempRegPath}");

                string tempBatPath = Path.Combine(Path.GetTempPath(), "PWin11TweakerPrivacyApply.bat");
                string tempLogPath = Path.Combine(Path.GetTempPath(), "PWin11TweakerPrivacyLog.txt");
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

                            // Проверяем значения в реестре после применения
                            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\InputPersonalization"))
                            {
                                if (key != null)
                                {
                                    object? textCollectionValue = key.GetValue("RestrictImplicitTextCollection");
                                    object? inkCollectionValue = key.GetValue("RestrictImplicitInkCollection");
                                    object? enableInkingValue = key.GetValue("EnableInkingWithTouch");
                                    System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: После применения RestrictImplicitTextCollection: {textCollectionValue}");
                                    System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: После применения RestrictImplicitInkCollection: {inkCollectionValue}");
                                    System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: После применения EnableInkingWithTouch: {enableInkingValue}");
                                }
                            }

                            using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection"))
                            {
                                if (key != null)
                                {
                                    object? allowTelemetryValue = key.GetValue("AllowTelemetry");
                                    System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: После применения AllowTelemetry: {allowTelemetryValue}");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine("ApplyButton_Click: Ключ DataCollection не существует после применения (ожидаемо, если твик отключен).");
                                }
                            }
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

                // Проверяем, применились ли твики
                bool inputDataCollectionApplied = VerifyInputDataCollectionTweak(disableInputDataCollection);
                bool telemetryApplied = VerifyTelemetryTweak(disableTelemetry);
                bool allTweaksApplied = inputDataCollectionApplied && telemetryApplied;

                if (success && allTweaksApplied)
                {
                    ContentDialog successDialog = new()
                    {
                        Title = "Успех",
                        Content = "Все настройки приватности успешно применены!\nДля полного применения изменений может потребоваться перезапуск системы.",
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
                        Content = "Не удалось применить настройки.\n" +
                                  $"Отключение отправки данных о вводе: {(inputDataCollectionApplied ? "Применилось" : "Не применилось")}\n" +
                                  $"Отключение слежки: {(telemetryApplied ? "Применилось" : "Не применилось")}\n" +
                                  $"Проверьте лог: {tempLogPath}",
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
            }
        }

        private bool VerifyInputDataCollectionTweak(bool expectedState)
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\InputPersonalization"))
                {
                    if (key != null)
                    {
                        object? textCollectionValue = key.GetValue("RestrictImplicitTextCollection");
                        object? inkCollectionValue = key.GetValue("RestrictImplicitInkCollection");
                        object? enableInkingValue = key.GetValue("EnableInkingWithTouch");

                        bool isTextCollectionRestricted = textCollectionValue != null && (int)textCollectionValue == (expectedState ? 1 : 0);
                        bool isInkCollectionRestricted = inkCollectionValue != null && (int)inkCollectionValue == (expectedState ? 1 : 0);
                        bool isInkingDisabled = enableInkingValue != null && (int)enableInkingValue == (expectedState ? 0 : 1);

                        return isTextCollectionRestricted && isInkCollectionRestricted && isInkingDisabled;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VerifyInputDataCollectionTweak: Ошибка: {ex.Message}");
                return false;
            }
        }

        private bool VerifyTelemetryTweak(bool expectedState)
        {
            try
            {
                using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection"))
                {
                    if (expectedState)
                    {
                        // Если твик включён, проверяем, что AllowTelemetry равно 0
                        if (key != null)
                        {
                            object? allowTelemetryValue = key.GetValue("AllowTelemetry");
                            return allowTelemetryValue != null && (int)allowTelemetryValue == 0;
                        }
                        return false;
                    }
                    else
                    {
                        // Если твик выключен, проверяем, что ключ отсутствует
                        return key == null;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VerifyTelemetryTweak: Ошибка: {ex.Message}");
                return false;
            }
        }

        private void LoadCurrentSettings()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("LoadCurrentSettings: Начало загрузки настроек из реестра...");

                // Твик 1: Отключение отправки данных о вводе
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\InputPersonalization"))
                {
                    if (key != null)
                    {
                        object? textCollectionValue = key.GetValue("RestrictImplicitTextCollection");
                        object? inkCollectionValue = key.GetValue("RestrictImplicitInkCollection");
                        object? enableInkingValue = key.GetValue("EnableInkingWithTouch");

                        System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: RestrictImplicitTextCollection: {textCollectionValue}");
                        System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: RestrictImplicitInkCollection: {inkCollectionValue}");
                        System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: EnableInkingWithTouch: {enableInkingValue}");

                        bool isTextCollectionRestricted = textCollectionValue != null && (int)textCollectionValue == 1;
                        bool isInkCollectionRestricted = inkCollectionValue != null && (int)inkCollectionValue == 1;
                        bool isInkingDisabled = enableInkingValue != null && (int)enableInkingValue == 0;

                        bool isInputDataCollectionDisabled = isTextCollectionRestricted && isInkCollectionRestricted && isInkingDisabled;
                        TweakStatus.IsInputDataCollectionDisabled = isInputDataCollectionDisabled;
                        DisableInputDataCollection.IsChecked = isInputDataCollectionDisabled;
                        System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: Отключение отправки данных о вводе: {isInputDataCollectionDisabled}");
                    }
                    else
                    {
                        TweakStatus.IsInputDataCollectionDisabled = false;
                        DisableInputDataCollection.IsChecked = false;
                        System.Diagnostics.Debug.WriteLine("LoadCurrentSettings: Ключ InputPersonalization не найден, твик отключен.");
                    }
                }

                // Твик 2: Отключение слежки через реестр
                using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection"))
                {
                    if (key != null)
                    {
                        object? allowTelemetryValue = key.GetValue("AllowTelemetry");
                        System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: AllowTelemetry: {allowTelemetryValue}");

                        bool isTelemetryDisabled = allowTelemetryValue != null && (int)allowTelemetryValue == 0;
                        TweakStatus.IsTelemetryDisabled = isTelemetryDisabled;
                        DisableTelemetry.IsChecked = isTelemetryDisabled;
                        System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: Отключение слежки: {isTelemetryDisabled}");
                    }
                    else
                    {
                        TweakStatus.IsTelemetryDisabled = false;
                        DisableTelemetry.IsChecked = false;
                        System.Diagnostics.Debug.WriteLine("LoadCurrentSettings: Ключ DataCollection не найден, твик отключен.");
                    }
                }

                System.Diagnostics.Debug.WriteLine("LoadCurrentSettings: Текущие настройки успешно загружены из реестра.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: Ошибка: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }
    }
}