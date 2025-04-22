using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PWin11_Tweaker_s.Script
{
    public class BloatwareItem : INotifyPropertyChanged
    {
        private string _name = "";
        private string _type = "";
        private long _size;
        private string _recommendation = "";
        private string _packageName = "";
        private bool _isSelected;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(); }
        }

        public long Size
        {
            get => _size;
            set { _size = value; OnPropertyChanged(); }
        }

        public string Recommendation
        {
            get => _recommendation;
            set { _recommendation = value; OnPropertyChanged(); }
        }

        public string PackageName
        {
            get => _packageName;
            set { _packageName = value; OnPropertyChanged(); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class StartupItem : INotifyPropertyChanged
    {
        private string _name;
        private string _path;
        private bool _isEnabled;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayText)); }
        }

        public string Path
        {
            get => _path;
            set { _path = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayText)); }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(); }
        }

        public string DisplayText
        {
            get
            {
                string convertedName = string.Empty;
                if (!string.IsNullOrEmpty(Path))
                {
                    try
                    {
                        convertedName = System.IO.Path.GetFileNameWithoutExtension(Path);
                    }
                    catch
                    {
                        convertedName = Name; // Если путь некорректен, используем Name
                    }
                }
                return string.IsNullOrEmpty(convertedName) ? Name : $"{convertedName} ({Path})";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}