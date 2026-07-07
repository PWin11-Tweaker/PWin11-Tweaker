using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using PWin11_Tweaker_s.Models;
using PWin11_Tweaker_s.Services;
using System.Windows;
using System.Xml;
using WinUI3Localizer;

namespace PWin11_Tweaker_s
{
    public sealed partial class SettingsPage : Page
    {
        // Keys must match the literal folder names under "Strings\".
        private static readonly Dictionary<string, string> LanguageDisplayNames = new()
        {
            ["en-US"] = "English",
            ["ru_RU"] = "Русский",
            ["fr-FR"] = "Français",
        };

        public SettingsPage()
        {
            this.InitializeComponent();
            Loaded += SettingsPage_Loaded;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            bool isOllamaInstalled = OllamaManager.IsOllamaInstalled();
            UninstallOllamaButton.Visibility = isOllamaInstalled ? Visibility.Visible : Visibility.Collapsed;
            Debug.WriteLine($"Ollama installed: {isOllamaInstalled}, Button Visibility: {UninstallOllamaButton.Visibility}");

            UpdateLanguageButtonText();
        }

        private void UpdateLanguageButtonText()
        {
            string currentLanguage = Localizer.Get().GetCurrentLanguage();
            LanguageButtonText.Text = LanguageDisplayNames.TryGetValue(currentLanguage, out string displayName)
                ? displayName
                : currentLanguage;
        }

        private async void LanguageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuFlyoutItem item || item.Tag is not string langTag)
            {
                return;
            }

            if (Localizer.Get().GetCurrentLanguage() == langTag)
            {
                return;
            }

            try
            {
                // Re-localizes every live element bound with l:Uids.Uid immediately,
                // no app restart needed.
                await Localizer.Get().SetLanguage(langTag);

                // Persist the choice so it's restored as the default on next launch.
                LocalizationManager.CurrentLanguage = langTag;

                Debug.WriteLine($"SettingsPage: Language changed to {langTag}.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SettingsPage: Error changing language: {ex.Message}");
            }

            UpdateLanguageButtonText();
        }

        private void OpenUpdaterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Инициируем закрытие всего приложения
                Application.Current.Exit(); // Корректное закрытие приложения в WinUI
                Debug.WriteLine("PWin11 Tweaker's application closed.");

                // Ждём небольшую задержку, чтобы приложение завершило работу
                Task.Delay(1000).Wait(); // Синхронное ожидание 1 секунда

                string updaterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PWin11Updater.exe");
                if (File.Exists(updaterPath))
                {
                    ProcessStartInfo processInfo = new ProcessStartInfo
                    {
                        FileName = updaterPath,
                        Verb = "runas" // Запуск от имени администратора
                    };
                    Process.Start(processInfo);
                    Debug.WriteLine($"Opened updater with admin rights: {updaterPath}");
                }
                else
                {
                    Debug.WriteLine($"Updater not found at: {updaterPath}");
                }
            }
            catch (Exception ex)
            {
                // Обработка случая, когда пользователь отказывается от прав администратора
                if (ex is System.ComponentModel.Win32Exception && ex.Message.Contains("The requested operation requires elevation"))
                {
                    Debug.WriteLine("User declined admin rights.");
                }
                else
                {
                    Debug.WriteLine($"Error opening updater: {ex.Message}");
                }
            }
        }

        private async void UninstallOllamaButton_Click(object sender, RoutedEventArgs e)
        {
            UninstallOllamaButton.IsEnabled = false;
            UninstallOllamaStatusText.Visibility = Visibility.Visible;
            UninstallOllamaStatusText.Text = "Удаление Ollama и моделей...";

            try
            {
                await OllamaManager.UninstallOllamaAsync();
                UninstallOllamaStatusText.Text = "Ollama и модели успешно удалены.";
                UninstallOllamaButton.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                UninstallOllamaStatusText.Text = $"Ошибка: {ex.Message}";
            }
            finally
            {
                UninstallOllamaButton.IsEnabled = true;
            }
        }

    }
}