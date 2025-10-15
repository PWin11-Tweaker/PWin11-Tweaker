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
        private int _port;

        public AIChatService(string model, int port)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _port = port;
            _ollama = new OllamaApiClient($"http://localhost:{_port}");
        }

        public async Task<string> AskAsync(string userMessage)
        {
            var messages = new List<OllamaSharp.Models.Chat.Message>
            {
                new OllamaSharp.Models.Chat.Message(ChatRole.System, "Your name is eraAI, you are an experienced Windows 11 assistant, and you must respond in the language I am writing in"),
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
                    yield return chunk.Message.Content;
                }
            }
        }
    }
}