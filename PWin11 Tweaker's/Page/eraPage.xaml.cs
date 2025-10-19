using Microsoft.UI;
using Microsoft.UI.Dispatching;
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
using System.Diagnostics;
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
        private int _currentPort;

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
                ShowInstallPanel(true, true); // Показать панель установки Ollama
                return;
            }

            // Запуск Ollama с динамическим портом
            StatusTextBlock.Text = "Запуск Ollama API...";
            _currentPort = await OllamaManager.StartOllamaIfNeededAsync(); // Теперь возвращает int
            if (!await OllamaManager.IsApiReadyAsync(_currentPort))
            {
                StatusTextBlock.Text = "Ошибка: Ollama API недоступен. Проверьте установку.";
                ShowInstallPanel(true, true); // Показать панель установки Ollama
                return;
            }

            // Проверка модели
            if (string.IsNullOrEmpty(_appSettings.SelectedModel) || !await OllamaManager.IsModelInstalledAsync(_appSettings.SelectedModel))
            {
                StatusTextBlock.Text = "Модель не выбрана или не установлена. Выберите модель.";
                ShowInstallPanel(true, false); // Показать панель выбора модели
                return;
            }

            // Инициализация
            try
            {
                _ai = new AIChatService(_appSettings.SelectedModel, _currentPort);
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Ошибка инициализации AI: {ex.Message}";
                return;
            }

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

        private void RefreshSession()
        {
            if (_currentSession == null) return;
            var oldSession = _currentSession;
            var newSession = new ChatSession { Title = oldSession.Title };
            newSession.CopyMessagesFrom(oldSession);
            int index = _sessions.IndexOf(oldSession);
            _sessions[index] = newSession;
            _currentSession = newSession;
            ChatListView.ItemsSource = _currentSession.Messages;
            SessionsListView.SelectedItem = _currentSession;
            SaveSessions();
            ScrollToBottom();
        }

        private void ShowInstallPanel(bool show, bool isOllamaMissing = false)
        {
            InstallPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            chatScrollViewer.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
            SendButton.IsEnabled = !show;
            PromptTextBox.IsReadOnly = show;

            if (show)
            {
                if (isOllamaMissing)
                {
                    InstallTitleTextBlock.Text = "Скачай Ollama для начала работы с ИИ";
                    InstallOllamaPanelButton.Visibility = Visibility.Visible;
                    InstallRussianOllamaPanelButton.Visibility = Visibility.Visible;
                    InstallOllamaStatusText.Visibility = Visibility.Visible;
                    InstallProgressBar.Visibility = Visibility.Visible;
                    LaunchOllamaButton.Visibility = Visibility.Collapsed;
                    ModelSelector.Visibility = Visibility.Collapsed;
                    InstallModelButton.Visibility = Visibility.Collapsed;
                }
                else
                {
                    InstallTitleTextBlock.Text = "Выберите модель ИИ для установки";
                    InstallOllamaPanelButton.Visibility = Visibility.Collapsed;
                    InstallRussianOllamaPanelButton.Visibility = Visibility.Collapsed;
                    InstallOllamaStatusText.Visibility = Visibility.Collapsed;
                    InstallProgressBar.Visibility = Visibility.Collapsed;
                    LaunchOllamaButton.Visibility = Visibility.Collapsed;
                    ModelSelector.Visibility = Visibility.Visible;
                    InstallModelButton.Visibility = Visibility.Visible;
                }
            }
        }

        private async void InstallOllamaPanelButton_Click(object sender, RoutedEventArgs e)
        {
            InstallOllamaPanelButton.IsEnabled = false;
            InstallRussianOllamaPanelButton.IsEnabled = false;
            InstallOllamaStatusText.Visibility = Visibility.Visible;
            InstallProgressBar.Visibility = Visibility.Visible;
            InstallOllamaStatusText.Text = "Скачивание быстрой версии с GitHub (~1 GB)...";
            InstallProgressBar.Value = 0;

            var progress = new Progress<(double percent, string status)>(data =>
            {
                System.Diagnostics.Debug.WriteLine($"Progress: {data.percent}% - {data.status}");
                var dispatcher = DispatcherQueue.GetForCurrentThread();
                if (dispatcher != null)
                {
                    dispatcher.TryEnqueue(() =>
                    {
                        InstallProgressBar.Value = data.percent;
                        InstallOllamaStatusText.Text = data.status;
                    });
                }
                else
                {
                    InstallProgressBar.Value = data.percent;
                    InstallOllamaStatusText.Text = data.status;
                }
            });

            try
            {
                await OllamaManager.InstallOllamaAsync(progress);
                var dispatcher = DispatcherQueue.GetForCurrentThread();
                if (dispatcher != null)
                {
                    dispatcher.TryEnqueue(() =>
                    {
                        InstallOllamaStatusText.Text = "Скачивание завершено. Запустите установщик вручную.";
                        LaunchOllamaButton.Visibility = Visibility.Visible;
                    });
                }
                else
                {
                    InstallOllamaStatusText.Text = "Скачивание завершено. Запустите установщик вручную.";
                    LaunchOllamaButton.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                var dispatcher = DispatcherQueue.GetForCurrentThread();
                if (dispatcher != null)
                {
                    dispatcher.TryEnqueue(() => InstallOllamaStatusText.Text = $"Ошибка: {ex.Message}");
                }
                else
                {
                    InstallOllamaStatusText.Text = $"Ошибка: {ex.Message}";
                }
                System.Diagnostics.Debug.WriteLine($"Installation error: {ex.Message}");
            }
            finally
            {
                var dispatcher = DispatcherQueue.GetForCurrentThread();
                if (dispatcher != null)
                {
                    dispatcher.TryEnqueue(() =>
                    {
                        InstallOllamaPanelButton.IsEnabled = true;
                        InstallRussianOllamaPanelButton.IsEnabled = true;
                    });
                }
                else
                {
                    InstallOllamaPanelButton.IsEnabled = true;
                    InstallRussianOllamaPanelButton.IsEnabled = true;
                }
            }
        }

        private async void InstallRussianOllamaPanelButton_Click(object sender, RoutedEventArgs e)
        {
            InstallOllamaPanelButton.IsEnabled = false;
            InstallRussianOllamaPanelButton.IsEnabled = false;
            InstallOllamaStatusText.Visibility = Visibility.Visible;
            InstallProgressBar.Visibility = Visibility.Visible;
            InstallOllamaStatusText.Text = "Скачивание версии для России с pwin11.ru (~1 GB)...";
            InstallProgressBar.Value = 0;

            var progress = new Progress<(double percent, string status)>(data =>
            {
                System.Diagnostics.Debug.WriteLine($"Progress (Russian): {data.percent}% - {data.status}");
                var dispatcher = DispatcherQueue.GetForCurrentThread();
                if (dispatcher != null)
                {
                    dispatcher.TryEnqueue(() =>
                    {
                        InstallProgressBar.Value = data.percent;
                        InstallOllamaStatusText.Text = data.status;
                    });
                }
                else
                {
                    InstallProgressBar.Value = data.percent;
                    InstallOllamaStatusText.Text = data.status;
                }
            });

            try
            {
                await OllamaManager.InstallRussianOllamaAsync(progress);
                var dispatcher = DispatcherQueue.GetForCurrentThread();
                if (dispatcher != null)
                {
                    dispatcher.TryEnqueue(() =>
                    {
                        InstallOllamaStatusText.Text = "Скачивание завершено. Запустите установщик вручную.";
                        LaunchOllamaButton.Visibility = Visibility.Visible;
                    });
                }
                else
                {
                    InstallOllamaStatusText.Text = "Скачивание завершено. Запустите установщик вручную.";
                    LaunchOllamaButton.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                var dispatcher = DispatcherQueue.GetForCurrentThread();
                if (dispatcher != null)
                {
                    dispatcher.TryEnqueue(() => InstallOllamaStatusText.Text = $"Ошибка: {ex.Message}");
                }
                else
                {
                    InstallOllamaStatusText.Text = $"Ошибка: {ex.Message}";
                }
                System.Diagnostics.Debug.WriteLine($"Installation error (Russian): {ex.Message}");
            }
            finally
            {
                var dispatcher = DispatcherQueue.GetForCurrentThread();
                if (dispatcher != null)
                {
                    dispatcher.TryEnqueue(() =>
                    {
                        InstallOllamaPanelButton.IsEnabled = true;
                        InstallRussianOllamaPanelButton.IsEnabled = true;
                    });
                }
                else
                {
                    InstallOllamaPanelButton.IsEnabled = true;
                    InstallRussianOllamaPanelButton.IsEnabled = true;
                }
            }
        }

        private async void InstallOllamaButton_Click(object sender, RoutedEventArgs e)
        {
            await OllamaManager.InstallOllamaAsync();
            StatusTextBlock.Text = "Скачивание завершено. Запустите установщик вручную.";
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
            ShowInstallPanel(true, false);
            StatusTextBlock.Text = "Выберите новую модель для смены.";
        }

        private void LaunchOllamaButton_Click(object sender, RoutedEventArgs e)
        {
            var installerPath = Path.Combine(AppContext.BaseDirectory, "OllamaSetup.exe");
            if (File.Exists(installerPath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = installerPath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    InstallOllamaStatusText.Text = $"Ошибка запуска: {ex.Message}";
                    System.Diagnostics.Debug.WriteLine($"Launch error: {ex.Message}");
                }
            }
            else
            {
                InstallOllamaStatusText.Text = "Файл установщика не найден.";
            }
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
                var systemMessage = new OllamaMessage { Role = "system", Content = "Your name is eraAI, you are an experienced Windows 11 assistant, and you must respond in the language I am writing in" };
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
                RefreshSession();
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
                    ? new SolidColorBrush(Color.FromArgb(255, 43, 113, 243))
                    : new SolidColorBrush(Color.FromArgb(255, 35, 57, 204));
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