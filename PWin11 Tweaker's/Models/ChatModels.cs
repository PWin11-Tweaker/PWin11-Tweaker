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

        // Конструктор по умолчанию (для object initializer)
        public Message() { }

        // Конструктор для копирования
        public Message(Message other)
        {
            Role = other.Role;
            Content = other.Content;
            Timestamp = other.Timestamp;
        }
    }

    // DTO для сериализации (только простые свойства)
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

        // Конструктор по умолчанию
        public ChatSession() { }

        // Конструктор из DTO (для загрузки)
        public ChatSession(ChatSessionDto dto)
        {
            Title = dto.Title;
            Messages = new ObservableCollection<Message>(dto.Messages.Select(m => new Message(m)));
        }

        // Метод для создания DTO (для сохранения)
        public ChatSessionDto ToDto()
        {
            return new ChatSessionDto
            {
                Title = Title,
                Messages = Messages.Select(m => new Message(m)).ToList()
            };
        }

        // Метод для копирования сообщений (deep copy)
        public void CopyMessagesFrom(ChatSession other)
        {
            Messages.Clear();
            foreach (var msg in other.Messages)
            {
                Messages.Add(new Message(msg));
            }
        }

        // Метод для добавления сообщения с копированием
        public void AddCopiedMessage(Message originalMessage)
        {
            Messages.Add(new Message(originalMessage));
        }
    }
}