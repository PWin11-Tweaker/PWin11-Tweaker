using Microsoft.UI.Xaml;
using Microsoft.UI.Dispatching;
using System;
using Windows.UI.Popups; // Для MessageDialog
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
        public static MainWindow MainWindowInstance { get; private set; }

        public DispatcherQueue DispatcherQueue { get; private set; }

        public App()
        {
            try
            {
                this.InitializeComponent();
                System.Diagnostics.Debug.WriteLine("App: Инициализация завершена.");
                LocalizationManager.Initialize();
                DispatcherQueue = DispatcherQueue.GetForCurrentThread();
                DebugLogger.Initialize();
                PWin11_Tweaker_s.Script.DebugLogger.Initialize();
            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText("app_init_error.log",
                    $"Ошибка в App: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        // Указываем, что sender может быть null
        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            // Логируем асинхронные ошибки
            System.Diagnostics.Debug.WriteLine($"Асинхронная ошибка: {e.Exception.Message}\nСтек: {e.Exception.StackTrace}");
            e.SetObserved(); // Помечаем как обработанное
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("App.OnLaunched: Запуск приложения.");

                // Запускаем SplashScreen
                Window splashScreen = new SplashScreen();
                splashScreen.Activate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"App.OnLaunched: Ошибка при запуске приложения: {ex.Message}");
            }
        }


        private async Task InitializeLocalizer()
        {
            // Initialize a "Strings" folder in the executables folder.
            string stringsFolderPath = Path.Combine(AppContext.BaseDirectory, "Strings");
            StorageFolder stringsFolder = await StorageFolder.GetFolderFromPathAsync(stringsFolderPath);

            ILocalizer localizer = await new LocalizerBuilder()
                .AddStringResourcesFolderForLanguageDictionaries(stringsFolderPath)
                .SetOptions(options =>
                {
                    options.DefaultLanguage = "en-US";
                })
                .Build();
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