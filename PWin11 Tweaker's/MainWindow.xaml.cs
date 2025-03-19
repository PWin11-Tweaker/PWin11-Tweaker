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

        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item)
            {
                switch (item.Tag.ToString())
                {
                    case "Explorer":
                        ContentFrame.Navigate(typeof(ExplorerPage));
                        break;
                    case "System":
                        ContentFrame.Navigate(typeof(SystemPage));
                        break;
                    case "Interface":
                        ContentFrame.Navigate(typeof(InterfacePage));
                        break;
                    case "Performance":
                        ContentFrame.Navigate(typeof(PerformancePage));
                        break;
                    case "Privacy":
                        ContentFrame.Navigate(typeof(PrivacyPage));
                        break;
                }
            }
        }

        private void ToggleThemeButton_Click(object sender, RoutedEventArgs e)
        {
            var currentTheme = ((FrameworkElement)this.Content).RequestedTheme;
            ((FrameworkElement)this.Content).RequestedTheme = currentTheme == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark; 
        }
    }
}