using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace PWin11Tweaker
{
    public sealed partial class RebootWindow : Window
    {
        private AppWindow _appWindow;

        public RebootWindow()
        {
            this.InitializeComponent();
            InitializeWindow();
        }

        private void InitializeWindow()
        {

            var windowHandle = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
            _appWindow = AppWindow.GetFromWindowId(windowId);


            _appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 300, Height = 150 });


            var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest);
            int x = (displayArea.WorkArea.Width - 300) / 2;
            int y = (displayArea.WorkArea.Height - 150) / 2;
            _appWindow.Move(new Windows.Graphics.PointInt32 { X = x, Y = y });

            if (_appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
            }


            _appWindow.Title = "Перезагрузка системы";
        }

        private void RebootNowButton_Click(object sender, RoutedEventArgs e)
        {

            System.Diagnostics.Process.Start("shutdown", "/r /t 0");
            this.Close();
        }

        private void RebootLaterButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}