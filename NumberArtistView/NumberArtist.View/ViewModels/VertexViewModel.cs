using NumberArtistView.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumberArtistView.NumberArtist.View.ViewModels
{
    public class VertexViewModel:VertexModel
    {
        public bool IsSelected { get; set; }
        public string? DisplayText { get; set; }
        public int Index { get; set; }

    }
}
