using System;
using Windows.ApplicationModel.Resources;
using Windows.Storage;

namespace PWin11_Tweaker_s
{
    public static class LocalizationManager
    {
        private const string LanguageSettingKey = "AppLanguage";
        private static string _currentLanguage = "ru-RU";
        private static bool _isInitialized = false;

        public static event EventHandler LanguageChanged;

        public static void Initialize()
        {
            if (_isInitialized)
            {
                System.Diagnostics.Debug.WriteLine("LocalizationManager: Уже инициализирован, пропускаем повторную инициализацию.");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("LocalizationManager: Начало инициализации.");

                string savedLanguage = ApplicationData.Current.LocalSettings.Values[LanguageSettingKey] as string;
                System.Diagnostics.Debug.WriteLine($"LocalizationManager: Сохранённый язык: {savedLanguage ?? "не установлен"}");

                _currentLanguage = savedLanguage ?? "ru-RU";
                System.Diagnostics.Debug.WriteLine($"LocalizationManager: Установлен язык: {_currentLanguage}");

                _isInitialized = true;
                System.Diagnostics.Debug.WriteLine("LocalizationManager: Инициализация успешно завершена.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LocalizationManager: Ошибка при инициализации: {ex.Message} StackTrace: {ex.StackTrace}");
                _currentLanguage = "ru-RU";
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

                if (_currentLanguage == value)
                {
                    System.Diagnostics.Debug.WriteLine($"LocalizationManager: Язык уже установлен на {value}, пропускаем.");
                    return;
                }

                try
                {
                    System.Diagnostics.Debug.WriteLine($"LocalizationManager: Смена языка на {value}");
                    _currentLanguage = value;

                    ApplicationData.Current.LocalSettings.Values[LanguageSettingKey] = value;
                    System.Diagnostics.Debug.WriteLine("LocalizationManager: Язык сохранён в LocalSettings.");

                    LanguageChanged?.Invoke(null, EventArgs.Empty);
                    System.Diagnostics.Debug.WriteLine("LocalizationManager: Событие LanguageChanged вызвано.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"LocalizationManager: Ошибка при смене языка: {ex.Message} StackTrace: {ex.StackTrace}");
                }
            }
        }

        public static string GetString(string resourceKey)
        {
            if (!_isInitialized)
                Initialize();

            if (string.IsNullOrEmpty(resourceKey))
            {
                System.Diagnostics.Debug.WriteLine("LocalizationManager: Ключ ресурса пустой или null.");
                return string.Empty;
            }

            try
            {
                // Указываем язык вручную при создании ResourceLoader
                var resourceLoader = ResourceLoader.GetForViewIndependentUse($"Strings/{_currentLanguage}/Resources");
                string result = resourceLoader.GetString(resourceKey);

                if (string.IsNullOrEmpty(result))
                {
                    System.Diagnostics.Debug.WriteLine($"LocalizationManager: Ресурс {resourceKey} не найден для языка {_currentLanguage}.");
                    return resourceKey;
                }

                System.Diagnostics.Debug.WriteLine($"LocalizationManager: Ресурс {resourceKey} найден: {result}");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LocalizationManager: Ошибка при получении ресурса {resourceKey}: {ex.Message} StackTrace: {ex.StackTrace}");
                return resourceKey;
            }
        }
    }
}