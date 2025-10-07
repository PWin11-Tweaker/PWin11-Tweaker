using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace PWin11_Tweaker_s.Services
{
    public static class OllamaManager
    {
        private static readonly string _ollamaUrl = "https://ollama.ai/download/OllamaSetup.exe";
        private static readonly string _ollamaPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Ollama");
        private static readonly string _modelName = "gemma2:2b";

        public static bool IsOllamaInstalled()
        {
            var process = Process.GetProcessesByName("ollama");
            return process.Length > 0 || Directory.Exists(_ollamaPath);
        }

        public static async Task InstallOllamaAsync()
        {
            var installerPath = Path.Combine(Path.GetTempPath(), "OllamaSetup.exe");
            using var client = new HttpClient();
            var data = await client.GetByteArrayAsync(_ollamaUrl);
            await File.WriteAllBytesAsync(installerPath, data);

            Process.Start(new ProcessStartInfo(installerPath) { UseShellExecute = true });
        }

        public static async Task<bool> IsModelInstalledAsync()
        {
            try
            {
                using var http = new HttpClient();
                var res = await http.GetAsync("http://localhost:11434/api/tags");
                if (!res.IsSuccessStatusCode) return false;
                var text = await res.Content.ReadAsStringAsync();
                return text.Contains(_modelName);
            }
            catch
            {
                return false;
            }
        }

        public static async Task PullModelAsync()
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ollama",
                    Arguments = $"pull {_modelName}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            await process.WaitForExitAsync();
        }
        public static async Task StartOllamaIfNeededAsync()
        {
            if (Process.GetProcessesByName("ollama").Length == 0)
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ollama",
                        Arguments = "serve",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };
                process.Start();
                await Task.Delay(3000);
            }
        }

        public static async Task<bool> IsApiReadyAsync(int timeoutSeconds = 10)
        {
            var startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalSeconds < timeoutSeconds)
            {
                try
                {
                    using var http = new HttpClient();
                    var res = await http.GetAsync("http://localhost:11434/api/tags");
                    if (res.IsSuccessStatusCode)
                        return true;
                }
                catch { }
                await Task.Delay(500);
            }
            return false;
        }

    }
}
