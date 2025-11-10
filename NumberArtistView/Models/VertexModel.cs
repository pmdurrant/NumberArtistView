using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Business.Objects
{
    public class VertexModel
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Bulge { get; set; }
    }

    public class Pline2DModel
    {
        public List<VertexModel> Vertices { get; set; } = new List<VertexModel>();
        public bool IsClosed { get; set; }
        public string Layer { get; set; }
    }
}
