using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Reflection;

class Program
{
    private static readonly string zipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "release.zip");
    private static readonly string tempUnzipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tempUpdate");
    private static string localVersion; // Будет устанавливаться из аргументов
    private static string serverVersion;

    static void Main(string[] args)
    {
        Console.WriteLine("PWin11 Tweaker Updater started.");

        // Извлекаем версию из аргументов
        localVersion = "1.12.6"; // Значение по умолчанию
        foreach (var arg in args)
        {
            if (arg.StartsWith("--tweaker-version="))
            {
                localVersion = arg.Split('=')[1];
                break;
            }
        }
        Console.WriteLine($"Local Version (PWin11 Tweaker): {localVersion}");

        CheckServerVersionAsync().Wait();

        if (args.Length > 0 && args[0] == "--update")
        {
            if (serverVersion != null && serverVersion != localVersion)
            {
                Console.WriteLine($"New version {serverVersion} available. Starting update...");
                DownloadAndInstallUpdateAsync().Wait();
                Console.WriteLine("Update completed. Restarting application...");
                Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PWin11 Tweaker's.exe"));
            }
            else
            {
                Console.WriteLine("You are on the latest version or no update needed.");
            }
        }
        else
        {
            Console.WriteLine("No update command. Use '--update' to start the update process.");
        }

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
        Environment.Exit(0);
    }

    private static async Task CheckServerVersionAsync()
    {
        try
        {
            using var client = new HttpClient();
            var response = await client.GetStringAsync("https://raw.githubusercontent.com/PWin11-Tweaker/PWin11-Tweaker/refs/heads/main/PWin11%20Tweaker's/Assets/versionServer.xml");
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(response);
            serverVersion = xmlDoc.SelectSingleNode("//version")?.InnerText;
            Console.WriteLine($"Server Version: {serverVersion ?? "Not Available"}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching server version: {ex.Message}");
            Debug.WriteLine($"CheckServerVersionAsync: Error - {ex.Message}");
        }
    }

    private static async Task DownloadAndInstallUpdateAsync()
    {
        Console.WriteLine("Downloading update...");

        using var client = new HttpClient();
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://github.com/PWin11-Tweaker/PWin11-Tweaker/releases/latest/download/release.zip");
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await stream.CopyToAsync(fileStream, 81920);

            Console.WriteLine("Download completed. Extracting...");

            if (Directory.Exists(tempUnzipPath)) Directory.Delete(tempUnzipPath, true);
            Directory.CreateDirectory(tempUnzipPath);

            using var zip = ZipFile.OpenRead(zipPath);
            foreach (var entry in zip.Entries)
            {
                string extractPath = Path.Combine(tempUnzipPath, entry.FullName);
                string extractDir = Path.GetDirectoryName(extractPath);
                if (!string.IsNullOrEmpty(extractDir) && !Directory.Exists(extractDir))
                {
                    Directory.CreateDirectory(extractDir);
                }
                if (entry.Length > 0 || !entry.FullName.EndsWith("/"))
                {
                    entry.ExtractToFile(extractPath, true);
                }
            }

            Console.WriteLine("Extraction completed. Replacing files...");

            await Task.Delay(2000); // Дополнительная задержка для освобождения файлов
            foreach (var process in Process.GetProcessesByName("PWin11 Tweaker's"))
            {
                try
                {
                    Console.WriteLine($"Closing process: {process.Id}");
                    process.Kill();
                    process.WaitForExit(5000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to kill process {process.Id}: {ex.Message}");
                }
            }

            string targetDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] filesToCopy = Directory.GetFiles(tempUnzipPath, "*.*", SearchOption.AllDirectories);

            foreach (string file in filesToCopy)
            {
                string relativePath = Path.GetRelativePath(tempUnzipPath, file);
                string destPath = Path.Combine(targetDir, relativePath);
                string destDir = Path.GetDirectoryName(destPath);

                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                if (File.Exists(file))
                {
                    try
                    {
                        if (File.Exists(destPath))
                        {
                            File.SetAttributes(destPath, FileAttributes.Normal);
                            File.Delete(destPath);
                            Console.WriteLine($"Deleted existing file: {destPath}");
                        }
                        File.Copy(file, destPath, true);
                        Console.WriteLine($"Copied file: {file} to {destPath}");
                    }
                    catch (UnauthorizedAccessException uae)
                    {
                        Console.WriteLine($"Access denied copying {file} to {destPath}: {uae.Message}");
                        Environment.Exit(1);
                    }
                    catch (IOException ioe)
                    {
                        Console.WriteLine($"IO error copying {file} to {destPath}: {ioe.Message}");
                        Environment.Exit(1);
                    }
                }
            }

            Console.WriteLine("Update installed. Cleaning up...");
            File.Delete(zipPath);
            Directory.Delete(tempUnzipPath, true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Update error: {ex.Message}");
            Debug.WriteLine($"DownloadAndInstallUpdateAsync: Error - {ex.Message}\nStackTrace: {ex.StackTrace}");
            Environment.Exit(1);
        }
    }
}