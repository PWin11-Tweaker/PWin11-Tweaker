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

                IntPtr hWnd = WindowNative.GetWindowHandle(this);
                WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
                _appWindow = AppWindow.GetFromWindowId(windowId);
                _resourceLoader = new ResourceLoader();

                if (_appWindow != null)
                {
                    _appWindow.Resize(new Windows.Graphics.SizeInt32(300, 400));

                    if (_appWindow.Presenter is OverlappedPresenter presenter)
                    {
                        presenter.SetBorderAndTitleBar(false, false);
                    }

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

                StartSplashAnimation();
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

        private void StartSplashAnimation()
        {
            try
            {
                if (this.Content is Grid rootGrid)
                {
                    if (rootGrid.FindName("SplashImage") is Image splashImage)
                    {
                        Storyboard storyboard = new Storyboard();

                        DoubleAnimation opacityAnimation = new DoubleAnimation
                        {
                            From = 0,
                            To = 1,
                            Duration = new Duration(TimeSpan.FromSeconds(1.5)),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                        };
                        Storyboard.SetTarget(opacityAnimation, splashImage);
                        Storyboard.SetTargetProperty(opacityAnimation, "Opacity");
                        storyboard.Children.Add(opacityAnimation);

                        DoubleAnimation scaleXAnimation = new DoubleAnimation
                        {
                            From = 0.8,
                            To = 1,
                            Duration = new Duration(TimeSpan.FromSeconds(1.5)),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                        };
                        Storyboard.SetTarget(scaleXAnimation, splashImage);
                        Storyboard.SetTargetProperty(scaleXAnimation, "(UIElement.RenderTransform).(ScaleTransform.ScaleX)");
                        storyboard.Children.Add(scaleXAnimation);

                        DoubleAnimation scaleYAnimation = new DoubleAnimation
                        {
                            From = 0.8,
                            To = 1,
                            Duration = new Duration(TimeSpan.FromSeconds(1.5)),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                        };
                        Storyboard.SetTarget(scaleYAnimation, splashImage);
                        Storyboard.SetTargetProperty(scaleYAnimation, "(UIElement.RenderTransform).(ScaleTransform.ScaleY)");
                        storyboard.Children.Add(scaleYAnimation);

                        storyboard.Begin();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("S001");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("S002");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при запуске анимации: {ex.Message}");
            }
        }

        private async void StartApp()
        {
            try
            {
                await Task.Delay(1500);
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

        private async Task CheckTweaksStatus()
        {
            try
            {
                var tweaks = new[]
                {
                    new { Name = _resourceLoader.GetString("TweakClassicContextMenu"), CheckFunc = new Func<bool>(() => CheckClassicContextMenu()) },
                    new { Name = _resourceLoader.GetString("TweakShowHiddenFiles"), CheckFunc = new Func<bool>(() => CheckShowHiddenFiles()) },
                    new { Name = _resourceLoader.GetString("TweakSmallCaptions"), CheckFunc = new Func<bool>(() => CheckSmallCaptions()) },
                    new { Name = _resourceLoader.GetString("TweakStartAllBack"), CheckFunc = new Func<bool>(() => CheckStartAllBack()) },
                    new { Name = _resourceLoader.GetString("TweakTelemetry"), CheckFunc = new Func<bool>(() => CheckTelemetry()) },
                    new { Name = _resourceLoader.GetString("TweakAdvertisingId"), CheckFunc = new Func<bool>(() => CheckAdvertisingId()) },
                    new { Name = _resourceLoader.GetString("TweakLocationTracking"), CheckFunc = new Func<bool>(() => CheckLocationTracking()) },
                    new { Name = _resourceLoader.GetString("TweakCortana"), CheckFunc = new Func<bool>(() => CheckCortana()) },
                    new { Name = _resourceLoader.GetString("TweakBackgroundApps"), CheckFunc = new Func<bool>(() => CheckBackgroundApps()) },
                    new { Name = _resourceLoader.GetString("TweakCloudContent"), CheckFunc = new Func<bool>(() => CheckCloudContent()) },
                    new { Name = _resourceLoader.GetString("TweakFindMyDevice"), CheckFunc = new Func<bool>(() => CheckFindMyDevice()) },
                    new { Name = _resourceLoader.GetString("TweakInsiderTelemetry"), CheckFunc = new Func<bool>(() => CheckInsiderTelemetry()) },
                    new { Name = _resourceLoader.GetString("TweakEdgeDiagnostics"), CheckFunc = new Func<bool>(() => CheckEdgeDiagnostics()) },
                    new { Name = _resourceLoader.GetString("TweakSuggestedContent"), CheckFunc = new Func<bool>(() => CheckSuggestedContent()) }
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

                    bool result = tweak.CheckFunc();
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

                    await Task.Delay(500);
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

                await Task.Delay(500);
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
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}");
                return key != null;
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
    }
}