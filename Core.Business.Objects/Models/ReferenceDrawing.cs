using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace Core.Business.Objects.Models
{
    public class ReferenceDrawing
    {
        public ReferenceDrawing()
        {
            Name = string.Empty;
        }

     
        public ReferenceDrawing(string name)
        {
            Name = name;
        }

        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; }
        public string StoredFileName { get; set; }
        public int DxfFileId { get; set; }
    }
}
