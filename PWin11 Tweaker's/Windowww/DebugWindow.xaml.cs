using System;
using System.Diagnostics;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PWin11_Tweaker_s.Script;

namespace PWin11_Tweaker_s.Windowww
{
    public sealed partial class DebugWindow : Window
    {
        private readonly DebugConsoleTraceListener _listener;
        private ScrollViewer? _scrollViewer;

        public DebugWindow(Window? parentWindow)
        {
            this.InitializeComponent();
            this.Title = "Debug Console";
            this.AppWindow.Resize(new Windows.Graphics.SizeInt32(600, 400));

            _listener = new DebugConsoleTraceListener();
            _listener.OnMessageReceived += AppendMessage;

            // Находим ScrollViewer внутри TextBox после загрузки
            ConsoleOutput.Loaded += (s, e) =>
            {
                _scrollViewer = FindScrollViewer(ConsoleOutput);
                if (_scrollViewer != null)
                {
                    Trace.WriteLine("DebugWindow: ScrollViewer найден.");
                }
            };

            Trace.WriteLine("DebugWindow: Окно отладки открыто.");

            this.Closed += DebugWindow_Closed;
        }

        private void AppendMessage(string? message)
        {
            if (string.IsNullOrEmpty(message))
            {
                Trace.WriteLine("DebugWindow: Получено пустое сообщение, пропускаем.");
                return;
            }

            Trace.WriteLine($"DebugWindow: Получено сообщение: {message}");
            this.DispatcherQueue.TryEnqueue(() =>
            {
                ConsoleOutput.Text += message;
                ScrollToBottom();
                Trace.WriteLine($"DebugWindow: Текст добавлен в ConsoleOutput: {message}");
            });
        }

        private void ScrollToBottom()
        {
            if (_scrollViewer != null)
            {
                _scrollViewer.ChangeView(null, _scrollViewer.ScrollableHeight, null, true);
            }
            else
            {
                // Альтернативный способ, если ScrollViewer не найден
                ConsoleOutput.Select(ConsoleOutput.Text.Length, 0);
                Trace.WriteLine("DebugWindow: ScrollViewer не найден, использован альтернативный метод прокрутки.");
            }
        }

        private ScrollViewer? FindScrollViewer(DependencyObject depObj)
        {
            if (depObj is ScrollViewer scrollViewer)
            {
                return scrollViewer;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                var result = FindScrollViewer(child);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private void DebugWindow_Closed(object sender, WindowEventArgs args)
        {
            _listener.OnMessageReceived -= AppendMessage;
            Trace.WriteLine("DebugWindow: Окно закрыто.");
        }
    }
}