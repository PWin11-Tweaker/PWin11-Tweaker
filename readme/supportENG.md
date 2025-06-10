# PWin11 Tweaker Error Table

Below is a list of errors that may occur while using the PWin11 Tweaker application, along with their codes, descriptions, possible causes, and recommendations for resolution.

If you encounter these errors, please contact the developer via Issues on GitHub or through the Telegram channel (which can be found on my GitHub profile or within the application itself).

| Error Code | Error Description                            | Possible Cause                                                                    | Recommendation for Users                                                                      |
|------------|----------------------------------------------|-----------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------|
| E001       | The program is not running as an administrator | The application requires administrator privileges to perform certain functions.   | Run the application as an administrator: right-click on the .exe file and select "Run as administrator." |
| E002       | Invalid URL format                           | The URL specified in the application is incorrect (e.g., for opening a GitHub page). | Ensure the URL in the application settings is correct (e.g., https://github.com/PWin11-Tweaker/PWin11-Tweaker). If the issue persists, contact the developer. |
| E003       | Failed to open the page in the browser       | The system does not have a default browser set, or there is no internet connection. | Ensure you have a default browser set (e.g., Edge, Chrome) and an active internet connection. Try opening the link manually. |
| E004       | Error setting a custom icon                  | The `logo.ico` file is missing in the `Assets` folder.                            | Ensure the `logo.ico` file is present in the application's `Assets` folder. If it’s missing, contact the developer. |
| E005       | MicaBackdrop is not supported                | The current version of Windows does not support the Mica effect (e.g., Windows 10). | Update Windows to version 11 or higher. If updating is not possible, contact the developer to disable this feature. |
| E006       | Navigation error: page not found             | One of the application pages (e.g., HomePage, SettingsPage) could not be loaded.   | Restart the application. If the error persists, contact the developer.                        |
| E007       | Navigation error: ContentFrame not initialized | An internal application error related to navigation between pages.               | Restart the application. If the error persists, contact the developer.                        |
| E008       | Navigation error: unknown page tag           | The selected menu item has an incorrect tag that does not match any page.         | Restart the application and try again. If the error persists, contact the developer.          |
| E009       | Error saving theme settings                  | Failed to save theme settings to local storage.                                   | Ensure the application has permission to write to local settings. Restart the application.    |
| E010       | Operation canceled                           | An asynchronous operation (e.g., opening a page) was canceled.                    | Try again. If the error persists, check system stability and contact the developer.           |
| E011       | General application error                    | An unknown error related to the application's operation.                          | Restart the application. If the error persists, contact the developer with a description of the issue and logs (if available). |


| SplashScreen error code | Error description | Possible cause | Recommendation for a programmer |
|-------------------------|-------------------|----------------|---------------------------------|
| S001                    | The 'SplashImage' element was not found. | Change or check the image path for SplashImage | Check the path to the image in the SplashScreen.xaml.cs script |
| S002                    | The root element is not a Grid. | Xaml must have a Grid, not another markup. | Check the Splash Screen.xaml markup | 
| S003                    | The AppWindow could not be initialized. | Couldn't start the window                              |    Check the code                     | 



| The error code in TempCleanerPage | Error description | Possible cause | Recommendation for users |
|------------------------------|-----------------|-------------------|--------------------------------|
|Error near ComboBox| Where should the size be, how much should be cleared is written Error | Program(PWin11 Tweaker) not running as an administrator | Restart the program as an administrator |
