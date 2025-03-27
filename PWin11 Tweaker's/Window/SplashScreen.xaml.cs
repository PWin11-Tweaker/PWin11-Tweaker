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
using PWin11_Tweaker_s.Script; // Добавляем для File.Exists

namespace PWin11_Tweaker_s
{
    public sealed partial class SplashScreen : Window
    {
        private readonly AppWindow? _appWindow;

        public SplashScreen()
        {
            try
            {
                // Инициализация компонентов XAML
                this.InitializeComponent();

                // Получаем AppWindow для управления окном
                IntPtr hWnd = WindowNative.GetWindowHandle(this);
                WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
                _appWindow = AppWindow.GetFromWindowId(windowId);

                if (_appWindow != null)
                {
                    // Устанавливаем размер окна
                    _appWindow.Resize(new Windows.Graphics.SizeInt32(300, 400));

                    // Убираем рамку и заголовок
                    if (_appWindow.Presenter is OverlappedPresenter presenter)
                    {
                        presenter.SetBorderAndTitleBar(false, false);
                    }

                    // Центрируем окно
                    CenterWindow();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Не удалось инициализировать AppWindow.");
                }

                // Проверяем поддержку Mica и применяем запасной фон, если нужно
                if (!IsMicaSupported())
                {
                    System.Diagnostics.Debug.WriteLine("Mica не поддерживается, применяем запасной фон.");
                    if (this.Content is Grid rootGrid)
                    {
                        rootGrid.Background = new SolidColorBrush(Colors.DarkSlateGray);
                    }
                }

                // Запуск анимации
                StartSplashAnimation();

                // Запуск проверки твиков и основной логики приложения
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
            // Mica поддерживается только на Windows 11 (сборка 22000 и выше)
            return Environment.OSVersion.Version.Build >= 22000;
        }

        private void CenterWindow()
        {
            try
            {
                if (_appWindow == null)
                {
                    System.Diagnostics.Debug.WriteLine("AppWindow не инициализирован.");
                    return;
                }

                // Получаем размеры экрана
                var displayArea = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Nearest);
                if (displayArea == null)
                {
                    System.Diagnostics.Debug.WriteLine("Не удалось получить DisplayArea.");
                    return;
                }

                int screenWidth = displayArea.WorkArea.Width;
                int screenHeight = displayArea.WorkArea.Height;

                // Вычисляем позицию для центрирования
                int windowWidth = _appWindow.Size.Width;
                int windowHeight = _appWindow.Size.Height;
                int x = (screenWidth - windowWidth) / 2;
                int y = (screenHeight - windowHeight) / 2;

                // Устанавливаем позицию окна
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
                    // Находим Image по имени
                    if (rootGrid.FindName("SplashImage") is Image splashImage)
                    {
                        // Создаем Storyboard
                        Storyboard storyboard = new Storyboard();

                        // Анимация для Opacity
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

                        // Анимация для ScaleX
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

                        // Анимация для ScaleY
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

                        // Запускаем анимацию
                        storyboard.Begin();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Элемент 'SplashImage' не найден.");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Корневой элемент не является Grid.");
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
                // Ждём завершения анимации (1.5 секунды)
                await Task.Delay(1500);

                // Проверяем состояние твиков
                await CheckTweaksStatus();

                // Открываем MainWindow
                MainWindow mainWindow = new MainWindow();
                mainWindow.Activate();

                // Закрываем SplashScreen
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
                // Список твиков для проверки
                var tweaks = new[]
                {
                    new { Name = "Проверка классического контекстного меню...", CheckFunc = new Func<bool>(() => CheckClassicContextMenu()) },
                    new { Name = "Проверка скрытности файлов...", CheckFunc = new Func<bool>(() => CheckShowHiddenFiles()) },
                    new { Name = "Проверка уменьшения кнопок управления окном...", CheckFunc = new Func<bool>(() => CheckSmallCaptions()) },
                    new { Name = "Проверка установки StartAllBack...", CheckFunc = new Func<bool>(() => CheckStartAllBack()) }
                };

                int totalTweaks = tweaks.Length;
                int completedTweaks = 0;

                foreach (var tweak in tweaks)
                {
                    // Обновляем текст статуса
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

                    // Выполняем проверку
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
                    // Задержка для имитации проверки
                    await Task.Delay(500);
                }

                // Финальный статус
                if (this.Content is Grid rootGridFinal)
                {
                    if (rootGridFinal.FindName("StatusText") is TextBlock finalStatusText)
                    {
                        finalStatusText.Text = "Проверка завершена!";
                    }

                    if (rootGridFinal.FindName("ProgressBar") is ProgressBar finalProgressBar)
                    {
                        finalProgressBar.Value = 100;
                    }
                }

                // Дополнительная задержка перед открытием MainWindow
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
    }
}