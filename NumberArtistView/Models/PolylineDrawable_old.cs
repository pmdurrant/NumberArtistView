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

        // new: set of visible layer names (null means all visible)
        public HashSet<string>? VisibleLayers { get; set; }

        public IEnumerable<string> Layers => Polylines.Select(p => p.Layer).Distinct();

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var colorlist = new ColourSelectionList();
            var fallback = Microsoft.Maui.Graphics.Colors.Black;

            // Filter polylines by selected layer (if any) and VisibleLayers (if provided)
            var polylinesToDraw = Polylines
                .Where(p => (SelectedLayer == null || p.Layer == SelectedLayer)
                         && (VisibleLayers == null || VisibleLayers.Contains(p.Layer)));

            foreach (var pline in polylinesToDraw)
            {
                var vertices = pline.Vertices;
                if (vertices is IEnumerable<object> vertexList && vertexList.Count() < 2)
                    continue;

                var points = (vertices as IEnumerable<dynamic>)
                    .Select(v => new PointF((float)v.X, (float)v.Y))
                    .ToArray();

                // determine color by layer index within all layers collection
                var layerIndex = Layers.ToList().IndexOf(pline.Layer);
                //if (layerIndex >= 0 && colorlist.Selection != null && colorlist.Selection.Count > layerIndex)
                //{
                //    canvas.StrokeColor = colorlist.Selection[layerIndex];
                //}
                //else
                //{
                    canvas.StrokeColor = fallback;
                //}

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