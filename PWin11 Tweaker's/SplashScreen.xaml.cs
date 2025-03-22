using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Threading.Tasks;
using WinRT.Interop;

namespace PWin11_Tweaker_s
{
    public sealed partial class SplashScreen : Window
    {
        private readonly AppWindow? _appWindow; // Допускаем null

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

                // Устанавливаем размер окна
                if (_appWindow != null)
                {
                    _appWindow.Resize(new Windows.Graphics.SizeInt32(300, 400));

                    // Центрируем окно
                    CenterWindow();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Не удалось инициализировать AppWindow.");
                }

                // Запуск анимации
                StartSplashAnimation();

                // Запуск основной логики приложения
                StartApp();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в SplashScreen: {ex.Message}");
                this.Close();
            }
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
                // Ждём завершения анимации (1.5 секунды) + дополнительное время
                await Task.Delay(3000);

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
    }
}