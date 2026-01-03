using PWin11_Tweaker_s.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PWin11_Tweaker_s.Services
{
    public static class OllamaManager
    {
        private static readonly string _ollamaUrl = "https://github.com/ollama/ollama/releases/download/v0.13.5/OllamaSetup.exe";
        private static readonly string _russianOllamaUrl = "https://pwin11.ru/ollama_list/0_13_5/OllamaSetup.exe"; 
        private static readonly string _ollamaPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Ollama");
        private static readonly string _programFilesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ollama");
        private static int _currentPort = 11434;

        public static bool IsOllamaInstalled()
        {
            var process = Process.GetProcessesByName("ollama");
            bool isInstalled = process.Length > 0 || Directory.Exists(_ollamaPath) || Directory.Exists(_programFilesPath);
            Debug.WriteLine($"Checking Ollama installation: LocalAppData={Directory.Exists(_ollamaPath)}, ProgramFiles={Directory.Exists(_programFilesPath)}, ProcessRunning={process.Length > 0}, Result={isInstalled}");
            return isInstalled;
        }

        public static async Task<string> InstallOllamaAsync(IProgress<(double percent, string status)> progress = null)
        {
            var installerPath = Path.Combine(AppContext.BaseDirectory, "OllamaSetup.exe");
            if (File.Exists(installerPath))
            {
                File.Delete(installerPath);
            }
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(120);
            long totalRead = 0;

            try
            {
                var response = await client.GetAsync(_ollamaUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1;
                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(installerPath, FileMode.Create, FileAccess.Write, FileShare.None);
                var buffer = new byte[64 * 1024];
                long lastReported = 0;
                DateTime lastTime = DateTime.Now;

                int bytesRead;
                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalRead += bytesRead;

                    if (totalBytes > 0 && progress != null)
                    {
                        double percent = (double)totalRead / totalBytes * 100;
                        if (totalRead - lastReported >= 1024 * 1024 || percent >= 100) // Update every 1MB or at 100%
                        {
                            var currentTime = DateTime.Now;
                            var timeElapsed = (currentTime - lastTime).TotalSeconds;
                            double speed = timeElapsed > 0 ? (totalRead - lastReported) / timeElapsed / (1024 * 1024) : 0; // MB/s
                            long downloaded = totalRead / (1024 * 1024); // MB
                            long remaining = (totalBytes - totalRead) / (1024 * 1024); // MB

                            string status = $"Downloaded: {downloaded} MB, Left: {remaining} MB, Speed: {speed:F2} MB/s";
                            progress.Report((percent, status));

                            lastReported = totalRead;
                            lastTime = currentTime;
                        }
                        await Task.Yield(); // Yield to allow UI updates
                    }
                }

                if (totalBytes > 0 && progress != null)
                {
                    progress.Report((100, $"Downloaded: {totalRead / (1024 * 1024)} MB, Left: 0 MB, Speed: 0 MB/s"));
                }

                var fileInfo = new FileInfo(installerPath);
                if (fileInfo.Length < 900 * 1024 * 1024)
                {
                    throw new Exception("Installer file is corrupted or too small (expected ~1 GB).");
                }

                Debug.WriteLine($"Installer downloaded at: {installerPath}");

                // Launch installer in background (non-blocking) using UseShellExecute so installer UI can run elevated if needed
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = installerPath,
                        Arguments = "/SILENT",
                        UseShellExecute = true,
                        CreateNoWindow = true,
                        Verb = "runas"
                    };
                    var proc = Process.Start(startInfo);
                    progress?.Report((100, "InstallerLaunched"));

                    if (proc != null)
                    {
                        // Wait for installer to finish in background and notify progress
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await proc.WaitForExitAsync();
                                progress?.Report((100, "InstallerFinished"));
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error waiting for installer exit: {ex.Message}");
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to launch installer: {ex.Message}");
                    // still return installer path so UI can offer to launch manually
                }

                return installerPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error downloading Ollama: {ex.Message}");
                throw new Exception($"Failed to download installer: {ex.Message}");
            }
        }

        public static async Task<string> InstallRussianOllamaAsync(IProgress<(double percent, string status)> progress = null)
        {
            var installerPath = Path.Combine(AppContext.BaseDirectory, "OllamaSetup.exe");
            if (File.Exists(installerPath))
            {
                File.Delete(installerPath);
            }
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(120);
            long totalRead = 0;

            try
            {
                var response = await client.GetAsync(_russianOllamaUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1;
                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(installerPath, FileMode.Create, FileAccess.Write, FileShare.None);
                var buffer = new byte[64 * 1024];
                long lastReported = 0;
                DateTime lastTime = DateTime.Now;

                int bytesRead;
                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalRead += bytesRead;

                    if (totalBytes > 0 && progress != null)
                    {
                        double percent = (double)totalRead / totalBytes * 100;
                        if (totalRead - lastReported >= 1024 * 1024 || percent >= 100)
                        {
                            var currentTime = DateTime.Now;
                            var timeElapsed = (currentTime - lastTime).TotalSeconds;
                            double speed = timeElapsed > 0 ? (totalRead - lastReported) / timeElapsed / (1024 * 1024) : 0; // MB/s
                            long downloaded = totalRead / (1024 * 1024); // MB
                            long remaining = (totalBytes - totalRead) / (1024 * 1024); // MB

                            string status = $"Downloaded: {downloaded} MB, Left: {remaining} MB, Speed: {speed:F2} MB/s";
                            progress.Report((percent, status));

                            lastReported = totalRead;
                            lastTime = currentTime;
                        }
                        await Task.Yield();
                    }
                }

                if (totalBytes > 0 && progress != null)
                {
                    progress.Report((100, $"Downloaded: {totalRead / (1024 * 1024)} MB, Left: 0 MB, Speed: 0 MB/s"));
                }

                var fileInfo = new FileInfo(installerPath);
                if (fileInfo.Length < 900 * 1024 * 1024)
                {
                    throw new Exception("Installer file is corrupted or too small (expected ~1 GB).");
                }

                Debug.WriteLine($"Installer downloaded at: {installerPath}");

                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = installerPath,
                        Arguments = "/S",
                        UseShellExecute = true,
                        CreateNoWindow = true,
                        Verb = "runas"
                    };
                    Process.Start(startInfo);
                    progress?.Report((100, "Installer launched"));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to launch installer (Russian): {ex.Message}");
                }

                return installerPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error downloading Ollama (Russian): {ex.Message}");
                throw new Exception($"Failed to download installer (Russian): {ex.Message}");
            }
        }

        public static async Task UninstallOllamaAsync()
        {
            Debug.WriteLine("Starting Ollama uninstallation...");
            try
            {
                // Шаг 1: Проверка и удаление моделей
                string ollamaExe = Path.Combine(_ollamaPath, "ollama.exe");
                if (File.Exists(ollamaExe))
                {
                    Debug.WriteLine("Checking and removing models...");
                    using (var process = new Process())
                    {
                        process.StartInfo = new ProcessStartInfo
                        {
                            FileName = ollamaExe,
                            Arguments = "rm gemma3:1b",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };
                        process.Start();
                        await process.WaitForExitAsync();
                        Debug.WriteLine($"Removed model gemma3:1b. Exit code: {process.ExitCode}");
                    }

                    using (var process = new Process())
                    {
                        process.StartInfo = new ProcessStartInfo
                        {
                            FileName = ollamaExe,
                            Arguments = "rm phi3:latest",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };
                        process.Start();
                        await process.WaitForExitAsync();
                        Debug.WriteLine($"Removed model phi3:latest. Exit code: {process.ExitCode}");
                    }
                }
                else
                {
                    Debug.WriteLine($"Ollama executable not found at: {ollamaExe}");
                }

                // Шаг 2: Удаление Ollama через unins000.exe
                string uninstallPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "Local", "Programs", "Ollama", "unins000.exe");
                if (File.Exists(uninstallPath))
                {
                    Debug.WriteLine($"Found uninstaller at: {uninstallPath}");
                    using (var process = new Process())
                    {
                        process.StartInfo = new ProcessStartInfo
                        {
                            FileName = uninstallPath,
                            Arguments = "/SILENT",
                            Verb = "runas",
                            UseShellExecute = true,
                            CreateNoWindow = true
                        };
                        process.Start();
                        bool exited = process.WaitForExit(60000); // Тайм-аут 60 секунд
                        if (!exited || process.ExitCode != 0)
                        {
                            Debug.WriteLine($"Uninstaller failed or timed out with exit code {process.ExitCode}");
                            throw new Exception("Uninstaller failed.");
                        }
                        Debug.WriteLine("Uninstallation via unins000.exe completed.");
                    }
                }
                else
                {
                    Debug.WriteLine($"Uninstaller not found at: {uninstallPath}");
                }

                Debug.WriteLine("Ollama uninstallation completed successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Uninstallation failed: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw new Exception($"Failed to uninstall Ollama: {ex.Message}");
            }
        }

        private static async Task<string[]> GetModelListAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                var response = await client.GetAsync($"http://localhost:{_currentPort}/api/tags");
                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"API /api/tags returned {response.StatusCode}, no models to delete.");
                    return Array.Empty<string>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                var models = jsonDoc.RootElement
                    .GetProperty("models")
                    .EnumerateArray()
                    .Select(m => m.GetProperty("name").GetString())
                    .ToArray();
                Debug.WriteLine($"Found models: {string.Join(", ", models)}");
                return models;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting model list: {ex.Message}");
                return Array.Empty<string>();
            }
        }

        private static async Task RemoveModelAsync(string modelName)
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                var requestBody = new { name = modelName };
                var content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");
                var response = await client.DeleteAsync($"http://localhost:{_currentPort}/api/delete?model={modelName}");
                response.EnsureSuccessStatusCode();
                Debug.WriteLine($"Removed model {modelName}.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error removing model {modelName}: {ex.Message}");
                throw;
            }
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

        public static async Task<int> StartOllamaIfNeededAsync()
        {
            if (Process.GetProcessesByName("ollama").Length == 0)
            {
                _currentPort = FindFreePort();
                var ollamaExe = Path.Combine(_ollamaPath, "ollama.exe");
                if (!File.Exists(ollamaExe))
                {
                    ollamaExe = Path.Combine(_programFilesPath, "ollama.exe");
                }
                if (File.Exists(ollamaExe))
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = ollamaExe,
                            Arguments = $"serve",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        }
                    };
                    process.Start();
                    await Task.Delay(5000);
                }
            }
            return _currentPort;
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
            int endPort = 11500;
            for (int port = startPort; port <= endPort; port++)
            {
                if (IsPortAvailable(port))
                    return port;
            }
            throw new Exception("No available ports in range 11434-11500");
        }

        private static bool IsPortAvailable(int port)
        {
            try
            {
                using var tcpListener = new TcpListener(System.Net.IPAddress.Loopback, port);
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