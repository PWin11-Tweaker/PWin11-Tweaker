using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Windows.ApplicationModel.Resources;
using PWin11_Tweaker_s.Script;
using PWin11_Tweaker_s.Helpers;

namespace PWin11_Tweaker_s
{
    public sealed partial class PrivacyPage : Microsoft.UI.Xaml.Controls.Page
    {
        private readonly ResourceLoader resourceLoader;
        private CancellationTokenSource? _cts;

        public PrivacyPage()
        {
            this.InitializeComponent();
            resourceLoader = new ResourceLoader();
            Debug.WriteLine("PrivacyPage: Инициализация страницы завершена.");
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            try
            {
                Debug.WriteLine("LoadCurrentSettings: Загрузка текущих настроек начата.");

                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection", false))
                {
                    int? telemetry = key?.GetValue("AllowTelemetry") as int? ?? 3;
                    DisableTelemetryToggle.IsChecked = telemetry == 0;
                    Debug.WriteLine($"LoadCurrentSettings: Телеметрия - AllowTelemetry = {telemetry}, Toggle = {DisableTelemetryToggle.IsChecked}");
                }

                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo"))
                {
                    int? adId = key?.GetValue("Enabled") as int? ?? 1;
                    DisableAdvertisingIdToggle.IsChecked = adId == 0;
                    Debug.WriteLine($"LoadCurrentSettings: Рекламный ID - Enabled = {adId}, Toggle = {DisableAdvertisingIdToggle.IsChecked}");
                }

                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", false))
                {
                    int? location = key?.GetValue("DisableLocation") as int? ?? 0;
                    DisableLocationToggle.IsChecked = location == 1;
                    Debug.WriteLine($"LoadCurrentSettings: Местоположение - DisableLocation = {location}, Toggle = {DisableLocationToggle.IsChecked}");
                }

                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search", false))
                {
                    int? cortana = key?.GetValue("AllowCortana") as int? ?? 1;
                    DisableCortanaToggle.IsChecked = cortana == 0;
                    Debug.WriteLine($"LoadCurrentSettings: Cortana - AllowCortana = {cortana}, Toggle = {DisableCortanaToggle.IsChecked}");
                }

                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications"))
                {
                    int? backgroundApps = key?.GetValue("GlobalUserDisabled") as int? ?? 0;
                    DisableBackgroundAppsToggle.IsChecked = backgroundApps == 1;
                    Debug.WriteLine($"LoadCurrentSettings: Фоновые приложения - GlobalUserDisabled = {backgroundApps}, Toggle = {DisableBackgroundAppsToggle.IsChecked}");
                }

                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\CloudExperienceHost"))
                {
                    int? cloudContent = key?.GetValue("DisableCloudOptimizedContent") as int? ?? 0;
                    DisableCloudContentToggle.IsChecked = cloudContent == 1;
                    Debug.WriteLine($"LoadCurrentSettings: Облачный контент - DisableCloudOptimizedContent = {cloudContent}, Toggle = {DisableCloudContentToggle.IsChecked}");
                }

                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\FindMyDevice", false))
                {
                    int? findMyDevice = key?.GetValue("AllowFindMyDevice") as int? ?? 1;
                    DisableFindMyDeviceToggle.IsChecked = findMyDevice == 0;
                    Debug.WriteLine($"LoadCurrentSettings: Find My Device - AllowFindMyDevice = {findMyDevice}, Toggle = {DisableFindMyDeviceToggle.IsChecked}");
                }

                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\PreviewBuilds", false))
                {
                    int? insiderTelemetry = key?.GetValue("AllowBuildPreview") as int? ?? 1;
                    DisableInsiderTelemetryToggle.IsChecked = insiderTelemetry == 0;
                    Debug.WriteLine($"LoadCurrentSettings: Windows Insider Telemetry - AllowBuildPreview = {insiderTelemetry}, Toggle = {DisableInsiderTelemetryToggle.IsChecked}");
                }

                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Edge", false))
                {
                    int? edgeDiagnostics = key?.GetValue("DiagnosticData") as int? ?? 1;
                    DisableEdgeDiagnosticsToggle.IsChecked = edgeDiagnostics == 0;
                    Debug.WriteLine($"LoadCurrentSettings: Microsoft Edge Diagnostics - DiagnosticData = {edgeDiagnostics}, Toggle = {DisableEdgeDiagnosticsToggle.IsChecked}");
                }

                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"))
                {
                    int? suggestedContent = key?.GetValue("SubscribedContent-338393Enabled") as int? ?? 1;
                    DisableSuggestedContentToggle.IsChecked = suggestedContent == 0;
                    Debug.WriteLine($"LoadCurrentSettings: Suggested Content - SubscribedContent-338393Enabled = {suggestedContent}, Toggle = {DisableSuggestedContentToggle.IsChecked}");
                }

                Debug.WriteLine("LoadCurrentSettings: Загрузка текущих настроек завершена.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadCurrentSettings: Ошибка: {ex.Message}");
            }
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            try
            {
                Debug.WriteLine("ApplyButton_Click: Начало применения настроек.");
                ProgressPanel.Visibility = Visibility.Visible;
                ApplyButton.IsEnabled = false;
                ResetButton.IsEnabled = false;
                StatusText.Text = resourceLoader.GetString("Preparation");
                ProgressBar.Value = 0;
                await Task.Delay(100, ct);

                var tasks = new System.Collections.Generic.List<Task>();

                bool disableTelemetry = DisableTelemetryToggle.IsChecked ?? false;
                tasks.Add(AsyncHelpers.SetRegistryValueAsync(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", disableTelemetry ? 0 : 1, RegistryValueKind.DWord, ct));
                if (disableTelemetry)
                {
                    tasks.Add(AsyncHelpers.SetRegistryValueAsync(RegistryHive.LocalMachine,
                        @"SYSTEM\CurrentControlSet\Services\DiagTrack", "Start", 4, RegistryValueKind.DWord, ct));
                }

                bool disableAdId = DisableAdvertisingIdToggle.IsChecked ?? false;
                tasks.Add(AsyncHelpers.SetRegistryValueAsync(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", disableAdId ? 0 : 1, RegistryValueKind.DWord, ct));

                bool disableLocation = DisableLocationToggle.IsChecked ?? false;
                tasks.Add(AsyncHelpers.SetRegistryValueAsync(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "DisableLocation", disableLocation ? 1 : 0, RegistryValueKind.DWord, ct));
                tasks.Add(AsyncHelpers.SetRegistryValueAsync(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "DisableLocationForAllUsers", disableLocation ? 1 : 0, RegistryValueKind.DWord, ct));

                bool disableCortana = DisableCortanaToggle.IsChecked ?? false;
                tasks.Add(AsyncHelpers.SetRegistryValueAsync(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", disableCortana ? 0 : 1, RegistryValueKind.DWord, ct));

                bool disableBackgroundApps = DisableBackgroundAppsToggle.IsChecked ?? false;
                tasks.Add(AsyncHelpers.SetRegistryValueAsync(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled", disableBackgroundApps ? 1 : 0, RegistryValueKind.DWord, ct));

                bool disableCloudContent = DisableCloudContentToggle.IsChecked ?? false;
                tasks.Add(AsyncHelpers.SetRegistryValueAsync(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\CloudExperienceHost", "DisableCloudOptimizedContent", disableCloudContent ? 1 : 0, RegistryValueKind.DWord, ct));
                tasks.Add(AsyncHelpers.SetRegistryValueAsync(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SystemPaneSuggestionsEnabled", disableCloudContent ? 0 : 1, RegistryValueKind.DWord, ct));

                bool disableFindMyDevice = DisableFindMyDeviceToggle.IsChecked ?? false;
                tasks.Add(AsyncHelpers.SetRegistryValueAsync(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\FindMyDevice", "AllowFindMyDevice", disableFindMyDevice ? 0 : 1, RegistryValueKind.DWord, ct));

                bool disableInsiderTelemetry = DisableInsiderTelemetryToggle.IsChecked ?? false;
                tasks.Add(AsyncHelpers.SetRegistryValueAsync(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\PreviewBuilds", "AllowBuildPreview", disableInsiderTelemetry ? 0 : 1, RegistryValueKind.DWord, ct));

                bool disableEdgeDiagnostics = DisableEdgeDiagnosticsToggle.IsChecked ?? false;
                tasks.Add(AsyncHelpers.SetRegistryValueAsync(RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Edge", "DiagnosticData", disableEdgeDiagnostics ? 0 : 1, RegistryValueKind.DWord, ct));

                bool disableSuggestedContent = DisableSuggestedContentToggle.IsChecked ?? false;
                tasks.Add(AsyncHelpers.SetRegistryValueAsync(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338393Enabled", disableSuggestedContent ? 0 : 1, RegistryValueKind.DWord, ct));

                StatusText.Text = resourceLoader.GetString("Apply_Change");
                ProgressBar.Value = 50;
                await Task.Delay(100, ct);

                await Task.WhenAll(tasks).ConfigureAwait(false);

                // Применяем некоторые остановки служб, если нужно
                try
                {
                    if (disableTelemetry)
                    {
                        var psi = new ProcessStartInfo { FileName = "sc", Arguments = "stop DiagTrack", UseShellExecute = true, CreateNoWindow = true };
                        await AsyncHelpers.RunProcessAsync(psi, 5000, ct).ConfigureAwait(false);
                    }
                    if (disableLocation)
                    {
                        var psi = new ProcessStartInfo { FileName = "sc", Arguments = "stop lfsvc", UseShellExecute = true, CreateNoWindow = true };
                        await AsyncHelpers.RunProcessAsync(psi, 5000, ct).ConfigureAwait(false);
                    }
                    if (disableCortana)
                    {
                        var psi = new ProcessStartInfo { FileName = "sc", Arguments = "stop Cortana", UseShellExecute = true, CreateNoWindow = true };
                        await AsyncHelpers.RunProcessAsync(psi, 5000, ct).ConfigureAwait(false);
                    }
                    if (disableFindMyDevice)
                    {
                        var psi = new ProcessStartInfo { FileName = "sc", Arguments = "stop OneSyncSvc", UseShellExecute = true, CreateNoWindow = true };
                        await AsyncHelpers.RunProcessAsync(psi, 5000, ct).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ApplyButton_Click: Ошибка при остановке служб: {ex.Message}");
                }

                // Сохранение состояния твиков в TweakStatus
                TweakStatus.IsTelemetryDisabled = disableTelemetry;
                TweakStatus.IsAdvertisingIdDisabled = disableAdId;
                TweakStatus.IsLocationTrackingDisabled = disableLocation;
                TweakStatus.IsCortanaDisabled = disableCortana;
                TweakStatus.IsBackgroundAppsDisabled = disableBackgroundApps;
                TweakStatus.IsCloudContentDisabled = disableCloudContent;
                TweakStatus.IsFindMyDeviceDisabled = disableFindMyDevice;
                TweakStatus.IsInsiderTelemetryDisabled = disableInsiderTelemetry;
                TweakStatus.IsEdgeDiagnosticsDisabled = disableEdgeDiagnostics;
                TweakStatus.IsSuggestedContentDisabled = disableSuggestedContent;
                TweakStatus.SaveSettings();

                StatusText.Text = resourceLoader.GetString("Success");
                ProgressBar.Value = 100;
                await Task.Delay(500, ct);

                var dialog = new ContentDialog
                {
                    Title = resourceLoader.GetString("Success"),
                    Content = resourceLoader.GetString("Success_Title_Privacy"),
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("ApplyButton_Click: Операция отменена пользователем.");
                StatusText.Text = resourceLoader.GetString("Dialog_Error_Title");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApplyButton_Click: Ошибка: {ex.Message}");
                var dialog = new ContentDialog
                {
                    Title = resourceLoader.GetString("Dialog_Error_Title"),
                    Content = ex.Message,
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
            finally
            {
                ProgressPanel.Visibility = Visibility.Collapsed;
                ApplyButton.IsEnabled = true;
                ResetButton.IsEnabled = true;
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("ResetButton_Click: Сброс всех настроек.");
            DisableTelemetryToggle.IsChecked = false;
            Debug.WriteLine("ResetButton_Click: Телеметрия - Сброшено (unchecked).");
            DisableAdvertisingIdToggle.IsChecked = false;
            Debug.WriteLine("ResetButton_Click: Рекламный ID - Сброшено (unchecked).");
            DisableLocationToggle.IsChecked = false;
            Debug.WriteLine("ResetButton_Click: Местоположение - Сброшено (unchecked).");
            DisableCortanaToggle.IsChecked = false;
            Debug.WriteLine("ResetButton_Click: Cortana - Сброшено (unchecked).");
            DisableBackgroundAppsToggle.IsChecked = false;
            Debug.WriteLine("ResetButton_Click: Фоновые приложения - Сброшено (unchecked).");
            DisableCloudContentToggle.IsChecked = false;
            Debug.WriteLine("ResetButton_Click: Облачный контент - Сброшено (unchecked).");
            DisableFindMyDeviceToggle.IsChecked = false;
            Debug.WriteLine("ResetButton_Click: Find My Device - Сброшено (unchecked).");
            DisableInsiderTelemetryToggle.IsChecked = false;
            Debug.WriteLine("ResetButton_Click: Windows Insider Telemetry - Сброшено (unchecked).");
            DisableEdgeDiagnosticsToggle.IsChecked = false;
            Debug.WriteLine("ResetButton_Click: Microsoft Edge Diagnostics - Сброшено (unchecked).");
            DisableSuggestedContentToggle.IsChecked = false;
            Debug.WriteLine("ResetButton_Click: Suggested Content - Сброшено (unchecked).");
            Debug.WriteLine("ResetButton_Click: Сброс настроек завершен.");
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _cts?.Cancel();
                Debug.WriteLine("CancelButton_Click: Операция отменена пользователем (PrivacyPage).");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CancelButton_Click: Ошибка при попытке отмены: {ex.Message}");
            }
        }
    }
}