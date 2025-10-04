using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PWin11_Tweaker_s.Services.Models
{
    public class ChatHistory
    {
        [JsonPropertyName("session")]
        public List<ChatSession> Sessions { get; set; } = new List<ChatSession>(); //

        [JsonPropertyName("currentSessionId")]
        public string CurrentSessionId { get; set; } = string.Empty;
    }
}
