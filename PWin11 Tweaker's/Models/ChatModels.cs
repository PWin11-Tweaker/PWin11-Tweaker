using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;

namespace PWin11_Tweaker_s.Models
{
    public class Message
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public Message() { }

        public Message(Message other)
        {
            Role = other.Role;
            Content = other.Content;
            Timestamp = other.Timestamp;
        }
    }

    public class ChatSessionDto
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<Message> Messages { get; set; } = new List<Message>();
    }

    public class ChatSession
    {
        [JsonIgnore]
        public string Title { get; set; } = string.Empty;

        [JsonIgnore]
        public ObservableCollection<Message> Messages { get; set; } = new ObservableCollection<Message>();

        public ChatSession() { }

        public ChatSession(ChatSessionDto dto)
        {
            Title = dto.Title;
            Messages = new ObservableCollection<Message>(dto.Messages.Select(m => new Message(m)));
        }

        public ChatSessionDto ToDto()
        {
            return new ChatSessionDto
            {
                Title = Title,
                Messages = Messages.Select(m => new Message(m)).ToList()
            };
        }

        public void CopyMessagesFrom(ChatSession other)
        {
            Messages.Clear();
            foreach (var msg in other.Messages)
            {
                Messages.Add(new Message(msg));
            }
        }

        public void AddCopiedMessage(Message originalMessage)
        {
            Messages.Add(new Message(originalMessage));
        }
    }

    // Новый класс для настроек
    public class AppSettings
    {
        [JsonPropertyName("selectedModel")]
        public string SelectedModel { get; set; } = string.Empty;

        [JsonPropertyName("port")]
        public int Port { get; set; } = 11434;
    }
}