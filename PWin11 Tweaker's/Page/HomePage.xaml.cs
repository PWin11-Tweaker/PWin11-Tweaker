using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PWin11_Tweaker_s
{
    public sealed partial class HomePage : Page
    {
        public HomePage()
        {
            this.InitializeComponent();
        }

        private void GoToPrivacyButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("GoToPrivacyButton_Click: Начало навигации...");
            if (this.Frame != null)
            {
                System.Diagnostics.Debug.WriteLine("GoToPrivacyButton_Click: Frame найден, переходим на PrivacyPage.");
                this.Frame.Navigate(typeof(PrivacyPage));

                if (this.Frame.Parent is NavigationView navView)
                {
                    System.Diagnostics.Debug.WriteLine("GoToPrivacyButton_Click: NavigationView найден, обновляем выбранный пункт.");
                    foreach (var item in navView.MenuItems)
                    {
                        if (item is NavigationViewItem navItem && navItem.Tag?.ToString() == "PrivacyPage")
                        {
                            navView.SelectedItem = navItem;
                            System.Diagnostics.Debug.WriteLine("GoToPrivacyButton_Click: Пункт PrivacyPage выбран.");
                            break;
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("GoToPrivacyButton_Click: NavigationView не найден.");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("GoToPrivacyButton_Click: Frame не найден.");
            }
        }
    }
}