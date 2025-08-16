using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PWin11_Tweaker_s.Script
{
    public static class TweakStatus
    {
        // ExplorerPage
        public static bool IsShowHiddenFilesEnabled { get; set; }
        public static bool IsSmallCaptionsEnabled { get; set; }
        public static bool IsClassicContextMenuEnabled { get; set; }
        public static bool IsStartAllBackInstalled { get; set; }

        // PrivacyPage
        public static bool IsInputDataCollectionDisabled { get; set; }
        public static bool IsTelemetryDisabled { get; set; }
        public static bool IsAdvertisingIdDisabled { get; set; }
        public static bool IsLocationTrackingDisabled { get; set; }
        public static bool IsCortanaDisabled { get; set; }
        public static bool IsBackgroundAppsDisabled { get; set; }
        public static bool IsCloudContentDisabled { get; set; }
        public static bool IsFindMyDeviceDisabled { get; set; }
        public static bool IsInsiderTelemetryDisabled { get; set; }
        public static bool IsEdgeDiagnosticsDisabled { get; set; }
        public static bool IsSuggestedContentDisabled { get; set; }

        // InterfacePage
        public static bool IsTaskbarAlignmentLeft { get; set; }
        public static bool IsTaskbarTransparencyEnabled { get; set; }
        public static bool IsSearchButtonHidden { get; set; }

        // PerformancePage
        public static bool IsVisualEffectsDisabled { get; set; }
        public static bool IsWindowsSearchDisabled { get; set; }
        public static bool IsSysMainDisabled { get; set; }
        public static string? CurrentPowerPlan { get; set; } // Исправлено: сделано nullable

        // SystemPage
        public static bool IsServicesDisabled { get; set; }
        public static bool IsUACDisabled { get; set; }
        public static bool IsClipboardHistoryDisabled { get; set; }
        public static bool IsWindowsSpeedUpApplied { get; set; }

        public static bool IsHomeFolderDisabled { get; set; }
        public static bool IsGalleryFolderDisabled { get; set; }

        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PWin11Tweaker",
            "TweakStatus.json");

        static TweakStatus()
        {
            string? directory = Path.GetDirectoryName(SettingsFilePath); // Исправлено: проверка на null
            if (directory != null)
            {
                Directory.CreateDirectory(directory);
            }
            LoadSettings();
        }

        public static void SaveSettings()
        {
            try
            {
                var settings = new
                {
                    IsShowHiddenFilesEnabled,
                    IsSmallCaptionsEnabled,
                    IsClassicContextMenuEnabled,
                    IsStartAllBackInstalled,
                    IsInputDataCollectionDisabled,
                    IsTelemetryDisabled,
                    IsAdvertisingIdDisabled,
                    IsLocationTrackingDisabled,
                    IsCortanaDisabled,
                    IsBackgroundAppsDisabled,
                    IsCloudContentDisabled,
                    IsFindMyDeviceDisabled,
                    IsInsiderTelemetryDisabled,
                    IsEdgeDiagnosticsDisabled,
                    IsSuggestedContentDisabled,
                    IsTaskbarAlignmentLeft,
                    IsTaskbarTransparencyEnabled,
                    IsSearchButtonHidden,
                    IsVisualEffectsDisabled,
                    IsWindowsSearchDisabled,
                    IsSysMainDisabled,
                    CurrentPowerPlan,
                    IsServicesDisabled,
                    IsUACDisabled,
                    IsClipboardHistoryDisabled,
                    IsWindowsSpeedUpApplied,
                    IsHomeFolderDisabled,
                    IsGalleryFolderDisabled
                };
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
                System.Diagnostics.Debug.WriteLine($"TweakStatus: Настройки сохранены в {SettingsFilePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TweakStatus.SaveSettings: Ошибка: {ex.Message}");
            }
        }

        public static void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                    IsShowHiddenFilesEnabled = GetBool(settings, nameof(IsShowHiddenFilesEnabled));
                    IsSmallCaptionsEnabled = GetBool(settings, nameof(IsSmallCaptionsEnabled));
                    IsClassicContextMenuEnabled = GetBool(settings, nameof(IsClassicContextMenuEnabled));
                    IsStartAllBackInstalled = GetBool(settings, nameof(IsStartAllBackInstalled));
                    IsInputDataCollectionDisabled = GetBool(settings, nameof(IsInputDataCollectionDisabled));
                    IsTelemetryDisabled = GetBool(settings, nameof(IsTelemetryDisabled));
                    IsAdvertisingIdDisabled = GetBool(settings, nameof(IsAdvertisingIdDisabled));
                    IsLocationTrackingDisabled = GetBool(settings, nameof(IsLocationTrackingDisabled));
                    IsCortanaDisabled = GetBool(settings, nameof(IsCortanaDisabled));
                    IsBackgroundAppsDisabled = GetBool(settings, nameof(IsBackgroundAppsDisabled));
                    IsCloudContentDisabled = GetBool(settings, nameof(IsCloudContentDisabled));
                    IsFindMyDeviceDisabled = GetBool(settings, nameof(IsFindMyDeviceDisabled));
                    IsInsiderTelemetryDisabled = GetBool(settings, nameof(IsInsiderTelemetryDisabled));
                    IsEdgeDiagnosticsDisabled = GetBool(settings, nameof(IsEdgeDiagnosticsDisabled));
                    IsSuggestedContentDisabled = GetBool(settings, nameof(IsSuggestedContentDisabled));
                    IsTaskbarAlignmentLeft = GetBool(settings, nameof(IsTaskbarAlignmentLeft));
                    IsTaskbarTransparencyEnabled = GetBool(settings, nameof(IsTaskbarTransparencyEnabled));
                    IsSearchButtonHidden = GetBool(settings, nameof(IsSearchButtonHidden));
                    IsVisualEffectsDisabled = GetBool(settings, nameof(IsVisualEffectsDisabled));
                    IsWindowsSearchDisabled = GetBool(settings, nameof(IsWindowsSearchDisabled));
                    IsSysMainDisabled = GetBool(settings, nameof(IsSysMainDisabled));
                    CurrentPowerPlan = settings.TryGetValue(nameof(CurrentPowerPlan), out var powerPlan) ? powerPlan?.ToString() : "Balanced";
                    IsServicesDisabled = GetBool(settings, nameof(IsServicesDisabled));
                    IsUACDisabled = GetBool(settings, nameof(IsUACDisabled));
                    IsClipboardHistoryDisabled = GetBool(settings, nameof(IsClipboardHistoryDisabled));
                    IsWindowsSpeedUpApplied = GetBool(settings, nameof(IsWindowsSpeedUpApplied));
                    IsHomeFolderDisabled = GetBool(settings, nameof(IsHomeFolderDisabled));
                    IsGalleryFolderDisabled = GetBool(settings, nameof(IsGalleryFolderDisabled));

                    System.Diagnostics.Debug.WriteLine($"TweakStatus: Настройки загружены из {SettingsFilePath}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"TweakStatus: Файл настроек {SettingsFilePath} не найден, используются значения по умолчанию.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TweakStatus.LoadSettings: Ошибка: {ex.Message}");
            }
        }

        private static bool GetBool(Dictionary<string, object>? settings, string key) // Исправлено: сделано nullable
        {
            if (settings == null || !settings.TryGetValue(key, out var value) || value is not bool boolValue)
                return false;
            return boolValue;
        }
    }
}