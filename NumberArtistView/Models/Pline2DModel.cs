using System.ComponentModel;
using System.Security.Cryptography;

namespace NumberArtistView.Models
{
    public class Pline2DModel:INotifyPropertyChanged
    {
        public int Id { get; set; }    
        public bool IsClosed { get; set; }
        public required string Layer { get; set; }
        public required object LayerColour { get; set; }
        public required object Vertices { get; set; }
      //  public Action<object?, PropertyChangedEventArgs> PropertyChanged { get; internal set; }

        /// <summary>
        /// Indicates whether this vertex has been painted/rendered.
        /// </summary>
        public bool IsPainted { get; set; } = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}