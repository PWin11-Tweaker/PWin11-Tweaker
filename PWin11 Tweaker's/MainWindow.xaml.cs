using System;
using System.Collections.Generic;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Windows.Storage;
using System.Diagnostics;
using System.Security.Principal; // Для проверки прав администратора
using Windows.UI.ViewManagement;
using WinRT.Interop;

namespace PWin11_Tweaker_s
{
    public sealed partial class MainWindow : Window
    {
        private MicaBackdrop micaBackdrop;
        private const string ThemePreferenceKey = "ThemePreference";
        private AppWindow _appWindow;

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
                SetCustomIcon();

                // Проверяем права администратора
                CheckAdminRights();

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

                _appWindow = GetAppWindowForCurrentWindow();
                _appWindow.Title = "PWin11";
                _appWindow.SetIcon("Assets/logo2.ico");
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
                // Проверяем, запущено ли приложение с правами администратора
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);

                    // Если не администратор, показываем предупреждение
                    AdminWarningText.Visibility = isAdmin ? Visibility.Collapsed : Visibility.Visible;
                    System.Diagnostics.Debug.WriteLine($"MainWindow: Приложение запущено с правами администратора: {isAdmin}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow: Ошибка при проверке прав администратора: {ex.Message}");
                // В случае ошибки показываем предупреждение, чтобы быть на стороне безопасности
                AdminWarningText.Visibility = Visibility.Visible;
            }
        }

        private void SetCustomIcon()
        {
            try
            {
                // Получаем AppWindow из текущего окна
                var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
                var windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
                var appWindow = AppWindow.GetFromWindowId(windowId);

                // Указываем путь к файлу иконки
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

                // Проверяем, что ContentFrame инициализирован
                if (ContentFrame == null)
                {
                    System.Diagnostics.Debug.WriteLine("NavView_ItemInvoked: Ошибка: ContentFrame не инициализирован.");
                    return;
                }

                // Обработка перехода на страницу настроек
                if (args.IsSettingsInvoked)
                {
                    System.Diagnostics.Debug.WriteLine("NavView_ItemInvoked: Переход на страницу настроек (SettingsPage).");
                    if (ContentFrame.CurrentSourcePageType != typeof(SettingsPage))
                    {
                        ContentFrame.Navigate(typeof(SettingsPage));
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("NavView_ItemInvoked: Уже на странице SettingsPage, навигация не требуется.");
                    }
                    return;
                }

                // Получаем вызванный элемент
                var invokedItem = args.InvokedItemContainer as NavigationViewItem;
                if (invokedItem == null)
                {
                    System.Diagnostics.Debug.WriteLine("NavView_ItemInvoked: Ошибка: InvokedItemContainer не является NavigationViewItem.");
                    return;
                }

                // Получаем тег элемента
                string? tag = invokedItem.Tag?.ToString();
                if (string.IsNullOrEmpty(tag))
                {
                    System.Diagnostics.Debug.WriteLine("NavView_ItemInvoked: Ошибка: Тег элемента пустой или null.");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"NavView_ItemInvoked: Выбран тег: {tag}");

                // Словарь для сопоставления тегов с типами страниц
                var pageMap = new Dictionary<string, Type>
                {
                    { "HomePage", typeof(HomePage) },
                    { "ExplorerPage", typeof(ExplorerPage) },
                    { "SystemPage", typeof(SystemPage) },
                    { "InterfacePage", typeof(InterfacePage) },
                    { "PerformancePage", typeof(PerformancePage) },
                    { "PrivacyPage", typeof(PrivacyPage) }
                };

                // Проверяем, есть ли тег в словаре
                if (!pageMap.TryGetValue(tag, out Type? pageType) || pageType == null)
                {
                    System.Diagnostics.Debug.WriteLine($"NavView_ItemInvoked: Ошибка: Неизвестный тег '{tag}'.");
                    return;
                }

                // Проверяем, не находится ли пользователь уже на этой странице
                if (ContentFrame.CurrentSourcePageType == pageType)
                {
                    System.Diagnostics.Debug.WriteLine($"NavView_ItemInvoked: Уже на странице {pageType.Name}, навигация не требуется.");
                    return;
                }

                // Выполняем навигацию
                System.Diagnostics.Debug.WriteLine($"NavView_ItemInvoked: Переход на страницу {pageType.Name}.");
                ContentFrame.Navigate(pageType);
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