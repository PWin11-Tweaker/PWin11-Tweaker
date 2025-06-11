using System;
using System.Collections.Generic;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation; // Добавляем для DrillInNavigationTransitionInfo
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Windows.Storage;
using System.Diagnostics;
using System.Security.Principal;
using Windows.UI.ViewManagement;
using WinRT.Interop;
using PWin11_Tweaker_s.TempCleaner;

namespace PWin11_Tweaker_s
{
    public sealed partial class MainWindow : Window
    {
        private MicaBackdrop micaBackdrop = new MicaBackdrop();
        private const string ThemePreferenceKey = "ThemePreference";
        private AppWindow? appWindow;

        public MainWindow()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("MainWindow: Начало инициализации.");
                this.InitializeComponent();
                System.Diagnostics.Debug.WriteLine("MainWindow: InitializeComponent завершён.");

                this.SystemBackdrop = micaBackdrop;
                System.Diagnostics.Debug.WriteLine("MainWindow: MicaBackdrop установлен.");
                SetCustomIcon();

                CheckAdminRights();

                DispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        ContentFrame.Navigate(typeof(HomePage), null, new DrillInNavigationTransitionInfo());
                        NavView.SelectedItem = NavView.MenuItems[0];
                        System.Diagnostics.Debug.WriteLine("MainWindow: Начальная страница установлена.");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"MainWindow: Ошибка при навигации на HomePage: {ex.Message}");
                    }
                });

                App.InitializeMainWindow(this);

                appWindow = GetAppWindowForCurrentWindow();
                if (appWindow != null)
                {
                    appWindow.Title = "PWin11";
                    appWindow.SetIcon("Assets/icon4.ico");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("MainWindow: Не удалось инициализировать appWindow.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow: Ошибка при инициализации: {ex.Message}");
            }
        }

        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
        }

        private void CheckAdminRights()
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
                    AdminWarningText.Visibility = isAdmin ? Visibility.Collapsed : Visibility.Visible;
                    System.Diagnostics.Debug.WriteLine($"MainWindow: Приложение запущено с правами администратора: {isAdmin}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow: Ошибка при проверке прав администратора: {ex.Message}");
                AdminWarningText.Visibility = Visibility.Visible;
            }
        }

        private void SetCustomIcon()
        {
            try
            {
                var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
                var windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
                var appWindow = AppWindow.GetFromWindowId(windowId);
                string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.ico");
                appWindow.SetIcon(iconPath);
                System.Diagnostics.Debug.WriteLine("MainWindow: Кастомная иконка установлена.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow.SetCustomIcon: Ошибка: {ex.Message}");
            }
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("NavView_ItemInvoked: Начало обработки события навигации.");

                if (ContentFrame == null)
                {
                    System.Diagnostics.Debug.WriteLine("NavView_ItemInvoked: Ошибка: ContentFrame не инициализирован.");
                    return;
                }

                if (args.IsSettingsInvoked)
                {
                    System.Diagnostics.Debug.WriteLine("NavView_ItemInvoked: Переход на страницу настроек (SettingsPage).");
                    if (ContentFrame.CurrentSourcePageType != typeof(SettingsPage))
                    {
                        ContentFrame.Navigate(typeof(SettingsPage), null, new DrillInNavigationTransitionInfo());
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("NavView_ItemInvoked: Уже на странице SettingsPage, навигация не требуется.");
                    }
                    return;
                }

                var invokedItem = args.InvokedItemContainer as NavigationViewItem;
                if (invokedItem == null)
                {
                    System.Diagnostics.Debug.WriteLine("NavView_ItemInvoked: Ошибка: InvokedItemContainer не является NavigationViewItem.");
                    return;
                }

                string? tag = invokedItem.Tag?.ToString();
                if (string.IsNullOrEmpty(tag))
                {
                    System.Diagnostics.Debug.WriteLine("NavView_ItemInvoked: Ошибка: Тег элемента пустой или null.");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"NavView_ItemInvoked: Выбран тег: {tag}");

                var pageMap = new Dictionary<string, Type>
                {
                    { "HomePage", typeof(HomePage) },
                    { "ExplorerPage", typeof(ExplorerPage) },
                    { "SystemPage", typeof(SystemPage) },
                    { "InterfacePage", typeof(InterfacePage) },
                    { "PerformancePage", typeof(PerformancePage) },
                    { "PrivacyPage", typeof(PrivacyPage) },
                    { "TempCleanerPage", typeof(TempCleanerPage)},
                };

                if (!pageMap.TryGetValue(tag, out Type? pageType) || pageType == null)
                {
                    System.Diagnostics.Debug.WriteLine($"NavView_ItemInvoked: Ошибка: Неизвестный тег '{tag}'.");
                    return;
                }

                if (ContentFrame.CurrentSourcePageType == pageType)
                {
                    System.Diagnostics.Debug.WriteLine($"NavView_ItemInvoked: Уже на странице {pageType.Name}, навигация не требуется.");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"NavView_ItemInvoked: Переход на страницу {pageType.Name}.");
                ContentFrame.Navigate(pageType, null, new DrillInNavigationTransitionInfo());
                System.Diagnostics.Debug.WriteLine($"NavView_ItemInvoked: Навигация на {pageType.Name} выполнена успешно.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NavView_ItemInvoked: Ошибка при навигации: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        public void ToggleTheme()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("MainWindow.ToggleTheme: Переключаем тему.");
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
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