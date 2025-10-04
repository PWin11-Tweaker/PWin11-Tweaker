using System.Text.Json.Serialization;

namespace PWin11_Tweaker_s.Services.Models
{
    public class EraAIPrompt
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty; // 

        [JsonPropertyName("basePrompt")]
        public string BasePrompt { get; set; } = string.Empty; // 

        [JsonPropertyName("userInput")]
        public string UserInput {  get; set; } = string.Empty; // 

    }
}
