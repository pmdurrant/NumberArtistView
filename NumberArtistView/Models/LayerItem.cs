using Core.Business.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Graphics;

namespace NumberArtistView.Models
{
    public class LayerItem : INotifyPropertyChanged
    {
        public LayerItem() 
        {
            ColourSelection = new List<LayerColorObject>();
            ColourSelection.Add(new LayerColorObject() { R = 99, G = 122, B = 150 });
            ColourSelection.Add(new LayerColorObject() { R = 166, G = 173, B = 114 });
            ColourSelection.Add(new LayerColorObject() { R = 31, G = 117, B = 45 });
            ColourSelection.Add(new LayerColorObject() { R = 102, G = 52, B = 95 });
            ColourSelection.Add(new LayerColorObject() { R = 21, G = 142, B = 8 });
            ColourSelection.Add(new LayerColorObject() { R = 29, G = 61, B = 17 });
            ColourSelection.Add(new LayerColorObject() { R = 155, G = 242, B = 195 });
            ColourSelection.Add(new LayerColorObject() { R = 72, G = 152, B = 201 });
            ColourSelection.Add(new LayerColorObject() { R = 13, G = 119, B = 116 });
            ColourSelection.Add(new LayerColorObject() { R = 1, G = 47, B = 96 });
            ColourSelection.Add(new LayerColorObject() { R = 104, G = 201, B = 140 }); 
            ColourSelection.Add(new LayerColorObject() { R = 147, G = 39, B = 79 });
            ColourSelection.Add(new LayerColorObject() { R = 163, G = 163, B = 163 });
            ColourSelection.Add(new LayerColorObject() { R = 71, G = 109, B = 58 });
            ColourSelection.Add(new LayerColorObject() { R = 158, G = 148, B = 152 });
            ColourSelection.Add(new LayerColorObject() { R = 48, G = 49, B = 56 });
            ColourSelection.Add(new LayerColorObject() { R = 76, G = 174, B = 196 });
            ColourSelection.Add(new LayerColorObject() { R = 9, G = 6, B = 10 });
            ColourSelection.Add(new LayerColorObject() { R = 165, G = 125, B = 87 });
            ColourSelection.Add(new LayerColorObject() { R = 155, G = 110, B = 145 });
            ColourSelection.Add(new LayerColorObject() { R = 215, G = 216, B = 224 });
            ColourSelection.Add(new LayerColorObject() { R = 242, G = 38, B = 242 });
            ColourSelection.Add(new LayerColorObject() { R = 137, G = 128, B = 99 });
            ColourSelection.Add(new LayerColorObject() { R = 41, G = 232, B = 16 });  
        }

        public LayerColorObject LayerColour { get; set; }
        public Color color { get; set; }
        public int LayerIndex { get; set; }
        public string LayerName { get; set; }
        public List<LayerColorObject> ColourSelection { get; set; }

        // New visibility flag for the layer
        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        protected bool SetProperty<T>(ref T backingField, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingField, value))
                return false;
            backingField = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
