using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PWin11_Tweaker_s
{
    // Кастомные модели (локально в файле)
    public class Message
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Role { get; set; } = string.Empty;  // "user" или "assistant"
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ChatSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "Новый чат";
        public List<Message> Messages { get; set; } = new List<Message>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public void GenerateTitle()
        {
            var firstUserMessage = Messages.FirstOrDefault(m => m.Role == "user");
            if (firstUserMessage != null)
            {
                Title = firstUserMessage.Content.Length > 50
                    ? firstUserMessage.Content.Substring(0, 50) + "..."
                    : firstUserMessage.Content;
            }
        }
    }

    public class ChatHistory
    {
        public List<ChatSession> Sessions { get; set; } = new List<ChatSession>();
        public string CurrentSessionId { get; set; } = string.Empty;
    }

    // Модели для Ollama API
    public class OllamaChatRequest
    {
        public string model { get; set; } = string.Empty;
        public List<OllamaMessage> messages { get; set; } = new List<OllamaMessage>();
        public bool stream { get; set; } = false;
        public OllamaOptions? options { get; set; }
    }

    public class OllamaMessage
    {
        public string role { get; set; } = string.Empty;
        public string content { get; set; } = string.Empty;
    }

    public class OllamaOptions
    {
        public float temperature { get; set; } = 0.7f;
        public float top_p { get; set; } = 0.9f;
    }

    public class OllamaChatResponse
    {
        public OllamaMessage message { get; set; } = new OllamaMessage();
        public bool done { get; set; }
    }

    public class OllamaModelsResponse
    {
        public List<OllamaModel> models { get; set; } = new List<OllamaModel>();
    }

    public class OllamaModel
    {
        public string name { get; set; } = string.Empty;
    }

    public sealed partial class eraPage : Page
    {
        private readonly HttpClient _httpClient;
        private readonly string _historyPath;
        private ChatHistory _history = new ChatHistory();
        private ObservableCollection<Message> _currentMessages = new ObservableCollection<Message>();
        private const string ModelName = "microsoft/phi-3-mini-4k-instruct";
        private const string OllamaBaseUrl = "http://localhost:11434";

        public eraPage()
        {
            this.InitializeComponent();
            _httpClient = new HttpClient();

            // Путь к истории
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, "PWin11Tweaker");
            Directory.CreateDirectory(appFolder);
            _historyPath = Path.Combine(appFolder, "eraai_history.json");

            LoadHistoryAndUI();
            CheckOllamaAndModelAsync();  // Проверка при запуске
        }

        private async void CheckOllamaAndModelAsync()
        {
            bool serverReady = await IsOllamaReadyAsync();
            bool modelReady = await IsModelReadyAsync();

            SendButton.IsEnabled = serverReady && modelReady;
            InstallOllamaButton.Visibility = (serverReady && modelReady) ? Visibility.Collapsed : Visibility.Visible;

            if (serverReady && modelReady)
            {
                StatusTextBlock.Text = "EraAI готов. Задавайте вопросы!";
            }
            else if (serverReady)
            {
                StatusTextBlock.Text = "Ollama запущен, но модель не найдена. Установите модель.";
                InstallOllamaButton.Content = "Установить модель ИИ (~2.3 GB)";
            }
            else
            {
                StatusTextBlock.Text = "Ollama не установлен. Установите для EraAI.";
                InstallOllamaButton.Content = "Установить Ollama (~100 MB) + модель (~2.3 GB)";
            }
        }

        private async Task<bool> IsOllamaReadyAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{OllamaBaseUrl}/api/tags");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        private async Task<bool> IsModelReadyAsync()
        {
            if (!await IsOllamaReadyAsync()) return false;

            try
            {
                var response = await _httpClient.GetAsync($"{OllamaBaseUrl}/api/tags");
                if (!response.IsSuccessStatusCode) return false;

                string json = await response.Content.ReadAsStringAsync();
                var modelsResponse = JsonSerializer.Deserialize<OllamaModelsResponse>(json);
                return modelsResponse?.models?.Any(m => m.name == ModelName) ?? false;
            }
            catch { return false; }
        }

        private async void InstallOllamaButton_Click(object sender, RoutedEventArgs e)
        {
            InstallOllamaButton.IsEnabled = false;
            StatusTextBlock.Text = "Установка Ollama...";

            await InstallOllamaAsync();

            // Перепроверка
            await Task.Delay(2000);  // Ждём завершения
            CheckOllamaAndModelAsync();
        }

        private async Task InstallOllamaAsync()
        {
            try
            {
                bool serverReady = await IsOllamaReadyAsync();
                if (!serverReady)
                {
                    StatusTextBlock.Text = "Скачивание Ollama (~100 MB)...";
                    string url = "https://ollama.com/download/OllamaSetup.exe";
                    string tempPath = Path.Combine(Path.GetTempPath(), "OllamaSetup.exe");
                    var bytes = await _httpClient.GetByteArrayAsync(url);
                    await File.WriteAllBytesAsync(tempPath, bytes);

                    var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = tempPath,
                        Arguments = "/S",
                        UseShellExecute = true,
                        Verb = "runas"
                    });
                    if (process != null) await process.WaitForExitAsync();

                    // Запуск сервера
                    Process.Start("ollama", "serve");
                    await Task.Delay(5000);
                }

                bool modelReady = await IsModelReadyAsync();
                if (!modelReady)
                {
                    StatusTextBlock.Text = "Скачивание модели (~2.3 GB)...";
                    Process.Start("ollama", $"pull {ModelName}");
                    await Task.Delay(30000);  // Ждём скачивания (можно мониторить API, но просто)
                }

                StatusTextBlock.Text = "Установка завершена! Перезагрузите страницу.";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Ошибка установки: {ex.Message}";
            }
            finally
            {
                InstallOllamaButton.IsEnabled = true;
            }
        }

        // ... Остальные методы без изменений: LoadHistoryAndUI, UpdateSessionComboBox, LoadCurrentSession, SendButton_Click, GenerateResponseAsync (обновлён для stream=false), NewSessionButton_Click, SessionComboBox_SelectionChanged, SaveHistory ...

        private void LoadHistoryAndUI()
        {
            if (File.Exists(_historyPath))
            {
                string json = File.ReadAllText(_historyPath);
                _history = JsonSerializer.Deserialize<ChatHistory>(json) ?? new ChatHistory();
            }

            UpdateSessionComboBox();
            LoadCurrentSession();
        }

        private void UpdateSessionComboBox()
        {
            SessionComboBox.Items.Clear();
            foreach (var session in _history.Sessions)
            {
                SessionComboBox.Items.Add(new ComboBoxItem { Content = session.Title, Tag = session.Id });
            }
            if (!string.IsNullOrEmpty(_history.CurrentSessionId))
            {
                var currentItem = SessionComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(i => (string)i.Tag == _history.CurrentSessionId);
                if (currentItem != null) SessionComboBox.SelectedItem = currentItem;
            }
        }

        private void LoadCurrentSession()
        {
            var currentSession = _history.Sessions.FirstOrDefault(s => s.Id == _history.CurrentSessionId);
            if (currentSession != null)
            {
                _currentMessages.Clear();
                foreach (var msg in currentSession.Messages)
                {
                    _currentMessages.Add(msg);
                }
                ChatListView.ItemsSource = _currentMessages;
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string prompt = PromptTextBox.Text.Trim();
            if (string.IsNullOrEmpty(prompt)) return;

            SendButton.IsEnabled = false;
            StatusTextBlock.Text = "Генерация ответа...";

            // Добавляем сообщение пользователя
            var userMsg = new Message { Role = "user", Content = prompt };
            _currentMessages.Add(userMsg);
            PromptTextBox.Text = string.Empty;

            // Генерация ответа
            string response = await GenerateResponseAsync(prompt);
            var aiMsg = new Message { Role = "assistant", Content = response };
            _currentMessages.Add(aiMsg);

            // Сохранение
            SaveHistory();
            ChatListView.ScrollIntoView(aiMsg);

            SendButton.IsEnabled = true;
            StatusTextBlock.Text = "Готово!";
        }

        private async Task<string> GenerateResponseAsync(string prompt)
        {
            bool isReady = await IsOllamaReadyAsync() && await IsModelReadyAsync();
            if (!isReady) return "EraAI недоступен. Установите Ollama и модель.";

            try
            {
                // Контекст из текущей сессии (последние 5 сообщений)
                string context = string.Join("\n", _currentMessages.TakeLast(5).Select(m => $"{m.Role}: {m.Content}"));
                string systemPrompt = "Ты эксперт по Windows 11 твикам. Отвечай кратко, с C# кодом если нужно.";
                string fullPrompt = $"{systemPrompt}\n\nКонтекст:\n{context}\n\nПользователь: {prompt}\nAI:";

                // JSON-запрос к /api/chat
                var requestBody = new
                {
                    model = ModelName,
                    messages = new[] { new { role = "user", content = fullPrompt } },
                    stream = false,
                    options = new { temperature = 0.7f, top_p = 0.9f }
                };

                string jsonBody = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{OllamaBaseUrl}/api/chat") { Content = content };

                var httpResponse = await _httpClient.SendAsync(httpRequest);
                if (!httpResponse.IsSuccessStatusCode)
                {
                    return $"Ollama ошибка: {httpResponse.StatusCode}";
                }

                string responseJson = await httpResponse.Content.ReadAsStringAsync();
                var ollamaResponse = JsonSerializer.Deserialize<OllamaChatResponse>(responseJson);

                string response = ollamaResponse?.message?.content ?? "Нет ответа.";
                return response;
            }
            catch (Exception ex)
            {
                return $"Ошибка: {ex.Message}";
            }
        }

        private void NewSessionButton_Click(object sender, RoutedEventArgs e)
        {
            var newSession = new ChatSession();
            _history.Sessions.Add(newSession);
            _history.CurrentSessionId = newSession.Id;
            _currentMessages.Clear();
            UpdateSessionComboBox();
            SaveHistory();
        }

        private void SessionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SessionComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is string sessionId)
            {
                _history.CurrentSessionId = sessionId;
                LoadCurrentSession();
                SaveHistory();
            }
        }

        private void SaveHistory()
        {
            var currentSession = _history.Sessions.FirstOrDefault(s => s.Id == _history.CurrentSessionId);
            if (currentSession != null)
            {
                currentSession.Messages = _currentMessages.ToList();
                currentSession.GenerateTitle();
            }
            string json = JsonSerializer.Serialize(_history, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_historyPath, json);
        }

        // Пример навигации назад
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.Frame != null) this.Frame.GoBack();
        }
    }
}