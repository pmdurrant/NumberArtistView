using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace NumberArtistView.Services.Models
{

    //json response 
    public class Root
    {
        public int id { get; set; }
        public string fileName { get; set; }
        public string contentType { get; set; }
        public string storedFileName { get; set; }
        public DateTime uploadedAt { get; set; }
        public string appUserId { get; set; }
        public object appUser { get; set; }
        public long ReferenceDrawingId { get; set; }
    }

}
