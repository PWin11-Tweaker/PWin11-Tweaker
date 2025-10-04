using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace PWin11_Tweaker_s.Services.Models
{
    public class ChatSession
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("title")]
        public string Title { get; set; } = "New Chat";

        [JsonPropertyName("messages")]
        public List<Message> Message { get; set; } = new List<Message>();

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public void GenerateTitle()
        {
            var firstUserMessage = Message.FirstOrDefault(m => m.Role == "user");
            if (firstUserMessage != null)
            {
                Title = firstUserMessage.Content.Length > 50
                    ? firstUserMessage.Content.Substring(0, 50) + "..."
                    : firstUserMessage.Content;
            }
        }

    }
}
