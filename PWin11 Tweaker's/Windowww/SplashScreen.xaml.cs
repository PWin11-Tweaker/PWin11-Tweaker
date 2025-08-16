using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Threading.Tasks;
using WinRT.Interop;
using Microsoft.Win32;
using System.IO;
using PWin11_Tweaker_s.Script;
using Microsoft.Windows.ApplicationModel.Resources;
using System.Diagnostics;
using Windows.System;

public class TweakCheck
{
    public string Name { get; set; }
    public Delegate CheckFunc { get; set; }

    public TweakCheck(string name, Delegate checkFunc)
    {
        Name = name;
        CheckFunc = checkFunc;
    }
}

namespace PWin11_Tweaker_s
{
    public sealed partial class SplashScreen : Window
    {
        private readonly AppWindow? _appWindow;
        private readonly ResourceLoader _resourceLoader;

        public SplashScreen()
        {
            try
            {
                this.InitializeComponent();
                ExtendsContentIntoTitleBar = true;
                SetTitleBar(SimpleTitleBar);
                IntPtr hWnd = WindowNative.GetWindowHandle(this);
                WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
                _appWindow = AppWindow.GetFromWindowId(windowId);
                _resourceLoader = new ResourceLoader();

                if (_appWindow != null)
                {
                    _appWindow.Resize(new Windows.Graphics.SizeInt32(300, 300));
                    CenterWindow();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("S003");
                }

                if (!IsMicaSupported())
                {
                    System.Diagnostics.Debug.WriteLine("Mica не поддерживается, применяем запасной фон.");
                    if (this.Content is Grid rootGrid)
                    {
                        rootGrid.Background = new SolidColorBrush(Colors.DarkSlateGray);
                    }
                }

                StartApp();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в SplashScreen: {ex.Message}");
                this.Close();
            }
        }

        private bool IsMicaSupported()
        {
            return Environment.OSVersion.Version.Build >= 22000;
        }

        private void CenterWindow()
        {
            try
            {
                if (_appWindow == null)
                {
                    System.Diagnostics.Debug.WriteLine("S003");
                    return;
                }

                var displayArea = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Nearest);
                if (displayArea == null)
                {
                    System.Diagnostics.Debug.WriteLine("Не удалось получить DisplayArea.");
                    return;
                }

                int screenWidth = displayArea.WorkArea.Width;
                int screenHeight = displayArea.WorkArea.Height;
                int windowWidth = _appWindow.Size.Width;
                int windowHeight = _appWindow.Size.Height;
                int x = (screenWidth - windowWidth) / 2;
                int y = (screenHeight - windowHeight) / 2;

                _appWindow.Move(new Windows.Graphics.PointInt32(x, y));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при центрировании окна: {ex.Message}");
            }
        }

        private async void StartApp()
        {
            try
            {
                await Task.Delay(1100);
                await CheckTweaksStatus();

                MainWindow mainWindow = new MainWindow();
                mainWindow.Activate();
                this.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при запуске MainWindow: {ex.Message}");
                this.Close();
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
                return false;
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
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckGalleryFolderDisabled: Ошибка: {ex.Message}");
                return false;
            }
        }

        private async Task CheckTweaksStatus()
        {
            try
            {
                // Явно типизированный массив с использованием класса TweakCheck
                var tweaks = new[]
                {
                    new TweakCheck(_resourceLoader.GetString("TweakClassicContextMenu"), new Func<bool>(() => CheckClassicContextMenu())),
                    new TweakCheck(_resourceLoader.GetString("TweakShowHiddenFiles"), new Func<bool>(() => CheckShowHiddenFiles())),
                    new TweakCheck(_resourceLoader.GetString("TweakSmallCaptions"), new Func<bool>(() => CheckSmallCaptions())),
                    new TweakCheck(_resourceLoader.GetString("TweakStartAllBack"), new Func<bool>(() => CheckStartAllBack())),
                    new TweakCheck(_resourceLoader.GetString("TweakTelemetry"), new Func<bool>(() => CheckTelemetry())),
                    new TweakCheck(_resourceLoader.GetString("TweakAdvertisingId"), new Func<bool>(() => CheckAdvertisingId())),
                    new TweakCheck(_resourceLoader.GetString("TweakLocationTracking"), new Func<bool>(() => CheckLocationTracking())),
                    new TweakCheck(_resourceLoader.GetString("TweakCortana"), new Func<bool>(() => CheckCortana())),
                    new TweakCheck(_resourceLoader.GetString("TweakBackgroundApps"), new Func<bool>(() => CheckBackgroundApps())),
                    new TweakCheck(_resourceLoader.GetString("TweakCloudContent"), new Func<bool>(() => CheckCloudContent())),
                    new TweakCheck(_resourceLoader.GetString("TweakFindMyDevice"), new Func<bool>(() => CheckFindMyDevice())),
                    new TweakCheck(_resourceLoader.GetString("TweakInsiderTelemetry"), new Func<bool>(() => CheckInsiderTelemetry())),
                    new TweakCheck(_resourceLoader.GetString("TweakEdgeDiagnostics"), new Func<bool>(() => CheckEdgeDiagnostics())),
                    new TweakCheck(_resourceLoader.GetString("TweakSuggestedContent"), new Func<bool>(() => CheckSuggestedContent())),
                    new TweakCheck(_resourceLoader.GetString("TweakHomeFolder"), new Func<bool>(() => CheckHomeFolderDisabled())),
                    new TweakCheck(_resourceLoader.GetString("TweakGalleryFolder"), new Func<bool>(() => CheckGalleryFolderDisabled())),
                    new TweakCheck(_resourceLoader.GetString("TweakTaskbarAlignment"), new Func<bool>(() => CheckTaskbarAlignmentLeft())),
                    new TweakCheck(_resourceLoader.GetString("TweakTaskbarTransparency"), new Func<bool>(() => CheckTaskbarTransparencyEnabled())),
                    new TweakCheck(_resourceLoader.GetString("TweakHideSearchButton"), new Func<bool>(() => CheckSearchButtonHidden())),
                    new TweakCheck(_resourceLoader.GetString("TweakVisualEffects"), new Func<bool>(() => CheckVisualEffects())),
                    new TweakCheck(_resourceLoader.GetString("TweakWindowsSearch"), new Func<bool>(() => CheckWindowsSearch())),
                    new TweakCheck(_resourceLoader.GetString("TweakSysMain"), new Func<bool>(() => CheckSysMain())),
                    new TweakCheck(_resourceLoader.GetString("TweakServices"), new Func<bool>(() => CheckServicesDisabled())),
                    new TweakCheck(_resourceLoader.GetString("TweakUAC"), new Func<bool>(() => CheckUACDisabled())),
                    new TweakCheck(_resourceLoader.GetString("TweakClipboardHistory"), new Func<bool>(() => CheckClipboardHistoryDisabled())),
                    new TweakCheck(_resourceLoader.GetString("TweakWindowsSpeedUp"), new Func<bool>(() => CheckWindowsSpeedUpApplied())),
                    new TweakCheck(_resourceLoader.GetString("TweakPowerPlan"), new Func<string?>(() => CheckCurrentPowerPlan()))
                };

                int totalTweaks = tweaks.Length;
                int completedTweaks = 0;

                foreach (var tweak in tweaks)
                {
                    if (this.Content is Grid rootGrid)
                    {
                        if (rootGrid.FindName("StatusText") is TextBlock statusText)
                        {
                            statusText.Text = tweak.Name;
                        }

                        if (rootGrid.FindName("ProgressBar") is ProgressBar progressBar)
                        {
                            completedTweaks++;
                            progressBar.Value = (double)completedTweaks / totalTweaks * 100;
                        }
                    }

                    if (tweak.CheckFunc is Func<bool> boolFunc)
                    {
                        bool result = boolFunc();
                        System.Diagnostics.Debug.WriteLine($"{tweak.Name} Результат: {(result ? "Включён" : "Выключен")}");

                        if (tweak.Name.Contains("классического контекстного меню"))
                        {
                            TweakStatus.IsClassicContextMenuEnabled = result;
                        }
                        else if (tweak.Name.Contains("отображения скрытых файлов"))
                        {
                            TweakStatus.IsShowHiddenFilesEnabled = result;
                        }
                        else if (tweak.Name.Contains("уменьшения кнопок управления окном"))
                        {
                            TweakStatus.IsSmallCaptionsEnabled = result;
                        }
                        else if (tweak.Name.Contains("установки StartAllBack"))
                        {
                            TweakStatus.IsStartAllBackInstalled = result;
                        }
                        else if (tweak.Name.Contains("телеметрии"))
                        {
                            TweakStatus.IsTelemetryDisabled = result;
                        }
                        else if (tweak.Name.Contains("рекламного идентификатора"))
                        {
                            TweakStatus.IsAdvertisingIdDisabled = result;
                        }
                        else if (tweak.Name.Contains("отслеживания местоположения"))
                        {
                            TweakStatus.IsLocationTrackingDisabled = result;
                        }
                        else if (tweak.Name.Contains("Cortana"))
                        {
                            TweakStatus.IsCortanaDisabled = result;
                        }
                        else if (tweak.Name.Contains("фоновых приложений"))
                        {
                            TweakStatus.IsBackgroundAppsDisabled = result;
                        }
                        else if (tweak.Name.Contains("облачного контента"))
                        {
                            TweakStatus.IsCloudContentDisabled = result;
                        }
                        else if (tweak.Name.Contains("функции \"Найти мое устройство\""))
                        {
                            TweakStatus.IsFindMyDeviceDisabled = result;
                        }
                        else if (tweak.Name.Contains("телеметрии Windows Insider"))
                        {
                            TweakStatus.IsInsiderTelemetryDisabled = result;
                        }
                        else if (tweak.Name.Contains("сбора данных Microsoft Edge"))
                        {
                            TweakStatus.IsEdgeDiagnosticsDisabled = result;
                        }
                        else if (tweak.Name.Contains("предлагаемого контента"))
                        {
                            TweakStatus.IsSuggestedContentDisabled = result;
                        }
                        else if (tweak.Name.Contains("папки \"Главное\""))
                        {
                            TweakStatus.IsHomeFolderDisabled = result;
                        }
                        else if (tweak.Name.Contains("папки \"Галерея\""))
                        {
                            TweakStatus.IsGalleryFolderDisabled = result;
                        }
                        else if (tweak.Name.Contains("выравнивания панели задач"))
                        {
                            TweakStatus.IsTaskbarAlignmentLeft = result;
                        }
                        else if (tweak.Name.Contains("прозрачности панели задач"))
                        {
                            TweakStatus.IsTaskbarTransparencyEnabled = result;
                        }
                        else if (tweak.Name.Contains("скрытия кнопки поиска"))
                        {
                            TweakStatus.IsSearchButtonHidden = result;
                        }
                        else if (tweak.Name.Contains("визуальных эффектов"))
                        {
                            TweakStatus.IsVisualEffectsDisabled = result;
                        }
                        else if (tweak.Name.Contains("поиска Windows"))
                        {
                            TweakStatus.IsWindowsSearchDisabled = result;
                        }
                        else if (tweak.Name.Contains("SysMain"))
                        {
                            TweakStatus.IsSysMainDisabled = result;
                        }
                        else if (tweak.Name.Contains("служб"))
                        {
                            TweakStatus.IsServicesDisabled = result;
                        }
                        else if (tweak.Name.Contains("UAC"))
                        {
                            TweakStatus.IsUACDisabled = result;
                        }
                        else if (tweak.Name.Contains("истории буфера обмена"))
                        {
                            TweakStatus.IsClipboardHistoryDisabled = result;
                        }
                        else if (tweak.Name.Contains("ускорения Windows"))
                        {
                            TweakStatus.IsWindowsSpeedUpApplied = result;
                        }
                    }
                    else if (tweak.CheckFunc is Func<string?> stringFunc)
                    {
                        string? result = stringFunc();
                        System.Diagnostics.Debug.WriteLine($"{tweak.Name} Результат: {result ?? "Не определён"}");
                        if (tweak.Name.Contains("плана электропитания"))
                        {
                            TweakStatus.CurrentPowerPlan = result;
                        }
                    }

                    await Task.Delay(200);
                }

                if (this.Content is Grid rootGridFinal)
                {
                    if (rootGridFinal.FindName("StatusText") is TextBlock finalStatusText)
                    {
                        finalStatusText.Text = "";
                    }

                    if (rootGridFinal.FindName("ProgressBar") is ProgressBar finalProgressBar)
                    {
                        finalProgressBar.Value = 100;
                    }
                }

                await Task.Delay(200);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при проверке твиков: {ex.Message}");
            }
        }

        private bool CheckClassicContextMenu()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32");
                if (key != null)
                {
                    string? defaultValue = key.GetValue("") as string;
                    bool isClassicContextMenuEnabled = string.IsNullOrEmpty(defaultValue);
                    System.Diagnostics.Debug.WriteLine($"CheckClassicContextMenu: Ключ найден, значение по умолчанию: '{defaultValue}', результат: {isClassicContextMenuEnabled}");
                    return isClassicContextMenuEnabled;
                }
                System.Diagnostics.Debug.WriteLine("CheckClassicContextMenu: Ключ не найден, результат: false");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при проверке классического контекстного меню: {ex.Message}");
                return false;
            }
        }

        private bool CheckShowHiddenFiles()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
                if (key != null)
                {
                    return (int?)key.GetValue("Hidden", 0) == 1;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при проверке отображения скрытых файлов: {ex.Message}");
                return false;
            }
        }

        private bool CheckSmallCaptions()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop\WindowMetrics");
                if (key != null)
                {
                    string? captionHeight = key.GetValue("CaptionHeight", "-330") as string;
                    if (int.TryParse(captionHeight, out int height))
                    {
                        return height > -330;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при проверке уменьшения кнопок управления окном: {ex.Message}");
                return false;
            }
        }

        private bool CheckStartAllBack()
        {
            try
            {
                return File.Exists(@"C:\Program Files\StartAllBack\StartAllBackCfg.exe");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при проверке StartAllBack: {ex.Message}");
                return false;
            }
        }

        private bool CheckTelemetry()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection");
                return (int?)key?.GetValue("AllowTelemetry", 1) == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при проверке телеметрии: {ex.Message}");
                return false;
            }
        }

        private bool CheckAdvertisingId()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo");
                return (int?)key?.GetValue("Enabled", 1) == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при проверке рекламного идентификатора: {ex.Message}");
                return false;
            }
        }

        private bool CheckLocationTracking()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors");
                return (int?)key?.GetValue("DisableLocation", 0) == 1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при проверке отслеживания местоположения: {ex.Message}");
                return false;
            }
        }

        private bool CheckCortana()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search");
                return (int?)key?.GetValue("AllowCortana", 1) == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при проверке Cortana: {ex.Message}");
                return false;
            }
        }

        private bool CheckBackgroundApps()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications");
                return (int?)key?.GetValue("GlobalUserDisabled", 0) == 1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при проверке фоновых приложений: {ex.Message}");
                return false;
            }
        }

        private bool CheckCloudContent()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\CloudExperienceHost");
                return (int?)key?.GetValue("DisableCloudOptimizedContent", 0) == 1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при проверке облачного контента: {ex.Message}");
                return false;
            }
        }

        private bool CheckFindMyDevice()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\FindMyDevice");
                return (int?)key?.GetValue("AllowFindMyDevice", 1) == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при проверке функции 'Найти мое устройство': {ex.Message}");
                return false;
            }
        }

        private bool CheckInsiderTelemetry()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\PreviewBuilds");
                return (int?)key?.GetValue("AllowBuildPreview", 1) == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при проверке телеметрии Windows Insider: {ex.Message}");
                return false;
            }
        }

        private bool CheckEdgeDiagnostics()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Edge");
                return (int?)key?.GetValue("DiagnosticData", 1) == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при проверке сбора данных Microsoft Edge: {ex.Message}");
                return false;
            }
        }

        private bool CheckSuggestedContent()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager");
                return (int?)key?.GetValue("SubscribedContent-338393Enabled", 1) == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при проверке предлагаемого контента: {ex.Message}");
                return false;
            }
        }

        private bool CheckTaskbarAlignmentLeft()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
                int? alignment = key?.GetValue("TaskbarAl") as int?;
                return alignment == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckTaskbarAlignmentLeft: Ошибка: {ex.Message}");
                return false;
            }
        }

        private bool CheckTaskbarTransparencyEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                int? transparency = key?.GetValue("EnableTransparency") as int?;
                return transparency == 1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckTaskbarTransparencyEnabled: Ошибка: {ex.Message}");
                return false;
            }
        }

        private bool CheckSearchButtonHidden()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Search");
                int? searchboxTaskbarMode = key?.GetValue("SearchboxTaskbarMode") as int?;
                return searchboxTaskbarMode == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckSearchButtonHidden: Ошибка: {ex.Message}");
                return false;
            }
        }

        private bool CheckVisualEffects()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects");
                int? effects = key?.GetValue("VisualFXSetting", 3) as int?;
                System.Diagnostics.Debug.WriteLine($"CheckVisualEffects: Значение VisualFXSetting = {effects}");
                return effects == 2;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckVisualEffects: Ошибка: {ex.Message}");
                return false;
            }
        }

        private bool CheckWindowsSearch()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\WSearch");
                int? start = key?.GetValue("Start") as int?;
                System.Diagnostics.Debug.WriteLine($"CheckWindowsSearch: Значение Start = {start}");
                return start == 4;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckWindowsSearch: Ошибка: {ex.Message}");
                return false;
            }
        }

        private bool CheckSysMain()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\SysMain");
                int? start = key?.GetValue("Start") as int?;
                System.Diagnostics.Debug.WriteLine($"CheckSysMain: Значение Start = {start}");
                return start == 4;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckSysMain: Ошибка: {ex.Message}");
                return false;
            }
        }

        private bool CheckServicesDisabled()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services");
                if (key != null)
                {
                    foreach (string subKeyName in key.GetSubKeyNames())
                    {
                        using var subKey = key.OpenSubKey(subKeyName);
                        int? start = subKey?.GetValue("Start") as int?;
                        if (start == 4) return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckServicesDisabled: Ошибка: {ex.Message}");
                return false;
            }
        }

        private bool CheckUACDisabled()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");
                int? consentPromptBehavior = key?.GetValue("ConsentPromptBehaviorAdmin", 5) as int?;
                return consentPromptBehavior == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckUACDisabled: Ошибка: {ex.Message}");
                return false;
            }
        }

        private bool CheckClipboardHistoryDisabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Clipboard");
                int? enabled = key?.GetValue("EnableClipboardHistory", 1) as int?;
                return enabled == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckClipboardHistoryDisabled: Ошибка: {ex.Message}");
                return false;
            }
        }

        private bool CheckWindowsSpeedUpApplied()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\PWin11Tweaker\Performance");
                return key?.GetValue("WindowsSpeedUpApplied") != null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckWindowsSpeedUpApplied: Ошибка: {ex.Message}");
                return false;
            }
        }

        private string? CheckCurrentPowerPlan()
        {
            try
            {
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
                    System.Diagnostics.Debug.WriteLine("CheckCurrentPowerPlan: Не удалось запустить powercfg.");
                    return "Balanced";
                }

                string output = powercfg.StandardOutput.ReadToEnd();
                powercfg.WaitForExit();
                System.Diagnostics.Debug.WriteLine($"CheckCurrentPowerPlan: Вывод powercfg: {output}");

                if (output.Contains("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"))
                    return "HighPerformance";
                else if (output.Contains("381b4222-f694-41f0-9685-ff5bb260df2e"))
                    return "Balanced";
                else if (output.Contains("a1841308-3541-4fab-bc81-f71556f20b4a"))
                    return "PowerSaver";
                else
                    return "Balanced";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckCurrentPowerPlan: Ошибка: {ex.Message}");
                return "Balanced";
            }
        }

        private async void FeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("https://github.com/PWin11-Tweaker/PWin11-Tweaker/issues/new?labels=bug&template=bug-report---.md"));
            Debug.WriteLine("Feedback link opened.");
        }
    }
}