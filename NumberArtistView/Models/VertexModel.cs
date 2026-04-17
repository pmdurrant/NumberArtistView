namespace NumberArtistView.Models
{
    public class VertexModel
    {
        /// <summary>
        /// Unique identifier for the vertex.
        /// </summary>
        public System.Guid Id { get; set; } = System.Guid.NewGuid();

        /// <summary>
        /// X coordinate.
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Y coordinate.
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Bulge value for arc representation.
        /// </summary>
        public double Bulge { get; set; }

    }
}