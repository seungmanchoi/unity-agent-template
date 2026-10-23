using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// Design tokens (design-system skill). Sizes are reference pixels on the 1080×2340 width-matched canvas.
    /// Screens use these names, never raw colors or sizes, so a palette change is one file.
    /// </summary>
    public static class Theme
    {
        public static readonly Color Background = new Color(0.07f, 0.09f, 0.10f);
        public static readonly Color Ink = new Color(0.94f, 0.92f, 0.86f);
        public static readonly Color Accent = new Color(0.35f, 0.89f, 0.82f);

        public const int Display = 96, Title = 64, Body = 44, Caption = 36;
        public const float Gutter = 64f;
        /// <summary>Minimum touch target: 132 reference px ≈ 44 pt on a 1080-wide canvas.</summary>
        public const float MinTouch = 132f;
    }
}
