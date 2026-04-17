using Microsoft.Maui.Graphics;
using System.Collections.Generic;

namespace NumberArtistView.Models
{
    internal class ColourSelectionList
    {
        public List<Color> Selection { get; set; } = new List<Color>();
        public ColourSelectionList()
        {
            AddColourToSelection("#637a96");
            AddColourToSelection("#a6ad72");
            AddColourToSelection("#1f752d");
            AddColourToSelection("#66345f");
            AddColourToSelection("#158e08");
            AddColourToSelection("#1d3d11");
            AddColourToSelection("#9bf2c3");
            AddColourToSelection("#4898c9");
            AddColourToSelection("#0d7774");
            AddColourToSelection("#012f60");
            AddColourToSelection("#68c98c");
            AddColourToSelection("#93274f");
            AddColourToSelection("#a3a3a3");
            AddColourToSelection("#476d3a");
            AddColourToSelection("#9e9498");
            AddColourToSelection("#303138");
            AddColourToSelection("#4caec4");
            AddColourToSelection("#09060a");
            AddColourToSelection("#a57d57");
            AddColourToSelection("#9b6e91");
            AddColourToSelection("#d7d8e0");
            AddColourToSelection("#f226f2");
            AddColourToSelection("#898063");
            AddColourToSelection("#29e810");
        }

        private void AddColourToSelection(string hexColour)
        {
            if (Color.TryParse(hexColour, out var color))
            {
                Selection.Add(color);
            }
        }
    }
}
