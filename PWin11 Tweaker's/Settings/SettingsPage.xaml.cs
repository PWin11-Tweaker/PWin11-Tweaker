using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using PWin11_Tweaker_s.Script;

namespace PWin11_Tweaker_s
{
    public sealed partial class SettingsPage : Page
    {
        public ObservableCollection<string> DebugMessages => DebugLogger.LogMessages;

        public SettingsPage()
        {
            this.InitializeComponent();
            System.Diagnostics.Debug.WriteLine("SettingsPage: InitializeComponent завершён.");

            // Устанавливаем текущий язык в ComboBox
            InitializeLanguageSelection();

            // Подписываемся на событие смены языка
            LocalizationManager.LanguageChanged += LocalizationManager_LanguageChanged;
        }

        private void InitializeLanguageSelection()
        {
            foreach (ComboBoxItem item in LanguageComboBox.Items)
            {
                if (item.Tag as string == LocalizationManager.CurrentLanguage)
                {
                    LanguageComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void LocalizationManager_LanguageChanged(object sender, EventArgs e)
        {
            // Принудительно обновляем UI при смене языка
            this.Bindings.Update();
            System.Diagnostics.Debug.WriteLine("SettingsPage: UI обновлён после смены языка.");
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string language = selectedItem.Tag as string;
                if (!string.IsNullOrEmpty(language))
                {
                    LocalizationManager.CurrentLanguage = language;
                    System.Diagnostics.Debug.WriteLine($"SettingsPage: Язык изменён на {language}");
                }
            }
        }
    }
}