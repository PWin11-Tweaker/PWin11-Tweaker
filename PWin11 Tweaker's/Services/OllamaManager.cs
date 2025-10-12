using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PWin11_Tweaker_s.Services
{
    public static class OllamaManager
    {
        private static readonly string _ollamaUrl = "https://ollama.ai/download/OllamaSetup.exe";
        private static readonly string _ollamaPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Ollama");
        private static int _currentPort = 11434; // Начальный порт

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

        public static async Task<bool> IsModelInstalledAsync(string modelName)
        {
            try
            {
                using var http = new HttpClient();
                var res = await http.GetAsync($"http://localhost:{_currentPort}/api/tags");
                if (!res.IsSuccessStatusCode) return false;
                var text = await res.Content.ReadAsStringAsync();
                return text.Contains(modelName);
            }
            catch
            {
                return false;
            }
        }

        public static async Task PullModelAsync(string modelName)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ollama",
                    Arguments = $"pull {modelName}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            await process.WaitForExitAsync();
        }

        public static async Task<int> StartOllamaIfNeededAsync() // Изменено на возврат int
        {
            if (Process.GetProcessesByName("ollama").Length == 0)
            {
                _currentPort = FindFreePort();
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ollama",
                        Arguments = $"serve --port {_currentPort}",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };
                process.Start();
                await Task.Delay(5000); // Дать время на запуск
            }
            return _currentPort; // Возвращаем текущий порт
        }

        public static async Task<bool> IsApiReadyAsync(int port, int timeoutSeconds = 10)
        {
            var startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalSeconds < timeoutSeconds)
            {
                try
                {
                    using var http = new HttpClient();
                    var res = await http.GetAsync($"http://localhost:{port}/api/tags");
                    if (res.IsSuccessStatusCode)
                        return true;
                }
                catch { }
                await Task.Delay(500);
            }
            return false;
        }

        private static int FindFreePort()
        {
            int startPort = 11434;
            int endPort = 11500; // Диапазон портов для поиска
            for (int port = startPort; port <= endPort; port++)
            {
                if (IsPortAvailable(port))
                    return port;
            }
            throw new Exception("Нет доступных портов в диапазоне 11434-11500");
        }

        private static bool IsPortAvailable(int port)
        {
            try
            {
                using var tcpListener = new TcpListener(IPAddress.Loopback, port);
                tcpListener.Start();
                tcpListener.Stop();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}