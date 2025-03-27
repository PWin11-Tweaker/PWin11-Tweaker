using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace PWin11_Tweaker_s
{
    public sealed partial class MainWindow : Window
    {
        private MicaBackdrop micaBackdrop;

        public MainWindow()
        {
            this.InitializeComponent(); // Инициализация XAML-элементов
            micaBackdrop = new MicaBackdrop();
            this.SystemBackdrop = micaBackdrop;

            // Устанавливаем начальную страницу
            ContentFrame.Navigate(typeof(HomePage));
            NavView.SelectedItem = NavView.MenuItems[0];
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                // Если включён встроенный пункт "Settings" в NavigationView
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
                case "SettingsPage":
                    pageType = typeof(SettingsPage);
                    break;
                case "ToggleTheme":
                    ToggleTheme();
                    return;
            }

            if (pageType != null && ContentFrame.CurrentSourcePageType != pageType)
            {
                ContentFrame.Navigate(pageType);
            }
        }

        private void ToggleTheme()
        {
            // Логика переключения темы
            var currentTheme = ((FrameworkElement)this.Content).RequestedTheme;
            ((FrameworkElement)this.Content).RequestedTheme = currentTheme == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark;
        }
    }
}