using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using PWin11_Tweaker_s.Script;
using Windows.System;
using PWin11_Tweaker_s.Windowww;

namespace PWin11_Tweaker_s
{
    public sealed partial class SettingsPage : Page
    {
        private DebugWindow _debugWindow;
        public SettingsPage()
        {
            this.InitializeComponent();
            System.Diagnostics.Debug.WriteLine("SettingsPage: InitializeComponent завершён.");
        }

        private void DebugButton_Click(object sender, RoutedEventArgs e)
        {
            OpenDebugWindow();
        }

        private void OpenDebugWindow()
        {
            try
            {
                if (_debugWindow == null || !_debugWindow.AppWindow.IsVisible)
                {
                    System.Diagnostics.Debug.WriteLine("SettingsPage.OpenDebugWindow: Создание нового DebugWindow.");
                    _debugWindow = new DebugWindow(null); // Передаем null, так как нет прямой ссылки на MainWindow
                    _debugWindow.Activate();
                    System.Diagnostics.Debug.WriteLine("SettingsPage.OpenDebugWindow: DebugWindow активирован.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("SettingsPage.OpenDebugWindow: Активация существующего DebugWindow.");
                    _debugWindow.Activate();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SettingsPage.OpenDebugWindow: Ошибка: {ex.Message}");
            }
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


    }
}