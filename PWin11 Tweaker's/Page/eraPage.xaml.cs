using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using PWin11_Tweaker_s.Models;
using PWin11_Tweaker_s.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.UI;
using OllamaMessage = OllamaSharp.Models.Chat.Message;

namespace PWin11_Tweaker_s
{
    public sealed partial class eraPage : Page
    {
        private AIChatService _ai;
        private ChatSession _currentSession;
        private ObservableCollection<ChatSession> _sessions = new ObservableCollection<ChatSession>();

        private readonly string _sessionFile = Path.Combine(AppContext.BaseDirectory, "chat_sessions.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public eraPage()
        {
            this.InitializeComponent();
            SessionsListView.ItemsSource = _sessions;
            Loaded += eraPage_Loaded;
            PromptTextBox.IsReadOnly = true;
        }

        private async void eraPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Проверка Ollama
            if (!OllamaManager.IsOllamaInstalled())
            {
                StatusTextBlock.Text = "Ollama не установлена.";
                InstallOllamaButton.Visibility = Visibility.Visible;
                return;
            }

            StatusTextBlock.Text = "Проверка модели...";
            if (!await OllamaManager.IsModelInstalledAsync())
            {
                StatusTextBlock.Text = "Модель не установлена, установка...";
                await OllamaManager.PullModelAsync();
                StatusTextBlock.Text = "Модель установлена.";
            }

            // Запуск Ollama serve если нужно и проверка API
            StatusTextBlock.Text = "Запуск Ollama API...";
            await OllamaManager.StartOllamaIfNeededAsync();
            if (!await OllamaManager.IsApiReadyAsync())
            {
                StatusTextBlock.Text = "Ошибка: Ollama API недоступен. Проверьте установку.";
                SendButton.IsEnabled = false;
                return;
            }

            _ai = new AIChatService();
            LoadSessions();
            if (_sessions.Count > 0)
            {
                SessionsListView.SelectedIndex = 0;
                _currentSession = _sessions[0];
                ChatListView.ItemsSource = _currentSession.Messages;
            }
            else
            {
                NewSessionButton_Click(null, null);
            }
            StatusTextBlock.Text = "Готов к работе.";
            SendButton.IsEnabled = true;
            PromptTextBox.IsReadOnly = false;
            PromptTextBox.Focus(FocusState.Programmatic);
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendMessageAsync();
        }

        private async void PromptTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter && SendButton.IsEnabled)
            {
                await SendMessageAsync();
            }
        }

        private async Task SendMessageAsync()
        {
            var userText = PromptTextBox.Text.Trim();
            if (string.IsNullOrEmpty(userText)) return;

            if (_currentSession == null)
            {
                NewSessionButton_Click(null, null);
            }

            SendButton.IsEnabled = false;
            PromptTextBox.IsReadOnly = true;

            var userMessage = new Message { Role = "Пользователь", Content = userText, Timestamp = DateTime.Now };
            _currentSession.Messages.Add(userMessage);
            ScrollToBottom();
            PromptTextBox.Text = "";
            TypingIndicator.Visibility = Visibility.Visible;
            StatusTextBlock.Text = "Генерация ответа...";

            Message aiMessage = null;

            try
            {
                // Формируем историю для Ollama (system + все user/ai сообщения)
                var systemMessage = new OllamaMessage { Role = "system", Content = "Ты эксперт Windows 11. Отвечай кратко и по делу на русском языке." };
                var ollamaHistory = new List<OllamaMessage> { systemMessage };
                ollamaHistory.AddRange(_currentSession.Messages
                    .Where(m => m.Role != "ИИ")
                    .Select(m => new OllamaMessage
                    {
                        Role = m.Role == "Пользователь" ? "user" : "assistant",
                        Content = m.Content
                    }));

                // Добавляем пустое сообщение ИИ для стриминга
                aiMessage = new Message { Role = "ИИ", Content = "", Timestamp = DateTime.Now };
                _currentSession.Messages.Add(aiMessage);

                await foreach (var delta in _ai.StreamAnswerAsync(ollamaHistory))
                {
                    if (!string.IsNullOrEmpty(delta))
                    {
                        aiMessage.Content += delta;
                        ScrollToBottom();
                    }
                }

                if (string.IsNullOrEmpty(aiMessage.Content))
                {
                    aiMessage.Content = "[Ошибка: Пустой ответ от ИИ. Проверьте модель.]";
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Ошибка: {ex.Message}";
                if (aiMessage != null)
                {
                    _currentSession.Messages.Remove(aiMessage);
                }
                aiMessage = new Message { Role = "ИИ", Content = "[Ошибка: " + ex.Message + "]", Timestamp = DateTime.Now };
                _currentSession.Messages.Add(aiMessage);
            }
            finally
            {
                TypingIndicator.Visibility = Visibility.Collapsed;
                SaveSessions();
                SendButton.IsEnabled = true;
                PromptTextBox.IsReadOnly = false;
                PromptTextBox.Focus(FocusState.Programmatic);
                StatusTextBlock.Text = "Готов к работе.";
            }
        }

        private void ScrollToBottom()
        {
            if (chatScrollViewer.ScrollableHeight > 0)
            {
                chatScrollViewer.ChangeView(null, chatScrollViewer.ScrollableHeight, null);
            }
        }

        private void LoadSessions()
        {
            if (File.Exists(_sessionFile))
            {
                try
                {
                    var json = File.ReadAllText(_sessionFile);
                    // Десериализуем в List<ChatSessionDto>
                    var loadedDtos = JsonSerializer.Deserialize<List<ChatSessionDto>>(json, JsonOptions);
                    if (loadedDtos != null)
                    {
                        foreach (var dto in loadedDtos)
                        {
                            var session = new ChatSession(dto); // Создаем ChatSession из DTO (с копированием сообщений)
                            _sessions.Add(session);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки сессий: {ex.Message}");
                }
            }
        }

        private void SaveSessions()
        {
            try
            {
                // Сохраняем только первые 10 сессий
                var dtosToSave = _sessions.Take(10).Select(s => s.ToDto()).ToList(); // Конвертируем в DTO с копированием

                var json = JsonSerializer.Serialize(dtosToSave, JsonOptions);
                File.WriteAllText(_sessionFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения сессий: {ex.Message}");
            }
        }

        private async void InstallOllamaButton_Click(object sender, RoutedEventArgs e)
        {
            await OllamaManager.InstallOllamaAsync();
            StatusTextBlock.Text = "Установщик запущен. После установки перезапустите программу.";
        }

        private void NewSessionButton_Click(object sender, RoutedEventArgs e)
        {
            var newSession = new ChatSession { Title = $"Сессия {DateTime.Now:HH:mm:ss}" };
            _sessions.Add(newSession);
            SessionsListView.SelectedItem = newSession;
            _currentSession = newSession;
            ChatListView.ItemsSource = _currentSession.Messages;
            StatusTextBlock.Text = "Новая сессия создана.";
        }

        private void SessionsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SessionsListView.SelectedItem is ChatSession session)
            {
                _currentSession = session;
                ChatListView.ItemsSource = _currentSession.Messages;
                ScrollToBottom();
            }
        }
    }

    public class RoleToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string role)
            {
                return role == "Пользователь"
                    ? new SolidColorBrush(Color.FromArgb(255, 0, 120, 215)) // Синий для пользователя
                    : new SolidColorBrush(Color.FromArgb(255, 32, 149, 87)); // Зеленый для ИИ
            }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class DateTimeToTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTime dt)
            {
                return dt.ToString("HH:mm");
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}