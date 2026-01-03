using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PWin11_Tweaker_s.Script;
using Microsoft.Windows.ApplicationModel.Resources; // Для работы локализации
using PWin11_Tweaker_s.Helpers;

namespace PWin11_Tweaker_s
{
    public sealed partial class InterfacePage : Microsoft.UI.Xaml.Controls.Page
    {
        //Для локализации
        private readonly ResourceLoader resourceLoader;
        private CancellationTokenSource? _cts;

        public InterfacePage()
        {
            this.InitializeComponent();
            //Инициализируем наши ресурсы для локализации
            resourceLoader = new ResourceLoader();
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            try
            {
                // Выравнивание панели задач
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                {
                    int? alignment = key?.GetValue("TaskbarAl") as int?;
                    TaskbarAlignmentCombo.SelectedIndex = alignment == 0 ? 1 : 0; // 0 = слева, 1 = по центру
                }


                // Прозрачность панели задач
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    int? transparency = key?.GetValue("EnableTransparency") as int?;
                    TaskbarTransparencyToggle.IsChecked = transparency == 1;
                }

                // Скрытие кнопки поиска
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Search"))
                {
                    int? searchboxTaskbarMode = key?.GetValue("SearchboxTaskbarMode") as int?;
                    HideSearchButtonToggle.IsChecked = searchboxTaskbarMode == 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadCurrentSettings: Ошибка: {ex.Message}");
            }
        }

        private void TaskbarAlignmentCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Чтобы не было ошибок ^_- (Всё через кнопку применить)
        }



        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            var dispatcher = this.DispatcherQueue;
            long lastUiUpdate = 0;
            void UpdateUI(Action a)
            {
                var now = Environment.TickCount;
                if (now - lastUiUpdate > 100)
                {
                    lastUiUpdate = now;
                    AsyncHelpers.RunOnUI(dispatcher, a);
                }
            }

            try
            {
                UpdateUI(() =>
                {
                    ProgressPanel.Visibility = Visibility.Visible;
                    ApplyButton.IsEnabled = false;
                    StatusText.Text = resourceLoader.GetString("Preparation");
                    ProgressBar.Value = 0;
                });

                await Task.Delay(100, ct);

                // Gather desired settings
                int alignment = TaskbarAlignmentCombo.SelectedItem is ComboBoxItem item ? int.Parse(item.Tag.ToString()) : 1;
                bool transparency = TaskbarTransparencyToggle.IsChecked ?? false;
                bool hideSearch = HideSearchButtonToggle.IsChecked ?? false;

                var tasks = new System.Collections.Generic.List<Task>();

                tasks.Add(AsyncHelpers.SetRegistryValueAsync(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAl", alignment, RegistryValueKind.DWord, ct));

                tasks.Add(AsyncHelpers.SetRegistryValueAsync(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", transparency ? 1 : 0, RegistryValueKind.DWord, ct));

                tasks.Add(AsyncHelpers.SetRegistryValueAsync(RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", hideSearch ? 0 : 1, RegistryValueKind.DWord, ct));

                UpdateUI(() => ProgressBar.Value = 30);

                await Task.WhenAll(tasks).ConfigureAwait(false);

                UpdateUI(() => ProgressBar.Value = 60);

                // Restart explorer to apply
                await AsyncHelpers.RestartExplorerAsync(5000, ct).ConfigureAwait(false);

                UpdateUI(() =>
                {
                    ProgressBar.Value = 100;
                    StatusText.Text = resourceLoader.GetString("Success");
                });

                await Task.Delay(500, ct);

                UpdateUI(async () =>
                {
                    var dialog = new ContentDialog
                    {
                        Title = resourceLoader.GetString("Success"),
                        Content = resourceLoader.GetString("Success_Title_Interface"),
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await dialog.ShowAsync();
                });
            }
            catch (OperationCanceledException)
            {
                AsyncHelpers.RunOnUI(this.DispatcherQueue, () => StatusText.Text = resourceLoader.GetString("Dialog_Error_Title"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyButton_Click: Ошибка: {ex.Message}");
                AsyncHelpers.RunOnUI(this.DispatcherQueue, () =>
                {
                    var dialog = new ContentDialog
                    {
                        Title = resourceLoader.GetString("Dialog_Error_Title"),
                        Content = ex.Message,
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    _ = dialog.ShowAsync();
                });
            }
            finally
            {
                AsyncHelpers.RunOnUI(this.DispatcherQueue, () =>
                {
                    ProgressPanel.Visibility = Visibility.Collapsed;
                    ApplyButton.IsEnabled = true;
                });
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _cts?.Cancel();
                Debug.WriteLine("CancelButton_Click: Операция отменена пользователем (InterfacePage).");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CancelButton_Click: Ошибка при попытке отмены: {ex.Message}");
            }
        }
    }
}