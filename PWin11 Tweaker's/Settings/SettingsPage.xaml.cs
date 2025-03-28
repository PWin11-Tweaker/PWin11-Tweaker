using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using PWin11_Tweaker_s.Script;

namespace PWin11_Tweaker_s
{
    public sealed partial class SettingsPage : Page
    {
        public ObservableCollection<string> DebugMessages => DebugLogger.LogMessages;
        public SettingsPage()
        {
            this.InitializeComponent();
            System.Diagnostics.Debug.WriteLine("SettingsPage: InitializeComponent завершён.");
        }
    }
}