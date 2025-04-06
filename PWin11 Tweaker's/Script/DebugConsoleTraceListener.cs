using System;
using System.Diagnostics;

namespace PWin11_Tweaker_s.Script
{
    public class DebugConsoleTraceListener : TraceListener
    {
        private static bool _isListenerAdded = false;

        public event Action<string?>? OnMessageReceived;

        public DebugConsoleTraceListener()
        {
            if (!_isListenerAdded)
            {
                Trace.Listeners.Add(this);
                _isListenerAdded = true;
                Trace.WriteLine("DebugConsoleTraceListener: Слушатель добавлен в Trace.Listeners.");
            }
        }

        public override void Write(string? message)
        {
            Trace.WriteLine($"DebugConsoleTraceListener: Write вызван: {message ?? "null"}");
            OnMessageReceived?.Invoke(message);
        }

        public override void WriteLine(string? message)
        {
            Trace.WriteLine($"DebugConsoleTraceListener: WriteLine вызван: {message ?? "null"}");
            OnMessageReceived?.Invoke(message != null ? message + Environment.NewLine : Environment.NewLine);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Trace.Listeners.Remove(this);
                _isListenerAdded = false;
                Trace.WriteLine("DebugConsoleTraceListener: Слушатель удалён из Trace.Listeners.");
            }
            base.Dispose(disposing);
        }
    }
}