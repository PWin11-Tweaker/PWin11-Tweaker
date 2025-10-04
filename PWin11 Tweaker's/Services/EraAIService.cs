using System.Collections.Generic;
using System;
using System.Linq;
using OllamaSharp;
using PWin11_Tweaker_s.Services.Models;
using System.Threading.Tasks;
using Microsoft.UI.Xaml; // UI

namespace PWin11_Tweaker_s.Services
{
    public class EraAIService
    {
        private readonly OllamaApiClient _client;
        private readonly OllamaInstaller _installer;
        private readonly ChatHistoryService _chatHistoryService;
        private const string ModelName = "microsoft/Phi-3-mini-4k-instruct";

        public EraAIService(OllamaInstaller installer, ChatHistoryService chatHistoryService)
        {
            _client = new OllamaApiClient("http://localhost:11434");
            _installer = installer;
            _chatHistoryService = chatHistoryService;
        }

        public async Task<string> GenerateResponseAsync(string prompt, XamlRoot xamlRoot)
        {
            // Проверка и установка Ollama
            bool isReady = await _installer.EnsureOllamaInstalledAsync(xamlRoot);
            if (!isReady)
            {
                return "EraAI отключен: Ollama не установлен.";
            }

            // Load History
            var history = _chatHistoryService.LoadHistory();
            if (string.IsNullOrEmpty(history.CurrentSessionId))
            {
                _chatHistoryService.CreateNewSession(history);
            }

            // Add message user in history
            var userMessage = new Message { Role = "user", Content = prompt };
            _chatHistoryService.AddMessageToCurrentSession(history, userMessage);

        }

        public async Task<bool> IsOllamaReadyAsync()
        {
            try
            {
                await _client.GenerateAsync(new GenerateRequest { Model = ModelName, Prompt = "test" });
                return true;
            }
            catch
            {
                return false;
            }
        }





    }
}
