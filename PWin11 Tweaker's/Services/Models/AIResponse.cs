using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PWin11_Tweaker_s.Services.Models
{
    public class AIResponse
    {
        [JsonPropertyName("response")]
        public string Response { get; set; } = string.Empty; //

        [JsonPropertyName("done")]
        public bool Done { get; set; } //

        [JsonPropertyName("context")]
        public List<int>? Context { get; set; } //

    }
}
