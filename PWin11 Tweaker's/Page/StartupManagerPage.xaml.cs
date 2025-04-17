using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PWin11_Tweaker_s.Page
{
    public sealed partial class StartupManagerPage : Microsoft.UI.Xaml.Controls.Page
    {
        public StartupManagerPage()
        {
            this.InitializeComponent();
            LoadStartupPrograms();
            LoadServices();
        }

        private void LoadStartupPrograms()
        {
            var startupItems = new List<(string Name, bool IsEnabled)>();

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
            {
                if (key != null)
                {
                    foreach (string name in key.GetValueNames())
                    {
                        string path = key.GetValue(name).ToString();
                        bool isEnabled = true;
                        startupItems.Add((name, isEnabled));
                    }
                }
            }

            if (startupItems.Count == 0)
            {
                startupItems.Add(("Notepad", true));
                startupItems.Add(("Explorer", false));
            }

            foreach (var item in startupItems)
            {
                var grid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) },
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                    }
                };
                var nameText = new TextBlock
                {
                    Text = item.Name,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                var toggle = new ToggleSwitch
                {
                    IsOn = item.IsEnabled,
                    OnContent = "Вкл",
                    OffContent = "Выкл",
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                toggle.Toggled += ToggleStartupProgram;
                grid.Children.Add(nameText);
                grid.Children.Add(toggle);
                Grid.SetColumn(nameText, 0);
                Grid.SetColumn(toggle, 1);
                StartupProgramsList.Items.Add(grid);
            }
        }

        private void LoadServices()
        {
            var serviceItems = new List<(string Name, bool IsEnabled)>();

            serviceItems.Add(("Service1", true));
            serviceItems.Add(("Service2", false));

            foreach (var item in serviceItems)
            {
                var grid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) },
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                    }
                };
                var nameText = new TextBlock
                {
                    Text = item.Name,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                var toggle = new ToggleSwitch
                {
                    IsOn = item.IsEnabled,
                    OnContent = "Вкл",
                    OffContent = "Выкл",
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                toggle.Toggled += ToggleStartupProgram;
                grid.Children.Add(nameText);
                grid.Children.Add(toggle);
                Grid.SetColumn(nameText, 0);
                Grid.SetColumn(toggle, 1);
                ServicesList.Items.Add(grid);
            }
        }

        private void ToggleStartupProgram(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggle)
            {
                var grid = (Grid)toggle.Parent;
                var nameText = (TextBlock)grid.Children[0];
                bool isEnabled = toggle.IsOn;
                Debug.WriteLine($"Toggle Startup Program: {nameText.Text} to {isEnabled}");
            }
        }

        private void ToggleService(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggle)
            {
                var grid = (Grid)toggle.Parent;
                var nameText = (TextBlock)grid.Children[0];
                bool isEnabled = toggle.IsOn;
                Debug.WriteLine($"Toggle Service: {nameText.Text} to {isEnabled}");
            }
        }
    }
}