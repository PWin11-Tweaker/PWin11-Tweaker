using Microsoft.UI.Xaml;
using Microsoft.UI.Dispatching;
using System;
using Windows.UI.Popups; // Для MessageDialog
using System.Threading.Tasks;

namespace PWin11_Tweaker_s
{
    public partial class App : Application
    {
        public App()
        {
            this.InitializeComponent();

            // Подписываемся на необработанные исключения для WinUI
            this.UnhandledException += App_UnhandledException;

            // Подписываемся на необработанные исключения в задачах
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private async void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // Предотвращаем завершение приложения сразу
            e.Handled = true;

            // Логируем исключение
            System.Diagnostics.Debug.WriteLine($"Необработанное исключение: {e.Exception.Message}\nСтек: {e.Exception.StackTrace}");

            // Показываем сообщение пользователю (асинхронно)
            await ShowErrorDialog(e.Exception.Message);
        }

        // Указываем, что sender может быть null
        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            // Логируем асинхронные ошибки
            System.Diagnostics.Debug.WriteLine($"Асинхронная ошибка: {e.Exception.Message}\nСтек: {e.Exception.StackTrace}");
            e.SetObserved(); // Помечаем как обработанное
        }

        private async Task ShowErrorDialog(string message)
        {
            // Используем MessageDialog из Windows.UI.Popups
            var dialog = new MessageDialog($"Произошла ошибка: {message}", "Ошибка");
            dialog.Commands.Add(new UICommand("ОК"));
            await dialog.ShowAsync();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            // Твой код запуска окна
            Window window = new SplashScreen();
            window.Activate();
        }
    }
}