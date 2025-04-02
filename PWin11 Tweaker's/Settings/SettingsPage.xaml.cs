using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using PWin11_Tweaker_s.Script;
using Windows.ApplicationModel.Resources;
using Microsoft.UI.Xaml.Navigation;

namespace PWin11_Tweaker_s
{
    public sealed partial class SettingsPage : Page
    {
        private ResourceLoader _resourceLoader;
        public ObservableCollection<string> DebugMessages => DebugLogger.LogMessages;

        public SettingsPage()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("SettingsPage: Начало инициализации.");

                this.InitializeComponent();
                System.Diagnostics.Debug.WriteLine("SettingsPage: InitializeComponent завершён.");

                // Инициализация ResourceLoader для текущего языка
                _resourceLoader = ResourceLoader.GetForViewIndependentUse("Resources");
                System.Diagnostics.Debug.WriteLine($"SettingsPage: ResourceLoader инициализирован для языка {LocalizationManager.CurrentLanguage}.");

                // Устанавливаем текущий язык в ComboBox
                InitializeLanguageSelection();
                System.Diagnostics.Debug.WriteLine("SettingsPage: InitializeLanguageSelection завершён.");

                // Подписываемся на событие смены языка
                LocalizationManager.LanguageChanged += LocalizationManager_LanguageChanged;
                System.Diagnostics.Debug.WriteLine("SettingsPage: Подписка на LanguageChanged завершена.");

                // Инициализация текста элементов (для первого запуска)
                UpdateUIText();
                System.Diagnostics.Debug.WriteLine("SettingsPage: UpdateUIText завершён (из конструктора).");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SettingsPage: Ошибка при инициализации: {ex.Message}\nStackTrace: {ex.StackTrace}");
                ShowErrorDialog($"{LocalizationManager.GetString("ErrorDialog.Content")} {ex.Message}");
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            UpdateUIText();
            System.Diagnostics.Debug.WriteLine("SettingsPage: UpdateUIText вызван из OnNavigatedTo.");
        }

        private void InitializeLanguageSelection()
        {
            try
            {
                foreach (ComboBoxItem item in LanguageComboBox.Items)
                {
                    if (item.Tag as string == LocalizationManager.CurrentLanguage)
                    {
                        LanguageComboBox.SelectedItem = item;
                        System.Diagnostics.Debug.WriteLine($"SettingsPage: Выбран язык {item.Tag} в ComboBox.");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InitializeLanguageSelection: Ошибка: {ex.Message}");
            }
        }

        private void LocalizationManager_LanguageChanged(object sender, EventArgs e)
        {
            try
            {
                // Обновляем ResourceLoader для нового языка
                _resourceLoader = ResourceLoader.GetForViewIndependentUse("Resources");
                System.Diagnostics.Debug.WriteLine($"SettingsPage: ResourceLoader обновлён для языка {LocalizationManager.CurrentLanguage}.");

                // Обновляем текст UI-элементов
                UpdateUIText();
                System.Diagnostics.Debug.WriteLine("SettingsPage: UI обновлён после смены языка.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LocalizationManager_LanguageChanged: Ошибка: {ex.Message}");
            }
        }

        private void UpdateUIText()
        {
            try
            {
                // Обновляем текст всех элементов вручную
                string titleText = _resourceLoader.GetString("SettingsPage.Title");
                TitleTextBlock.Text = titleText;
                System.Diagnostics.Debug.WriteLine($"SettingsPage: Установлен текст для заголовка: {titleText}");

                string labelText = _resourceLoader.GetString("LanguageLabel.Text");
                LanguageLabel.Text = labelText;
                System.Diagnostics.Debug.WriteLine($"SettingsPage: Установлен текст для метки языка: {labelText}");

                string russianText = _resourceLoader.GetString("LanguageComboBox.Russian");
                RussianItem.Content = russianText;
                System.Diagnostics.Debug.WriteLine($"SettingsPage: Установлен текст для ru-RU: {russianText}");

                string englishText = _resourceLoader.GetString("LanguageComboBox.English");
                EnglishItem.Content = englishText;
                System.Diagnostics.Debug.WriteLine($"SettingsPage: Установлен текст для en-US: {englishText}");

                System.Diagnostics.Debug.WriteLine("SettingsPage: UpdateUIText успешно выполнен.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateUIText: Ошибка: {ex.Message}");
            }
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LanguageComboBox_SelectionChanged: Ошибка: {ex.Message}");
            }
        }

        private async void ShowErrorDialog(string message)
        {
            try
            {
                var dialog = new ContentDialog
                {
                    Title = LocalizationManager.GetString("ErrorDialog.Title"),
                    Content = message,
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
                System.Diagnostics.Debug.WriteLine("SettingsPage: ShowErrorDialog отображён.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShowErrorDialog: Ошибка: {ex.Message}");
            }
        }
    }
}