using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using WinRT.Interop;

namespace PWin11_Tweaker_s
{
    public sealed partial class RebootWindow : Window
    {
        private readonly AppWindow? _appWindow; // Допускаем null

        public RebootWindow()
        {
            try
            {
                this.InitializeComponent();

                // Инициализация AppWindow
                var windowHandle = WindowNative.GetWindowHandle(this);
                var windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
                _appWindow = AppWindow.GetFromWindowId(windowId); // Теперь это в конструкторе

                // Устанавливаем размер окна
                if (_appWindow != null)
                {
                    _appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 300, Height = 150 });

                    // Центрируем окно
                    var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest);
                    if (displayArea != null)
                    {
                        int x = (displayArea.WorkArea.Width - 300) / 2;
                        int y = (displayArea.WorkArea.Height - 150) / 2;
                        _appWindow.Move(new Windows.Graphics.PointInt32 { X = x, Y = y });
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Не удалось получить DisplayArea.");
                    }

                    // Настраиваем Presenter
                    if (_appWindow.Presenter is OverlappedPresenter presenter)
                    {
                        presenter.IsResizable = false;
                        presenter.IsMaximizable = false;
                    }

                    // Устанавливаем заголовок
                    _appWindow.Title = "Перезагрузка системы";
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Не удалось инициализировать AppWindow.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в RebootWindow: {ex.Message}");
                this.Close();
            }
        }

        private void RebootNowButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("shutdown", "/r /t 0");
                this.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при перезагрузке: {ex.Message}");
            }
        }

        private void RebootLaterButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}