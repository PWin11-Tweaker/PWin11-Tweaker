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
            try
            {
                this.InitializeComponent();
                System.Diagnostics.Debug.WriteLine("App: Инициализация завершена.");
            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText("app_init_error.log",
                    $"Ошибка в App: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        // Указываем, что sender может быть null
        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            // Логируем асинхронные ошибки
            System.Diagnostics.Debug.WriteLine($"Асинхронная ошибка: {e.Exception.Message}\nСтек: {e.Exception.StackTrace}");
            e.SetObserved(); // Помечаем как обработанное
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            // Твой код запуска окна
            Window window = new MainWindow();
            window.Activate();
        }
    }
}