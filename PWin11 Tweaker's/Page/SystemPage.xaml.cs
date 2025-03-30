using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using Microsoft.Win32;
using System.Security.Principal;

namespace PWin11_Tweaker_s
{
    public sealed partial class SystemPage : Page
    {
        public SystemPage()
        {
            this.InitializeComponent();

            // Подписка на события элементов
            DisableServicesButton.Click += DisableUnnecessaryServices_Click;
            DisableUACCheckBox.Checked += DisableUAC_Click;
            DisableUACCheckBox.Unchecked += DisableUAC_Click;
            DisableClipboardCheckBox.Checked += DisableClipboardTracking_Click;
            DisableClipboardCheckBox.Unchecked += DisableClipboardTracking_Click;
            SpeedUpWindowsButton.Click += SpeedUpWindows_Click;
        }

        // Проверка запуска от имени администратора
        private bool IsUserAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        // 1. Отключение ненужных служб
        private void DisableUnnecessaryServices_Click(object sender, RoutedEventArgs e)
        {
            if (!IsUserAdministrator())
            {
                Debug.WriteLine("Ошибка: Перезапустите программу от имени администратора!");
                return;
            }
            
            string[] services = { "WSearch", "Fax", "Spooler", "RemoteRegistry", "WaaSMedicSvc" };
            foreach (var service in services)
            {
                try
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey($"SYSTEM\\CurrentControlSet\\Services\\{service}", true))
                    {
                        key?.SetValue("Start", 4, RegistryValueKind.DWord); // 4 = отключено
                    }
                    Debug.WriteLine($"Служба {service} отключена.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка при отключении {service}: {ex.Message}");
                }
            }
        }

        // 2. Отключение UAC
        private void DisableUAC_Click(object sender, RoutedEventArgs e)
        {
            if (!IsUserAdministrator())
            {
                Debug.WriteLine("Ошибка: Запустите программу от имени администратора!");
                return;
            }

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", true))
                {
                    key?.SetValue("EnableLUA", ((CheckBox)sender).IsChecked == true ? 0 : 1, RegistryValueKind.DWord);
                }
                Debug.WriteLine("UAC успешно изменён.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при отключении UAC: {ex.Message}");
            }
        }

        // 3. Отключение истории буфера обмена
        private void DisableClipboardTracking_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Clipboard"))
                {
                    key.SetValue("EnableClipboardHistory", ((CheckBox)sender).IsChecked == true ? 0 : 1, RegistryValueKind.DWord);
                }
                Debug.WriteLine("История буфера обмена отключена.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при отключении буфера обмена: {ex.Message}");
            }
        }

        // 4. Ускорение работы Windows
        private void SpeedUpWindows_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects"))
                {
                    key.SetValue("VisualFXSetting", 2, RegistryValueKind.DWord);
                }
                
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Serialize"))
                {
                    key.SetValue("StartupDelayInMSec", 0, RegistryValueKind.DWord);
                }
                
                Debug.WriteLine("Windows ускорен.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при ускорении Windows: {ex.Message}");
            }
        }
    }
}