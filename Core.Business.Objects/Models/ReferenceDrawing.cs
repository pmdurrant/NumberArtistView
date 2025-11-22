using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Business.Objects.Models
{
    public class ReferenceDrawing
    {
        public ReferenceDrawing() {
            Id = Guid.NewGuid().GetHashCode();
            Name = string.Empty;
        }
        public ReferenceDrawing(string name) {
            Name = name;
        }
        public string Name { get; set; }
        public int DxfFileId { get; set; }

        public int Id { get; private set; }
    }
}
