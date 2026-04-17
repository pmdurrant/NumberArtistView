using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NumberArtistView.Models
{
    // Small DTO representing a group of closed polylines for a given layer.
    public class LayerClosedGroup : INotifyPropertyChanged
    {
        private string _layerName = string.Empty;
        private List<Pline2DModel> _closedPlines = new List<Pline2DModel>();
        private int _closedPlinesCount;
        private bool _isPainted = false;

        public string LayerName
        {
            get => _layerName;
            set => SetProperty(ref _layerName, value);
        }

        // Changed event requested: consumers can subscribe to be notified whenever any property changes.
        public event EventHandler? Changed;

        // Use SetProperty so changes to IsPainted raise notifications.
        public bool IsPainted
        {
            get => _isPainted;
            set => SetProperty(ref _isPainted, value);
        }

        public List<Pline2DModel> ClosedPlines
        {
            get => _closedPlines;
            set
            {
                if (SetProperty(ref _closedPlines, value))
                {
                    ClosedPlinesCount = _closedPlines?.Count ?? 0;
                }
            }
        }

        public int ClosedPlinesCount
        {
            get => _closedPlinesCount;
            set => SetProperty(ref _closedPlinesCount, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value!;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            OnChanged();
            return true;
        }

        // Protected helper to raise Changed so derived types can override or call it.
        protected virtual void OnChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        //public static implicit operator ObservableCollection<object>(LayerClosedGroup v)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
