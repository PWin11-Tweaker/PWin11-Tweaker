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
using Microsoft.Windows.ApplicationModel.Resources;
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
        private readonly ResourceLoader resourceLoader; // Добавляем ResourceLoader

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
            resourceLoader = new ResourceLoader(); // Инициализация ResourceLoader
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
                StatusTextBlock.Text = resourceLoader.GetString("OllamaNotInstalled") ?? "Ollama is not installed.";
                ShowInstallPanel(true, true);
                return;
            }

            // Запуск Ollama с динамическим портом
            StatusTextBlock.Text = resourceLoader.GetString("RunningOllamaAPI") ?? "Run Ollama API...";
            _currentPort = await OllamaManager.StartOllamaIfNeededAsync();
            if (!await OllamaManager.IsApiReadyAsync(_currentPort))
            {
                StatusTextBlock.Text = resourceLoader.GetString("OllamaApiUnavailable") ?? "Error: The Ollama API is unavailable. Check the installation.";
                ShowInstallPanel(true, true);
                return;
            }

            // Проверка модели
            if (string.IsNullOrEmpty(_appSettings.SelectedModel) || !await OllamaManager.IsModelInstalledAsync(_appSettings.SelectedModel))
            {
                StatusTextBlock.Text = resourceLoader.GetString("ModelNotSelectedOrInstalled") ?? "The model is not selected or installed. Select a model.";
                ShowInstallPanel(true, false);
                return;
            }

            // Инициализация
            try
            {
                _ai = new AIChatService(_appSettings.SelectedModel, _currentPort);
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"{resourceLoader.GetString("AIInitializationError") ?? "AI initialization error"}: {ex.Message}";
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
            StatusTextBlock.Text = resourceLoader.GetString("ReadyToWork") ?? "Ready to work.";
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
                    InstallTitleTextBlock.Text = resourceLoader.GetString("DownloadOllamaPrompt") ?? "Download Ollama to get started with AI";
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
                    InstallTitleTextBlock.Text = resourceLoader.GetString("SelectModelPrompt") ?? "Select the AI model to install";
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
            InstallOllamaStatusText.Text = resourceLoader.GetString("DownloadingOllamaGitHub") ?? "Download the fast version from GitHub (~1 GB)...";
            InstallProgressBar.Value = 0;

            var progress = new Progress<(double percent, string status)>(data =>
            {
                Debug.WriteLine($"Progress: {data.percent}% - {data.status}");
                var dispatcher = DispatcherQueue.GetForCurrentThread();
                dispatcher?.TryEnqueue(() =>
                {
                    InstallProgressBar.Value = data.percent;
                    InstallOllamaStatusText.Text = data.status;
                });
            });

            try
            {
                await OllamaManager.InstallOllamaAsync(progress);
                var dispatcher = DispatcherQueue.GetForCurrentThread();
                dispatcher?.TryEnqueue(() =>
                {
                    InstallOllamaStatusText.Text = resourceLoader.GetString("DownloadCompleted") ?? "The download is completed. Run the installer manually.";
                    LaunchOllamaButton.Visibility = Visibility.Visible;
                });
            }
            catch (Exception ex)
            {
                var dispatcher = DispatcherQueue.GetForCurrentThread();
                dispatcher?.TryEnqueue(() =>
                {
                    InstallOllamaStatusText.Text = $"{resourceLoader.GetString("Error") ?? "Error"}: {ex.Message}";
                });
                Debug.WriteLine($"Installation error: {ex.Message}");
            }
            finally
            {
                var dispatcher = DispatcherQueue.GetForCurrentThread();
                dispatcher?.TryEnqueue(() =>
                {
                    InstallOllamaPanelButton.IsEnabled = true;
                    InstallRussianOllamaPanelButton.IsEnabled = true;
                });
            }
        }

        private async void InstallRussianOllamaPanelButton_Click(object sender, RoutedEventArgs e)
        {
            InstallOllamaPanelButton.IsEnabled = false;
            InstallRussianOllamaPanelButton.IsEnabled = false;
            InstallOllamaStatusText.Visibility = Visibility.Visible;
            InstallProgressBar.Visibility = Visibility.Visible;
            InstallOllamaStatusText.Text = resourceLoader.GetString("DownloadingOllamaRussian") ?? "Downloading the Russian version from pwin11.ru (~1 GB)...";
            InstallProgressBar.Value = 0;

            var progress = new Progress<(double percent, string status)>(data =>
            {
                Debug.WriteLine($"Progress (Russian): {data.percent}% - {data.status}");
                var dispatcher = DispatcherQueue.GetForCurrentThread();
                dispatcher?.TryEnqueue(() =>
                {
                    InstallProgressBar.Value = data.percent;
                    InstallOllamaStatusText.Text = data.status;
                });
            });

            try
            {
                await OllamaManager.InstallRussianOllamaAsync(progress);
                var dispatcher = DispatcherQueue.GetForCurrentThread();
                dispatcher?.TryEnqueue(() =>
                {
                    InstallOllamaStatusText.Text = resourceLoader.GetString("DownloadCompleted") ?? "The download is completed. Run the installer manually.";
                    LaunchOllamaButton.Visibility = Visibility.Visible;
                });
            }
            catch (Exception ex)
            {
                var dispatcher = DispatcherQueue.GetForCurrentThread();
                dispatcher?.TryEnqueue(() =>
                {
                    InstallOllamaStatusText.Text = $"{resourceLoader.GetString("Error") ?? "Error"}: {ex.Message}";
                });
                Debug.WriteLine($"Installation error (Russian): {ex.Message}");
            }
            finally
            {
                var dispatcher = DispatcherQueue.GetForCurrentThread();
                dispatcher?.TryEnqueue(() =>
                {
                    InstallOllamaPanelButton.IsEnabled = true;
                    InstallRussianOllamaPanelButton.IsEnabled = true;
                });
            }
        }

        private async void InstallOllamaButton_Click(object sender, RoutedEventArgs e)
        {
            await OllamaManager.InstallOllamaAsync();
            StatusTextBlock.Text = resourceLoader.GetString("DownloadCompleted") ?? "The download is completed. Run the installer manually.";
        }

        private async void InstallModelButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = ModelSelector.SelectedItem as string;
            if (string.IsNullOrEmpty(selected)) return;

            var modelName = selected.Split(' ')[0];
            StatusTextBlock.Text = string.Format(resourceLoader.GetString("InstallingModel") ?? "Installing the model {0}...", modelName);
            InstallModelButton.IsEnabled = false;

            try
            {
                await OllamaManager.PullModelAsync(modelName);
                _appSettings.SelectedModel = modelName;
                SaveSettings();
                StatusTextBlock.Text = resourceLoader.GetString("ModelInstalled") ?? "The model is installed.";
                ShowInstallPanel(false);
                _ai = new AIChatService(_appSettings.SelectedModel, _currentPort);
                SendButton.IsEnabled = true;
                PromptTextBox.IsReadOnly = false;
                ChangeModelButton.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"{resourceLoader.GetString("InstallationError") ?? "Installation error"}: {ex.Message}";
            }
            finally
            {
                InstallModelButton.IsEnabled = true;
            }
        }

        private void ChangeModelButton_Click(object sender, RoutedEventArgs e)
        {
            ShowInstallPanel(true, false);
            StatusTextBlock.Text = resourceLoader.GetString("SelectNewModel") ?? "Select a new model to change.";
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
                    InstallOllamaStatusText.Text = $"{resourceLoader.GetString("LaunchError") ?? "Launch error"}: {ex.Message}";
                    Debug.WriteLine($"Launch error: {ex.Message}");
                }
            }
            else
            {
                InstallOllamaStatusText.Text = resourceLoader.GetString("InstallerNotFound") ?? "The installer file was not found.";
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

            var userMessage = new Message { Role = "User", Content = userText, Timestamp = DateTime.Now };
            _currentSession.Messages.Add(userMessage);
            ScrollToBottom();
            PromptTextBox.Text = "";
            TypingIndicator.Visibility = Visibility.Visible;
            StatusTextBlock.Text = resourceLoader.GetString("GeneratingResponse") ?? "Response generation...";

            Message aiMessage = null;

            try
            {
                var systemMessage = new OllamaMessage { Role = "system", Content = resourceLoader.GetString("SystemPrompt") ?? "Your name is eraAI, you are an experienced Windows 11 assistant, and you must respond in the language I am writing in. Don't write on markdown, write as plain text." };
                var ollamaHistory = new List<OllamaMessage> { systemMessage };
                ollamaHistory.AddRange(_currentSession.Messages
                    .Where(m => m.Role != "eraAI")
                    .Select(m => new OllamaMessage
                    {
                        Role = m.Role == "User" ? "user" : "assistant",
                        Content = m.Content
                    }));

                aiMessage = new Message { Role = "eraAI", Content = "", Timestamp = DateTime.Now };
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
                    aiMessage.Content = resourceLoader.GetString("EmptyAIResponse") ?? "[Error: An empty response from the AI. Check the model.]";
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"{resourceLoader.GetString("Error") ?? "Error"}: {ex.Message}";
                if (aiMessage != null)
                {
                    _currentSession.Messages.Remove(aiMessage);
                }
                aiMessage = new Message { Role = "eraAI", Content = $"{resourceLoader.GetString("AIResponseError") ?? "[Error"}: {ex.Message}]", Timestamp = DateTime.Now };
                _currentSession.Messages.Add(aiMessage);
            }
            finally
            {
                TypingIndicator.Visibility = Visibility.Collapsed;
                SaveSessions();
                SendButton.IsEnabled = true;
                PromptTextBox.IsReadOnly = false;
                PromptTextBox.Focus(FocusState.Programmatic);
                StatusTextBlock.Text = resourceLoader.GetString("ReadyToWork") ?? "Ready to work.";
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
                    Debug.WriteLine($"Error loading sessions: {ex.Message}");
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
                Debug.WriteLine($"Error saving sessions: {ex.Message}");
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
                    _currentPort = _appSettings.Port;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading settings: {ex.Message}");
                }
            }
        }

        private void SaveSettings()
        {
            try
            {
                _appSettings.Port = _currentPort;
                var json = JsonSerializer.Serialize(_appSettings, JsonOptions);
                File.WriteAllText(_settingsFile, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        private void NewSessionButton_Click(object sender, RoutedEventArgs e)
        {
            var newSession = new ChatSession { Title = $"{resourceLoader.GetString("Session") ?? "Session"} {DateTime.Now:HH:mm:ss}" };
            _sessions.Add(newSession);
            SessionsListView.SelectedItem = newSession;
            _currentSession = newSession;
            ChatListView.ItemsSource = _currentSession.Messages;
            StatusTextBlock.Text = resourceLoader.GetString("NewSessionCreated") ?? "A new session has been created.";
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
                return role == "User"
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