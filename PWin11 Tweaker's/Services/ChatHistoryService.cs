using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PWin11_Tweaker_s.Services.Models;
using Windows.ApplicationModel.VoiceCommands;

namespace PWin11_Tweaker_s.Services
{
    public class ChatHistoryService
    {
        private readonly string _historyPath;

        public ChatHistoryService()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, "PWin11Tweaker");
            Directory.CreateDirectory(appFolder); // Create Folder.
            _historyPath = Path.Combine(appFolder, "chathistory.json");
        }


        public ChatHistory LoadHistory()
        {
            if (!File.Exists(_historyPath))
            {
                return new ChatHistory(); // Empty History
            }

            string json = File.ReadAllText(_historyPath);
            return JsonSerializer.Deserialize<ChatHistory>(json) ?? new ChatHistory();
        }

        public void SaveHistory(ChatHistory history)
        {
            string json = JsonSerializer.Serialize(history, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_historyPath, json);
        }

        public void AddMessageToCurrentSession(ChatHistory history, Message message)
        {
            var currentSession = GetCurrentSession(history);
            if (currentSession != null)
            {
                currentSession.Message.Add(message);
                if (currentSession.Message.Count == 1)
                {
                    currentSession.GenerateTitle();
                }
                SaveHistory(history);
            }
        }

        public void CreateNewSession(ChatHistory history)
        {
            var newSession = new ChatSession();
            history.Sessions.Add(newSession);
            history.CurrentSessionId = newSession.Id;
            SaveHistory(history);
        }

        public void SwitchToSession(ChatHistory history, string sessionId)
        {
            if (history.Sessions.Any(s => s.Id == sessionId))
            {
                history.CurrentSessionId = sessionId;
                SaveHistory(history);
            }
        }

        public void DeleteSession(ChatHistory history, string sessionId)
        {
            var sessionToDelete = history.Sessions.FirstOrDefault(s => s.Id == sessionId);
            if (sessionToDelete != null)
            {
                history.Sessions.Remove(sessionToDelete);
                if (history.CurrentSessionId == sessionId)
                {
                    history.CurrentSessionId = history.Sessions.FirstOrDefault()?.Id ?? string.Empty;
                }
                SaveHistory(history);
            }
        }

        private ChatSession? GetCurrentSession(ChatHistory history)
        {
            return history.Sessions.FirstOrDefault(s => s.Id == history.CurrentSessionId);
        }


    }
}
