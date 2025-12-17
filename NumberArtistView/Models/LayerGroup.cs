using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace NumberArtistView.Models
{
    public partial class LayerGroup : INotifyPropertyChanged
    {
        private string _layerName = string.Empty;
        private bool _isPainted = false;

        public string LayerName
        {
            get => _layerName;
            set
            {
                if (value == _layerName) return;
                _layerName = value;
                OnPropertyChanged();
            }
        }

        public bool IsPainted
        {
            get => _isPainted;
            set
            {
                if (value == _isPainted) return;
                _isPainted = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
