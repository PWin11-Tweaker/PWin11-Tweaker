using System;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace PWin11_Tweaker_s.Script
{
    public static class DebugLogger
    {
        private static readonly ObservableCollection<string> _logMessages = new ObservableCollection<string>();
        private static DebugTraceListener _traceListener;

        public static ObservableCollection<string> LogMessages => _logMessages;

        public static void Initialize()
        {
            // Удаляем старый listener, если он был добавлен
            if (_traceListener != null)
            {
                Trace.Listeners.Remove(_traceListener);
            }

            // Создаём новый listener и добавляем его
            _traceListener = new DebugTraceListener();
            Trace.Listeners.Add(_traceListener);

            Debug.WriteLine("DebugLogger: Инициализация завершена.");
            Debug.WriteLine($"DebugLogger: Количество listeners: {Trace.Listeners.Count}");
        }

        private static void AddMessage(string message)
        {
            string formattedMessage = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";

            if (App.Current is App app && app.DispatcherQueue != null)
            {
                Debug.WriteLine("DebugLogger: Добавление сообщения через DispatcherQueue.");
                app.DispatcherQueue.TryEnqueue(() =>
                {
                    Debug.WriteLine($"DebugLogger: Сообщение добавлено: {formattedMessage}");
                    _logMessages.Add(formattedMessage);
                });
            }
            else
            {
                Debug.WriteLine("DebugLogger: DispatcherQueue недоступен, добавление напрямую.");
                _logMessages.Add(formattedMessage);
            }
        }

        private class DebugTraceListener : TraceListener
        {
            public override void Write(string message)
            {
                Debug.WriteLine("DebugTraceListener: Вызван Write.");
                AddMessage(message);
            }

            public override void WriteLine(string message)
            {
                Debug.WriteLine("DebugTraceListener: Вызван WriteLine.");
                AddMessage(message);
            }
        }
    }
}