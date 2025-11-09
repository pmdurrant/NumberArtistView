using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Core.Business.Objects;
using System.Drawing;
using PointF = Microsoft.Maui.Graphics.PointF;

namespace NumberArtistView
{
    public class PolylineDrawable : IDrawable
    {
        public List<Pline2DModel> Polylines { get; set; } = new List<Pline2DModel>();
        public IEnumerable<string> Layers => Polylines.Select(p => p.Layer).Distinct();
        public string SelectedLayer { get; set; }
        public float Scale { get; set; } = 1.0f;
        public float OffsetX { get; set; } = 0f;
        public float OffsetY { get; set; } = 0f;

        public void FitToView(RectF dirtyRect)
        {
            if (Polylines == null || !Polylines.Any()) return;

            var allVertices = Polylines.SelectMany(p => p.Vertices).ToList();
            if (!allVertices.Any()) return;

            var minX = (float)allVertices.Min(v => v.X);
            var maxX = (float)allVertices.Max(v => v.X);
            var minY = (float)allVertices.Min(v => v.Y);
            var maxY = (float)allVertices.Max(v => v.Y);

            var modelWidth = maxX - minX;
            var modelHeight = maxY - minY;

            const float epsilon = 1e-6f;
            if (Math.Abs(modelWidth) < epsilon || Math.Abs(modelHeight) < epsilon) return;

            Scale = Math.Min(dirtyRect.Width / modelWidth, dirtyRect.Height / modelHeight) * 0.9f;
            OffsetX = (dirtyRect.Width - modelWidth * Scale) / 2f - minX * Scale;
            OffsetY = (dirtyRect.Height - modelHeight * Scale) / 2f - minY * Scale;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var polylinesToDraw = string.IsNullOrEmpty(SelectedLayer)
                ? Polylines
                : Polylines.Where(p => p.Layer == SelectedLayer);

            if (polylinesToDraw == null || !polylinesToDraw.Any()) return;

            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 2 / Scale; // Keep stroke size consistent when zooming

            foreach (var polyline in polylinesToDraw)
            {
                var path = new PathF();
                var firstVertex = polyline.Vertices.First();

                var startPoint = new PointF(
                    (float)firstVertex.X * Scale + OffsetX,
                    (float)firstVertex.Y * Scale + OffsetY
                );
                path.MoveTo(startPoint);

                for (int i = 0; i < polyline.Vertices.Count; i++)
                {
                    var startVertex = polyline.Vertices[i];
                    var endVertex = (i == polyline.Vertices.Count - 1) ?
                                    (polyline.IsClosed ? polyline.Vertices[0] : null)
                                    : polyline.Vertices[i + 1];

                    if (endVertex == null) continue;

                    var transformedStart = new VertexModel
                    {
                        X = startVertex.X * Scale + OffsetX,
                        Y = startVertex.Y * Scale + OffsetY,
                        Bulge = startVertex.Bulge
                    };

                    var transformedEnd = new VertexModel
                    {
                        X = endVertex.X * Scale + OffsetX,
                        Y = endVertex.Y * Scale + OffsetY,
                        Bulge = endVertex.Bulge
                    };

                    if (startVertex.Bulge == 0)
                    {
                        path.LineTo((float)transformedEnd.X, (float)transformedEnd.Y);
                    }
                    else
                    {
                        AddArcToPath(path, transformedStart, transformedEnd);
                    }
                }

                if (polyline.IsClosed)
                {
                    path.Close();
                }

                canvas.DrawPath(path);
            }
        }

        private void AddArcToPath(PathF path, VertexModel start, VertexModel end)
        {
            var p1 = new PointF((float)start.X, (float)start.Y);
            var p2 = new PointF((float)end.X, (float)end.Y);

            var midPoint = new PointF((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
            var perpendicular = new PointF(-(p2.Y - p1.Y), p2.X - p1.X);
            var dist = (float)Math.Sqrt(perpendicular.X * perpendicular.X + perpendicular.Y * perpendicular.Y);

            if (dist == 0) return;

            perpendicular.X /= dist;
            perpendicular.Y /= dist;

            var vectorLength = (float)Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
            var bulgeDist = (float)(vectorLength * start.Bulge / 2);
            var controlPoint = new PointF(midPoint.X + perpendicular.X * bulgeDist, midPoint.Y + perpendicular.Y * bulgeDist);

            path.QuadTo(controlPoint, p2);
        }
    }
}