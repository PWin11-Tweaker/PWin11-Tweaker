using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Dispatching;

namespace PWin11_Tweaker_s
{
    public sealed partial class MainWindow : Window
    {
        private MicaBackdrop micaBackdrop;
        private const string ThemePreferenceKey = "ThemePreference";

        public MainWindow()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("MainWindow: Начало инициализации.");
                this.InitializeComponent();
                System.Diagnostics.Debug.WriteLine("MainWindow: InitializeComponent завершён.");

                micaBackdrop = new MicaBackdrop();
                this.SystemBackdrop = micaBackdrop;
                System.Diagnostics.Debug.WriteLine("MainWindow: MicaBackdrop установлен.");

                // Откладываем навигацию на HomePage
                DispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        ContentFrame.Navigate(typeof(HomePage));
                        NavView.SelectedItem = NavView.MenuItems[0];
                        System.Diagnostics.Debug.WriteLine("MainWindow: Начальная страница установлена.");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"MainWindow: Ошибка при навигации на HomePage: {ex.Message}");
                    }
                });

                // Инициализируем MainWindow в App
                App.InitializeMainWindow(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow: Ошибка при инициализации: {ex.Message}");
            }
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            try
            {
                if (args.IsSettingsInvoked)
                {
                    ContentFrame.Navigate(typeof(SettingsPage));
                    return;
                }

                var invokedItem = args.InvokedItemContainer as NavigationViewItem;
                if (invokedItem == null) return;

                string? tag = invokedItem.Tag?.ToString();
                if (tag == null) return;

                Type? pageType = null;
                switch (tag)
                {
                    case "HomePage":
                        pageType = typeof(HomePage);
                        break;
                    case "ExplorerPage":
                        pageType = typeof(ExplorerPage);
                        break;
                    case "SystemPage":
                        pageType = typeof(SystemPage);
                        break;
                    case "InterfacePage":
                        pageType = typeof(InterfacePage);
                        break;
                    case "PerformancePage":
                        pageType = typeof(PerformancePage);
                        break;
                    case "PrivacyPage":
                        pageType = typeof(PrivacyPage);
                        break;
                }

                if (pageType != null && ContentFrame.CurrentSourcePageType != pageType)
                {
                    ContentFrame.Navigate(pageType);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NavView_ItemInvoked: Ошибка: {ex.Message}");
            }
        }

        public void ToggleTheme()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("MainWindow.ToggleTheme: Переключаем тему.");

                // Сохраняем выбор темы
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

                // Переключаем тему
                var currentTheme = ((FrameworkElement)this.Content).RequestedTheme;
                if (currentTheme == ElementTheme.Dark)
                {
                    System.Diagnostics.Debug.WriteLine("MainWindow.ToggleTheme: Устанавливаем светлую тему.");
                    ((FrameworkElement)this.Content).RequestedTheme = ElementTheme.Light;
                    localSettings.Values[ThemePreferenceKey] = "Light";
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("MainWindow.ToggleTheme: Устанавливаем тёмную тему.");
                    ((FrameworkElement)this.Content).RequestedTheme = ElementTheme.Dark;
                    localSettings.Values[ThemePreferenceKey] = "Dark";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow.ToggleTheme: Ошибка: {ex.Message}");
            }
        }
    }
}