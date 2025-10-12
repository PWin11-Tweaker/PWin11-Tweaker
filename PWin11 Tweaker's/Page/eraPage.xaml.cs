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
        private AppSettings _appSettings = new AppSettings();
        private int _currentPort; // Добавляем поле для хранения текущего порта

        private readonly string _sessionFile = Path.Combine(AppContext.BaseDirectory, "chat_sessions.json");
        private readonly string _settingsFile = Path.Combine(AppContext.BaseDirectory, "app_settings.json");

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
            LoadSettings();
            LoadSessions();

            // Проверка Ollama
            if (!OllamaManager.IsOllamaInstalled())
            {
                StatusTextBlock.Text = "Ollama не установлена.";
                InstallOllamaButton.Visibility = Visibility.Visible;
                ShowInstallPanel(true);
                return;
            }

            // Запуск Ollama с динамическим портом
            StatusTextBlock.Text = "Запуск Ollama API...";
            _currentPort = await OllamaManager.StartOllamaIfNeededAsync(); // Теперь возвращает int
            if (!await OllamaManager.IsApiReadyAsync(_currentPort))
            {
                StatusTextBlock.Text = "Ошибка: Ollama API недоступен. Проверьте установку.";
                SendButton.IsEnabled = false;
                ShowInstallPanel(true);
                return;
            }

            // Проверка модели
            if (string.IsNullOrEmpty(_appSettings.SelectedModel) || !await OllamaManager.IsModelInstalledAsync(_appSettings.SelectedModel))
            {
                StatusTextBlock.Text = "Модель не выбрана или не установлена.";
                ShowInstallPanel(true);
                return;
            }

            // Инициализация
            _ai = new AIChatService(_appSettings.SelectedModel, _currentPort);
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
            ChangeModelButton.Visibility = Visibility.Visible;
            ShowInstallPanel(false);
        }

        private void ShowInstallPanel(bool show)
        {
            InstallPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            chatScrollViewer.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
            SendButton.IsEnabled = !show;
            PromptTextBox.IsReadOnly = show;
        }

        private async void InstallOllamaButton_Click(object sender, RoutedEventArgs e)
        {
            await OllamaManager.InstallOllamaAsync();
            StatusTextBlock.Text = "Установщик запущен. После установки перезапустите программу.";
        }

        private async void InstallModelButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = ModelSelector.SelectedItem as string;
            if (string.IsNullOrEmpty(selected)) return;

            var modelName = selected.Split(' ')[0]; // Извлекаем имя модели, напр. "gemma3:1b"
            StatusTextBlock.Text = $"Установка модели {modelName}...";
            InstallModelButton.IsEnabled = false;

            try
            {
                await OllamaManager.PullModelAsync(modelName);
                _appSettings.SelectedModel = modelName;
                SaveSettings();
                StatusTextBlock.Text = "Модель установлена.";
                ShowInstallPanel(false);
                _ai = new AIChatService(_appSettings.SelectedModel, _currentPort); // Передаем текущий порт
                SendButton.IsEnabled = true;
                PromptTextBox.IsReadOnly = false;
                ChangeModelButton.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Ошибка установки: {ex.Message}";
            }
            finally
            {
                InstallModelButton.IsEnabled = true;
            }
        }

        private void ChangeModelButton_Click(object sender, RoutedEventArgs e)
        {
            ShowInstallPanel(true);
            StatusTextBlock.Text = "Выберите новую модель для смены.";
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
                var systemMessage = new OllamaMessage { Role = "system", Content = "Ты эксперт Windows 11. Отвечай кратко и по делу на русском языке." };
                var ollamaHistory = new List<OllamaMessage> { systemMessage };
                ollamaHistory.AddRange(_currentSession.Messages
                    .Where(m => m.Role != "ИИ")
                    .Select(m => new OllamaMessage
                    {
                        Role = m.Role == "Пользователь" ? "user" : "assistant",
                        Content = m.Content
                    }));

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
                    var loadedDtos = JsonSerializer.Deserialize<List<ChatSessionDto>>(json, JsonOptions);
                    if (loadedDtos != null)
                    {
                        foreach (var dto in loadedDtos)
                        {
                            var session = new ChatSession(dto);
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
                var dtosToSave = _sessions.Take(10).Select(s => s.ToDto()).ToList();
                var json = JsonSerializer.Serialize(dtosToSave, JsonOptions);
                File.WriteAllText(_sessionFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения сессий: {ex.Message}");
            }
        }

        private void LoadSettings()
        {
            if (File.Exists(_settingsFile))
            {
                try
                {
                    var json = File.ReadAllText(_settingsFile);
                    _appSettings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
                    _currentPort = _appSettings.Port; // Загружаем сохраненный порт
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки настроек: {ex.Message}");
                }
            }
        }

        private void SaveSettings()
        {
            try
            {
                _appSettings.Port = _currentPort; // Сохраняем текущий порт
                var json = JsonSerializer.Serialize(_appSettings, JsonOptions);
                File.WriteAllText(_settingsFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения настроек: {ex.Message}");
            }
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
                    ? new SolidColorBrush(Color.FromArgb(255, 0, 120, 215))
                    : new SolidColorBrush(Color.FromArgb(255, 32, 149, 87));
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