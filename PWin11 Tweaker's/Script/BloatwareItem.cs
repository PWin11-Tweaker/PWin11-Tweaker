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
}