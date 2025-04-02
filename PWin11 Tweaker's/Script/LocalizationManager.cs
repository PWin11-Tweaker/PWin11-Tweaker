using System;
using Windows.ApplicationModel.Resources;
using Windows.Storage;

namespace PWin11_Tweaker_s
{
    public static class LocalizationManager
    {
        private static ResourceLoader _resourceLoader;
        private const string LanguageSettingKey = "AppLanguage";
        private static string _currentLanguage;
        private static bool _isInitialized = false;

        public static void Initialize()
        {
            if (_isInitialized)
                return;

            try
            {
                System.Diagnostics.Debug.WriteLine("LocalizationManager: Начало инициализации.");

                // Загружаем сохранённый язык или используем русский по умолчанию
                string savedLanguage = null;
                try
                {
                    savedLanguage = ApplicationData.Current.LocalSettings.Values[LanguageSettingKey] as string;
                    System.Diagnostics.Debug.WriteLine($"LocalizationManager: Сохранённый язык: {savedLanguage ?? "не установлен"}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"LocalizationManager: Ошибка при чтении LocalSettings: {ex.Message}\nStackTrace: {ex.StackTrace}");
                }

                _currentLanguage = savedLanguage ?? "ru-RU";
                System.Diagnostics.Debug.WriteLine($"LocalizationManager: Установлен язык: {_currentLanguage}");

                UpdateResourceLoader();
                System.Diagnostics.Debug.WriteLine("LocalizationManager: Инициализация завершена.");

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LocalizationManager: Ошибка при инициализации: {ex.Message}\nStackTrace: {ex.StackTrace}");
                _currentLanguage = "ru-RU";
                UpdateResourceLoader();
                _isInitialized = true;
            }
        }

        public static string CurrentLanguage
        {
            get
            {
                if (!_isInitialized)
                    Initialize();
                return _currentLanguage;
            }
            set
            {
                if (!_isInitialized)
                    Initialize();

                try
                {
                    if (_currentLanguage != value)
                    {
                        System.Diagnostics.Debug.WriteLine($"LocalizationManager: Смена языка на {value}");
                        _currentLanguage = value;

                        try
                        {
                            ApplicationData.Current.LocalSettings.Values[LanguageSettingKey] = value;
                            System.Diagnostics.Debug.WriteLine("LocalizationManager: Язык сохранён в LocalSettings.");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"LocalizationManager: Ошибка при сохранении языка в LocalSettings: {ex.Message}");
                        }

                        UpdateResourceLoader();
                        LanguageChanged?.Invoke(null, EventArgs.Empty);
                        System.Diagnostics.Debug.WriteLine("LocalizationManager: Событие LanguageChanged вызвано.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"LocalizationManager: Ошибка при установке языка: {ex.Message}");
                }
            }
        }

        public static event EventHandler LanguageChanged;

        private static void UpdateResourceLoader()
        {
            try
            {
                _resourceLoader = ResourceLoader.GetForViewIndependentUse($"Strings/{_currentLanguage}/Resources");
                System.Diagnostics.Debug.WriteLine($"LocalizationManager: ResourceLoader обновлён для языка {_currentLanguage}.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LocalizationManager: Ошибка при обновлении ResourceLoader: {ex.Message}");
                if (_currentLanguage != "ru-RU")
                {
                    _currentLanguage = "ru-RU";
                    _resourceLoader = ResourceLoader.GetForViewIndependentUse($"Strings/{_currentLanguage}/Resources");
                    System.Diagnostics.Debug.WriteLine("LocalizationManager: Установлен язык по умолчанию (ru-RU) после ошибки.");
                }
            }
        }

        public static string GetString(string resourceKey)
        {
            if (!_isInitialized)
                Initialize();

            try
            {
                string result = _resourceLoader.GetString(resourceKey);
                if (string.IsNullOrEmpty(result))
                {
                    System.Diagnostics.Debug.WriteLine($"LocalizationManager: Ресурс {resourceKey} не найден для языка {_currentLanguage}.");
                }
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LocalizationManager: Ошибка при получении ресурса {resourceKey}: {ex.Message}");
                return resourceKey;
            }
        }
    }
}