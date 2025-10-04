using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;  // Для ContentDialog
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace PWin11_Tweaker_s.Services
{
    public class OllamaInstaller
    {
        private const string OllamaInstallerUrl = "https://ollama.com/download/OllamaSetup.exe";  // Актуальный URL на 2025 (проверь на ollama.com)
        private const string ModelName = "microsoft/Phi-3-mini-4k-instruct";  // Твоя модель

        public async Task<bool> EnsureOllamaInstalledAsync(XamlRoot xamlRoot)
        {
            if (await IsOllamaRunningAsync())
            {
                return true;  // Уже работает
            }

            // Диалог: Предложить установку
            var dialog = new ContentDialog
            {
                Title = "Ollama не найден",
                Content = "EraAI требует локальный AI-сервер Ollama. Установить автоматически? (Требует ~100 MB + модель ~2 GB)",
                PrimaryButtonText = "Да, установить",
                CloseButtonText = "Нет, отключить",
                XamlRoot = xamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return false;  // Пользователь отказался
            }

            // Скачивание и установка
            try
            {
                string tempPath = Path.Combine(Path.GetTempPath(), "OllamaSetup.exe");
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(OllamaInstallerUrl);
                    response.EnsureSuccessStatusCode();
                    await File.WriteAllBytesAsync(tempPath, await response.Content.ReadAsByteArrayAsync());
                }

                // Silent установка (/S для NSIS)
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = tempPath,
                        Arguments = "/S",  // Silent mode
                        UseShellExecute = true,
                        Verb = "runas"  // Запуск от админа, если нужно
                    }
                };
                process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new Exception("Установка Ollama провалилась.");
                }

                // Запуск Ollama и скачивание модели
                await StartOllamaAndPullModelAsync();

                return true;
            }
            catch (Exception ex)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Ошибка установки",
                    Content = $"Не удалось установить Ollama: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = xamlRoot
                };
                await errorDialog.ShowAsync();
                return false;
            }
        }

        private async Task<bool> IsOllamaRunningAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync("http://localhost:11434/api/tags");
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        private async Task StartOllamaAndPullModelAsync()
        {
            // Запуск Ollama как сервис (предполагаем, что installer добавил в PATH)
            Process.Start("ollama", "serve");  // Запуск сервера
            await Task.Delay(5000);  // Ждём запуска

            // Скачивание модели
            Process.Start("ollama", $"pull {ModelName}");  // Это запустит скачивание
            // Можно добавить прогресс, но для простоты ждём (или мониторим)
            await Task.Delay(10000);  // Пример задержки; в реальности мониторь API
        }

    }
}
