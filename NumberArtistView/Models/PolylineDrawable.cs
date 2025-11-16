using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Graphics;

namespace NumberArtistView.Models
{
    // Minimal implementation based on usage in MainPage.xaml.cs
    public class PolylineDrawable : IDrawable
    {
        public List<Pline2DModel> Polylines { get; set; } = new List<Pline2DModel>();
        public string SelectedLayer { get; set; }

        public IEnumerable<string> Layers => Polylines.Select(p => p.Layer).Distinct();

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            // Simplified loop using Select as per S3267
            var verticesGroups = Polylines
                .Where(p => SelectedLayer == null || p.Layer == SelectedLayer)
                .Select(p => p.Vertices);

            foreach (var vertices in verticesGroups)
            {
                // Assuming Vertices is a List<PointF> or similar
                if (vertices is IEnumerable<object> vertexList && vertexList.Count() < 2)
                    continue;

                // Fix: Cast vertices to IEnumerable<dynamic> before using LINQ methods
                var points = (vertices as IEnumerable<dynamic>)
                    .Select(v => new PointF((float)v.X, (float)v.Y))
                    .ToArray();

                canvas.StrokeColor = Colors.Red;

                // Fix: Use PathF to draw polygon since ICanvas does not have DrawPolygon
                if (points.Length > 1)
                {
                    var path = new PathF();
                    path.MoveTo(points[0]);
                    for (int i = 1; i < points.Length; i++)
                        path.LineTo(points[i]);
                    path.Close();
                    canvas.DrawPath(path);
                }
            }
        }
    }
}