using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using PWin11_Tweaker_s.Script;
using System.Diagnostics;
using Microsoft.UI.Dispatching;

namespace PWin11_Tweaker_s
{
    public sealed partial class SettingsPage : Page
    {
        public ObservableCollection<string> DebugMessages => DebugLogger.LogMessages;

        public SettingsPage()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("SettingsPage: Начало инициализации...");
                this.InitializeComponent();
                System.Diagnostics.Debug.WriteLine("SettingsPage: InitializeComponent завершён.");

                // Тестовое сообщение до инициализации
                DebugLogger.LogMessages.Add($"[Manual] Тестовое сообщение до инициализации - {DateTime.Now}");

                // Инициализация DebugLogger (если ещё не инициализирован)
                if (DebugLogger.LogMessages.Count == 1) // Считаем только тестовое сообщение
                {
                    DebugLogger.Initialize();
                    System.Diagnostics.Debug.WriteLine("SettingsPage: DebugLogger инициализирован.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("SettingsPage: DebugLogger уже инициализирован.");
                }

                // Тестовое сообщение после инициализации
                System.Diagnostics.Debug.WriteLine($"SettingsPage: Страница успешно загружена - {DateTime.Now}");
                DebugLogger.LogMessages.Add($"[Manual] Тестовое сообщение после инициализации - {DateTime.Now}");

                // Генерируем несколько тестовых сообщений для проверки
                for (int i = 0; i < 5; i++)
                {
                    System.Diagnostics.Debug.WriteLine($"SettingsPage: Тестовое сообщение {i} - {DateTime.Now}");
                }

                System.Diagnostics.Debug.WriteLine("SettingsPage: Инициализация завершена успешно.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SettingsPage: Ошибка при инициализации: {ex.Message}\nStackTrace: {ex.StackTrace}");
                ShowStatus($"Ошибка инициализации: {ex.Message}");
            }
        }

        private void TestLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"TestLogButton_Click: Тестовое сообщение из кнопки - {DateTime.Now}");
                ShowStatus("Тестовое сообщение добавлено в лог!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TestLogButton_Click: Ошибка: {ex.Message}");
                ShowStatus($"Ошибка теста: {ex.Message}");
            }
        }

        private void ShowStatus(string message)
        {
            StatusText.Text = message;
            StatusText.Visibility = Visibility.Visible;

            // Используем DispatcherTimer для скрытия статуса через 5 секунд
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(5);
            timer.Tick += (s, e) =>
            {
                StatusText.Visibility = Visibility.Collapsed;
                timer.Stop();
            };
            timer.Start();
        }
    }
}