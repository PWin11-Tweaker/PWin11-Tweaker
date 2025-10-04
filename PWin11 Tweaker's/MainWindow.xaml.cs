using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using PWin11_Tweaker_s.TempCleaner;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Principal;
using Windows.Storage;
using WinRT.Interop;

namespace PWin11_Tweaker_s
{
    public sealed partial class MainWindow : Window
    {
        private readonly DesktopAcrylicBackdrop acrylicBackdrop = new DesktopAcrylicBackdrop();
        private const string ThemePreferenceKey = "ThemePreference";
        private AppWindow? appWindow;

        public MainWindow()
        {
            try
            {
                //FOR TITLEBAR
                // Hides the default system title bar.
                ExtendsContentIntoTitleBar = true;
                // Replace system title bar with the WinUI TitleBar control. 
                SetTitleBar(SimpleTitleBar);

                Debug.WriteLine("MainWindow: Starting initialization.");
                this.InitializeComponent();
                Debug.WriteLine("MainWindow: InitializeComponent completed.");

                try
                {
                    this.SystemBackdrop = acrylicBackdrop;
                    Debug.WriteLine("MainWindow: DesktopAcrylicBackdrop set.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"MainWindow: Failed to set DesktopAcrylicBackdrop, using XAML Acrylic fallback. Error: {ex.Message}");
                }

                SetCustomIcon();
                CheckAdminRights();
                CheckWindowsVersion();

                DispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        if (Type.GetType("PWin11_Tweaker_s.HomePage") != null)
                        {
                            ContentFrame.Navigate(typeof(HomePage), null, new DrillInNavigationTransitionInfo());
                            NavView.SelectedItem = NavView.MenuItems[0];
                            Debug.WriteLine("MainWindow: Navigated to HomePage.");
                        }
                        else
                        {
                            Debug.WriteLine("MainWindow: HomePage type not found.");
                            ContentFrame.Content = new TextBlock
                            {
                                Text = "Error: HomePage not found. Please check page definitions.",
                                Foreground = new SolidColorBrush(Colors.Red),
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"MainWindow: Error navigating to HomePage: {ex.Message} StackTrace: {ex.StackTrace}");
                        ContentFrame.Content = new TextBlock
                        {
                            Text = $"Navigation Error: {ex.Message}",
                            Foreground = new SolidColorBrush(Colors.Red),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                    }
                });

                appWindow = GetAppWindowForCurrentWindow();
                if (appWindow != null)
                {
                    appWindow.Title = "PWin11";
                    appWindow.SetIcon("Assets/new_logo/mini_logo.ico");
                    Debug.WriteLine("MainWindow: AppWindow title and icon set.");
                }
                else
                {
                    Debug.WriteLine("MainWindow: Failed to initialize AppWindow.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MainWindow: Initialization error: {ex.Message} StackTrace: {ex.StackTrace}");
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
                    Debug.WriteLine($"MainWindow: Running with admin privileges: {isAdmin}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MainWindow: Error checking admin rights: {ex.Message}");
                AdminWarningText.Visibility = Visibility.Visible;
            }
        }

        private void SetCustomIcon()
        {
            try
            {
                var windowHandle = WindowNative.GetWindowHandle(this);
                var windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
                var appWindow = AppWindow.GetFromWindowId(windowId);
                string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.ico");
                appWindow.SetIcon(iconPath);
                Debug.WriteLine("MainWindow: Custom icon set.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MainWindow.SetCustomIcon: Error: {ex.Message}");
            }
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            try
            {
                Debug.WriteLine("NavView_ItemInvoked: Handling navigation event.");

                if (ContentFrame == null)
                {
                    Debug.WriteLine("NavView_ItemInvoked: Error: ContentFrame is not initialized.");
                    return;
                }

                if (args.IsSettingsInvoked)
                {
                    Debug.WriteLine("NavView_ItemInvoked: Navigating to SettingsPage.");
                    if (ContentFrame.CurrentSourcePageType != typeof(SettingsPage))
                    {
                        ContentFrame.Navigate(typeof(SettingsPage), null, new DrillInNavigationTransitionInfo());
                    }
                    else
                    {
                        Debug.WriteLine("NavView_ItemInvoked: Already on SettingsPage, no navigation needed.");
                    }
                    return;
                }

                var invokedItem = args.InvokedItemContainer as NavigationViewItem;
                if (invokedItem == null)
                {
                    Debug.WriteLine("NavView_ItemInvoked: Error: InvokedItemContainer is not a NavigationViewItem.");
                    return;
                }

                string? tag = invokedItem.Tag?.ToString();
                if (string.IsNullOrEmpty(tag))
                {
                    Debug.WriteLine("NavView_ItemInvoked: Error: Item tag is empty or null.");
                    return;
                }

                Debug.WriteLine($"NavView_ItemInvoked: Selected tag: {tag}");

                var pageMap = new Dictionary<string, Type>
                {
                    { "HomePage", typeof(HomePage) },
                    { "ExplorerPage", typeof(ExplorerPage) },
                    { "SystemPage", typeof(SystemPage) },
                    { "InterfacePage", typeof(InterfacePage) },
                    { "PerformancePage", typeof(PerformancePage) },
                    { "PrivacyPage", typeof(PrivacyPage) },
                    { "TempCleanerPage", typeof(TempCleanerPage) },
                    { "eraPage", typeof(eraPage) }  // Добавлена навигация на eraPage
                };

                if (!pageMap.TryGetValue(tag, out Type? pageType) || pageType == null)
                {
                    Debug.WriteLine($"NavView_ItemInvoked: Error: Unknown tag '{tag}'.");
                    ContentFrame.Content = new TextBlock
                    {
                        Text = $"Error: Page for tag '{tag}' not found.",
                        Foreground = new SolidColorBrush(Colors.Red),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    return;
                }

                if (ContentFrame.CurrentSourcePageType != pageType)
                {
                    Debug.WriteLine($"NavView_ItemInvoked: Navigating to {pageType.Name}.");
                    ContentFrame.Navigate(pageType, null, new DrillInNavigationTransitionInfo());
                    Debug.WriteLine($"NavView_ItemInvoked: Navigation to {pageType.Name} completed.");
                }
                else
                {
                    Debug.WriteLine($"NavView_ItemInvoked: Already on {pageType.Name}, no navigation needed.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"NavView_ItemInvoked: Navigation error: {ex.Message} StackTrace: {ex.StackTrace}");
                ContentFrame.Content = new TextBlock
                {
                    Text = $"Navigation Error: {ex.Message}",
                    Foreground = new SolidColorBrush(Colors.Red),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }
        }

        public void ToggleTheme()
        {
            try
            {
                Debug.WriteLine("MainWindow.ToggleTheme: Switching theme.");
                var localSettings = ApplicationData.Current.LocalSettings;
                var currentTheme = ((FrameworkElement)this.Content).RequestedTheme;
                if (currentTheme == ElementTheme.Dark)
                {
                    Debug.WriteLine("MainWindow.ToggleTheme: Setting light theme.");
                    ((FrameworkElement)this.Content).RequestedTheme = ElementTheme.Light;
                    localSettings.Values[ThemePreferenceKey] = "Light";
                }
                else
                {
                    Debug.WriteLine("MainWindow.ToggleTheme: Setting dark theme.");
                    ((FrameworkElement)this.Content).RequestedTheme = ElementTheme.Dark;
                    localSettings.Values[ThemePreferenceKey] = "Dark";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MainWindow.ToggleTheme: Error: {ex.Message}");
            }
        }

        private void CheckWindowsVersion()
        {
            try
            {
                var osVersion = Environment.OSVersion.Version;
                Debug.WriteLine($"MainWindow: Detected OS Version: {osVersion}");

                if (osVersion.Major < 10 || (osVersion.Major == 10 && osVersion.Build < 26100))
                {
                    UpdateWarningText.Visibility = Visibility.Visible;
                    Debug.WriteLine("MainWindow: Windows version is below 24H2. Showing update warning.");
                }
                else
                {
                    UpdateWarningText.Visibility = Visibility.Collapsed;
                    Debug.WriteLine("MainWindow: Windows version is 24H2 or higher. No update warning.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MainWindow: Error checking Windows version: {ex.Message}");
                UpdateWarningText.Visibility = Visibility.Collapsed;
            }
        }
    }
}