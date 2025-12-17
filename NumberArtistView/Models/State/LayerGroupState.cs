using System;
using SQLite;

namespace NumberArtistView.Models
{
    [Table("LayerGroupStates")]
    public class LayerGroupState
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public Guid UserId { get; set; }

        public string DxfFileName { get; set; }

        public string LayerName { get; set; }

        public int LayerIndex { get; set; }

        public bool IsVisible { get; set; }

        public int ColorR { get; set; }

        public int ColorG { get; set; }

        public int ColorB { get; set; }

        public int ColorA { get; set; }

        public DateTime LastModified { get; set; }
    }
}