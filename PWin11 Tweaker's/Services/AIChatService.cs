using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OllamaSharp;
using OllamaSharp.Models.Chat;

namespace PWin11_Tweaker_s.Services
{
    public class AIChatService
    {
        private readonly OllamaApiClient _ollama;
        private readonly string _model;
        private readonly string Host = "http://localhost:11434";

        public AIChatService(string model = "gemma2:2b")
        {
            _model = model;
            _ollama = new OllamaApiClient(Host);
        }

        public async Task<string> AskAsync(string userMessage)
        {
            var messages = new List<OllamaSharp.Models.Chat.Message>
            {
                new OllamaSharp.Models.Chat.Message(ChatRole.System, "Ты эксперт Windows 11."),
                new OllamaSharp.Models.Chat.Message(ChatRole.User, userMessage)
            };

            var request = new ChatRequest
            {
                Model = _model,
                Messages = messages
            };

            var answer = await _ollama.ChatAsync(request).StreamToEndAsync();
            return answer.Message?.Content ?? string.Empty;
        }

        public async IAsyncEnumerable<string> StreamAnswerAsync(IEnumerable<OllamaSharp.Models.Chat.Message> messages)
        {
            var request = new ChatRequest
            {
                Model = _model,
                Messages = messages.ToList()
            };

            await foreach (var chunk in _ollama.ChatAsync(request))
            {
                if (chunk.Message?.Content != null && !string.IsNullOrEmpty(chunk.Message.Content))
                {
                    yield return chunk.Message.Content; // Возвращаем дельту для инкрементального обновления
                }
            }
        }
    }
}