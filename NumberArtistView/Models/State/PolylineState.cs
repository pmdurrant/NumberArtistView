using System;
using SQLite;

namespace NumberArtistView.Models
{
    [Table("PolylineStates")]
    public class PolylineState
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public Guid UserId { get; set; }

        public string DxfFileName { get; set; }

        public string LayerName { get; set; }

        public int PolylineIndex { get; set; }

        public bool IsPainted { get; set; }

        public DateTime LastModified { get; set; }
    }
}