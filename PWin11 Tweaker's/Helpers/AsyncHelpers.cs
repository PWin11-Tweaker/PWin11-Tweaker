using Microsoft.UI.Dispatching;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PWin11_Tweaker_s.Helpers
{
    internal static class AsyncHelpers
    {
        public static Task SetRegistryValueAsync(RegistryHive hive, string subKeyPath, string name, object value, RegistryValueKind kind, CancellationToken ct)
        {
            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
                using var key = baseKey.CreateSubKey(subKeyPath);
                if (key == null) throw new InvalidOperationException($"Не удаётся открыть или создать ключ реестра: {subKeyPath}");
                key.SetValue(name, value, kind);
            }, ct);
        }

        public static Task DeleteRegistryKeyAsync(RegistryHive hive, string subKeyPath, CancellationToken ct)
        {
            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
                try
                {
                    baseKey.DeleteSubKeyTree(subKeyPath, false);
                }
                catch (ArgumentException)
                {
                   // ignore
                }
            }, ct);
        }

        public static async Task<int> RunProcessAsync(ProcessStartInfo psi, int timeoutMs, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            using var process = Process.Start(psi) ?? throw new InvalidOperationException("Не удалось запустить процесс.");

            var waitTask = process.WaitForExitAsync(ct);
            var delayTask = Task.Delay(timeoutMs, ct);

            var finished = await Task.WhenAny(waitTask, delayTask).ConfigureAwait(false);
            if (finished == delayTask)
            {
                try { process.Kill(true); } catch { }
                throw new TimeoutException("Процесс не завершился в отведённое время.");
            }

            await waitTask.ConfigureAwait(false);
            return process.ExitCode;
        }

        public static async Task RestartExplorerAsync(int timeoutMs, CancellationToken ct)
        {
            // Kill explorer
            var killInfo = new ProcessStartInfo
            {
                FileName = "taskkill",
                Arguments = "/f /im explorer.exe",
                UseShellExecute = true,
                CreateNoWindow = true
            };

            try
            {
                await RunProcessAsync(killInfo, timeoutMs, ct).ConfigureAwait(false);
            }
            catch (TimeoutException) { }
            catch (Exception) { }

            // Start explorer
            var startInfo = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                UseShellExecute = true,
                CreateNoWindow = true
            };

            try
            {
                await RunProcessAsync(startInfo, timeoutMs, ct).ConfigureAwait(false);
            }
            catch (Exception) { }
        }

        public static void RunOnUI(DispatcherQueue dispatcher, Action action)
        {
            if (dispatcher != null)
            {
                try { dispatcher.TryEnqueue(() => action()); }
                catch { action(); }
            }
            else
            {
                action();
            }
        }
    }
}
