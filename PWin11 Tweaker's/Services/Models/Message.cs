using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PWin11_Tweaker_s.Services.Models
{
    public class Message
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty; // 

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
