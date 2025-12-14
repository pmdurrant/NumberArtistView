
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Maui.Graphics;
using NumberArtistView.Models;

namespace NumberArtistView
{
  

    // Assumes Pline2DModel and VertexModel exist in the project as used by MainPage
    public class PolylineDrawable : IDrawable, INotifyPropertyChanged
    {
        private List<Pline2DModel> _polylines = new();
        public List<Pline2DModel> Polylines
        {
            get => _polylines;
            set
            {
                _polylines = value ?? new List<Pline2DModel>();
                OnPropertyChanged(nameof(Polylines));
                OnPropertyChanged(nameof(Layers));
            }
        }

        // Selected layer name (empty or null means none)
        private string _selectedLayer = string.Empty;
        public string SelectedLayer
        {
            get => _selectedLayer;
            set
            {
                if (_selectedLayer != value)
                {
                    _selectedLayer = value ?? string.Empty;
                    OnPropertyChanged(nameof(SelectedLayer));
                }
            }
        }

        // Null means "all visible"
        public HashSet<string>? VisibleLayers { get; set; } = null;

        // Computed list of available layer names (ordered)
        public IEnumerable<string> Layers => Polylines?.Select(p => p.Layer ?? string.Empty).Distinct().OrderBy(s => s) ?? Enumerable.Empty<string>();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (Polylines == null || Polylines.Count == 0)
                return;

            // Determine which polylines to render according to VisibleLayers
            var toDraw = Polylines.Where(p =>
            {
                var layerName = p.Layer ?? string.Empty;
                return VisibleLayers == null || VisibleLayers.Count == 0 || VisibleLayers.Contains(layerName);
            }).ToList();

            if (!toDraw.Any())
                return;

            // Collect bounds across selected polylines to compute fit-to-view transform
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var pl in toDraw)
            {
                if (pl.Vertices is System.Collections.IEnumerable verts)
                {
                    foreach (var vObj in verts)
                    {
                        // Expect VertexModel with X and Y
                        try
                        {
                            var vxProp = vObj.GetType().GetProperty("X");
                            var vyProp = vObj.GetType().GetProperty("Y");
                            if (vxProp == null || vyProp == null) continue;
                            var xv = Convert.ToDouble(vxProp.GetValue(vObj));
                            var yv = Convert.ToDouble(vyProp.GetValue(vObj));
                            minX = Math.Min(minX, (float)xv);
                            minY = Math.Min(minY, (float)yv);
                            maxX = Math.Max(maxX, (float)xv);
                            maxY = Math.Max(maxY, (float)yv);
                        }
                        catch
                        {
                            // ignore malformed vertex
                        }
                    }
                }
            }

            if (minX == float.MaxValue || minY == float.MaxValue)
                return;

            // Compute scale and offsets to fit polylines into dirtyRect with padding
            float padding = Math.Min(dirtyRect.Width, dirtyRect.Height) * 0.05f;
            float contentWidth = Math.Max(1e-6f, maxX - minX);
            float contentHeight = Math.Max(1e-6f, maxY - minY);

            float scaleX = (dirtyRect.Width - 2 * padding) / contentWidth;
            float scaleY = (dirtyRect.Height - 2 * padding) / contentHeight;
            float scale = Math.Min(scaleX, scaleY);

            // Centering offsets
            float totalWidth = contentWidth * scale;
            float totalHeight = contentHeight * scale;
            float offsetX = dirtyRect.X + (dirtyRect.Width - totalWidth) / 2f - minX * scale;
            // flip Y (DXF coordinate Y up -> canvas Y down), so we map maxY to top + padding
            float offsetY = dirtyRect.Y + (dirtyRect.Height - totalHeight) / 2f + maxY * scale;

            // Draw each polyline
            foreach (var pline in toDraw)
            {
                // prepare path
                var path = new PathF();

                if (pline.Vertices is System.Collections.IEnumerable verts)
                {
                    bool first = true;
                    float? startX = null, startY = null;
                    foreach (var vObj in verts)
                    {
                        var vxProp = vObj.GetType().GetProperty("X");
                        var vyProp = vObj.GetType().GetProperty("Y");
                        if (vxProp == null || vyProp == null) continue;
                        var xv = Convert.ToDouble(vxProp.GetValue(vObj));
                        var yv = Convert.ToDouble(vyProp.GetValue(vObj));

                        // map to canvas coords
                        float cx = (float)xv * scale + offsetX;
                        float cy = offsetY - (float)yv * scale; // flipped

                        if (first)
                        {
                            path.MoveTo(cx, cy);
                            startX = cx; startY = cy;
                            first = false;
                        }
                        else
                        {
                            path.LineTo(cx, cy);
                        }
                    }

                    // if closed, connect last to first
                    if (pline.IsClosed && startX.HasValue && startY.HasValue)
                    {
                        path.Close();
                    }
                }

                // Determine stroke color
                var strokeColor = ConvertToColor(pline.LayerColour) ?? Colors.Black;
                float strokeSize = 1f;

                // If this is the selected layer, highlight it
                if (!string.IsNullOrEmpty(SelectedLayer) && string.Equals(pline.Layer ?? string.Empty, SelectedLayer, StringComparison.Ordinal))
                {
                    strokeSize = 3f;
                    // optionally change color (overlay)
                    strokeColor = Colors.Red;
                }

                canvas.SaveState();
                canvas.StrokeColor = strokeColor;
                canvas.StrokeSize = strokeSize;
                canvas.DrawPath(path);
                canvas.RestoreState();
            }
        }

        private Color? ConvertToColor(object? layerColour)
        {
            if (layerColour == null) return null;

            // If it's a LayerColorObject
            if (layerColour is LayerColorObject lco)
            {
                return Color.FromRgba(
                    ClampByte(lco.R),
                    ClampByte(lco.G),
                    ClampByte(lco.B),
                    ClampByte(lco.A));
            }

            // If it's already a Microsoft.Maui.Graphics.Color
            if (layerColour is Color c) return c;

            // Try reflection (properties R,G,B,A)
            try
            {
                var type = layerColour.GetType();
                var pr = type.GetProperty("R");
                var pg = type.GetProperty("G");
                var pb = type.GetProperty("B");
                var pa = type.GetProperty("A");
                if (pr != null && pg != null && pb != null)
                {
                    int r = Convert.ToInt32(pr.GetValue(layerColour));
                    int g = Convert.ToInt32(pg.GetValue(layerColour));
                    int b = Convert.ToInt32(pb.GetValue(layerColour));
                    int a = pa != null ? Convert.ToInt32(pa.GetValue(layerColour)) : 255;
                    return Color.FromRgba(ClampByte(r), ClampByte(g), ClampByte(b), ClampByte(a));
                }
            }
            catch
            {
                // ignore and fallback below
            }

            return null;
        }

        private int ClampByte(int v) => Math.Clamp(v, 0, 255);
    }
    // Minimal LayerColorObject shape expected by your MainPage code
    public class LayerColorObject
    {
        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }
        public int A { get; set; } = 255;
    }
}