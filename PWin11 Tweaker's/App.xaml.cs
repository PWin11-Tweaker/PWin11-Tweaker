using Microsoft.UI.Xaml;
using Microsoft.UI.Dispatching;
using System;
using Windows.UI.Popups;
using System.Threading.Tasks;
using PWin11_Tweaker_s.Script;
using Windows.Storage;
using WinUI3Localizer;
using System.IO;

namespace PWin11_Tweaker_s
{
    public partial class App : Application
    {
        private const string ThemePreferenceKey = "ThemePreference";
        public static MainWindow? MainWindowInstance { get; private set; }

        public DispatcherQueue DispatcherQueue { get; private set; }
        public static Window MainWindow { get; internal set; }

        public App()
        {
            try
            {
                this.InitializeComponent();

                System.Diagnostics.Debug.WriteLine("App: Инициализация завершена.");
                LocalizationManager.Initialize();
                DispatcherQueue = DispatcherQueue.GetForCurrentThread();
            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText("app_init_error.log",
                    $"Ошибка в App: {ex.Message} StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Асинхронная ошибка: {e.Exception.Message} Стек: {e.Exception.StackTrace}");
            e.SetObserved();
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("App.OnLaunched: Запуск приложения.");

                // Собираем WinUI3Localizer ДО показа первого окна, чтобы все
                // элементы с l:Uids.Uid сразу получили нужный (сохранённый) язык.
                await InitializeLocalizer();

                Window splashScreen = new SplashScreen();
                splashScreen.Activate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"App.OnLaunched: Ошибка при запуске приложения: {ex.Message}");
            }
        }

        private static async Task InitializeLocalizer()
        {
            if (LocalizerBuilder.IsLocalizerAlreadyBuilt)
            {
                return;
            }

            // "Strings" folder next to the executable (unpackaged app).
            string stringsFolderPath = Path.Combine(AppContext.BaseDirectory, "Strings");

            await new LocalizerBuilder()
                .AddStringResourcesFolderForLanguageDictionaries(stringsFolderPath)
                .SetOptions(options =>
                {
                    // Ранее выбранный пользователем язык (или язык по умолчанию),
                    // сохранённый в LocalSettings через LocalizationManager.
                    options.DefaultLanguage = LocalizationManager.CurrentLanguage;
                })
                .Build();

            System.Diagnostics.Debug.WriteLine($"App: WinUI3Localizer собран, язык: {LocalizationManager.CurrentLanguage}");
        }

        private static async Task CreateStringResourceFileIfNotExists(StorageFolder stringsFolder, string language, string resourceFileName)
        {
            StorageFolder languageFolder = await stringsFolder.CreateFolderAsync(
                language,
                CreationCollisionOption.OpenIfExists);

            if (await languageFolder.TryGetItemAsync(resourceFileName) is null)
            {
                string resourceFilePath = Path.Combine(stringsFolder.Name, language, resourceFileName);
                StorageFile resourceFile = await LoadStringResourcesFileFromAppResource(resourceFilePath);
                _ = await resourceFile.CopyAsync(languageFolder);
            }
        }

        private static async Task<StorageFile> LoadStringResourcesFileFromAppResource(string filePath)
        {
            Uri resourcesFileUri = new($"ms-appx:///{filePath}");
            return await StorageFile.GetFileFromApplicationUriAsync(resourcesFileUri);
        }

        // Метод для установки MainWindow и начальной темы
        public static void InitializeMainWindow(MainWindow mainWindow)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("App.InitializeMainWindow: Инициализация MainWindow.");
                MainWindowInstance = mainWindow;

                // Устанавливаем начальную тему
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                var themePreference = localSettings.Values[ThemePreferenceKey]?.ToString();
                System.Diagnostics.Debug.WriteLine($"App.InitializeMainWindow: Загружено сохранённое значение темы: {themePreference}");

                if (mainWindow.Content is FrameworkElement rootElement)
                {
                    if (themePreference == "Dark")
                    {
                        System.Diagnostics.Debug.WriteLine("App.InitializeMainWindow: Устанавливаем тёмную тему.");
                        rootElement.RequestedTheme = ElementTheme.Dark;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("App.InitializeMainWindow: Устанавливаем светлую тему.");
                        rootElement.RequestedTheme = ElementTheme.Light;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("App.InitializeMainWindow: Не удалось найти корневой элемент.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"App.InitializeMainWindow: Ошибка: {ex.Message}");
            }
        }
    }
}