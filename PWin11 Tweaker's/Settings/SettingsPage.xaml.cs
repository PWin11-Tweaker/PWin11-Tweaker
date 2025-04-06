using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using PWin11_Tweaker_s.Script;
using Windows.System;


namespace PWin11_Tweaker_s
{
    public sealed partial class SettingsPage : Page
    {
        
        public SettingsPage()
        {
            this.InitializeComponent();
            System.Diagnostics.Debug.WriteLine("SettingsPage: InitializeComponent завершён.");
        }



       

        private async void VisitGitHubButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("VisitGitHubButton_Click: Попытка открыть GitHub.");
                Uri gitHubUri = new Uri("https://github.com/PWin11-Tweaker/PWin11-Tweaker");
                bool success = await Launcher.LaunchUriAsync(gitHubUri);
                if (success)
                {
                    System.Diagnostics.Debug.WriteLine("VisitGitHubButton_Click: GitHub успешно открыт.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("VisitGitHubButton_Click: Не удалось открыть GitHub.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VisitGitHubButton_Click: Ошибка при открытии GitHub: {ex.Message}");
            }
        }

        private async void VisitWebsiteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("VisitWebsiteButton_Click: Попытка открыть веб-сайт.");
                Uri websiteUri = new Uri("https://t.me/ph1ncyn"); // Замените на ваш URL
                bool success = await Launcher.LaunchUriAsync(websiteUri);
                if (success)
                {
                    System.Diagnostics.Debug.WriteLine("VisitWebsiteButton_Click: Веб-сайт успешно открыт.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("VisitWebsiteButton_Click: Не удалось открыть веб-сайт.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VisitWebsiteButton_Click: Ошибка при открытии веб-сайта: {ex.Message}");
            }
        }


    }
}