using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PWin11_Tweaker_s
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            System.Diagnostics.Debug.WriteLine("SettingsPage: InitializeComponent завершён.");
        }

        private void ToggleThemeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("SettingsPage.ToggleThemeButton_Click: Кнопка нажата.");

                // Получаем текущий объект Window (MainWindow)
                if (Window.Current is MainWindow mainWindow)
                {
                    mainWindow.ToggleTheme();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(
                        "SettingsPage.ToggleThemeButton_Click: Не удалось найти MainWindow.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в SettingsPage в ToggleTheme");
            }
        }
    }
}