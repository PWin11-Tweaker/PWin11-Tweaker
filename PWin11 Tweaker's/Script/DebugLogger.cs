using System;
using System.Collections.ObjectModel;
using System.Diagnostics;


namespace PWin11_Tweaker_s.Script;


public static class DebugLogger
{
    private static readonly ObservableCollection<string> _logMessages = new ObservableCollection<string>();
    
    public static ObservableCollection<string> LogMessages => _logMessages;

    public static void Initialize()
    {
        Trace.Listeners.Add(new DebugTraceListener());
    }


    private static void AddMessage(string message)
    {
        if (App.MainWindowInstance != null)
        {
            App.MainWindowInstance.DispatcherQueue.TryEnqueue(() =>
            {
                _logMessages.Add($"{DateTime.Now}: {message}");
            });
        }
        else
        {
            _logMessages.Add($"{DateTime.Now}: {message}");
        }
    }

    private class DebugTraceListener : TraceListener
    {
        public override void Write(string message)
        {
            AddMessage(message);
        }
        public override void WriteLine(string message)
        {
            AddMessage(message);
        }
    }
    
}