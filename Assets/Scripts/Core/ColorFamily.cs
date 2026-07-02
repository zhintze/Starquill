namespace Starquill.Core
{
    /// Ordinals are save-format contract (SerializedKey stores the int):
    /// append only, never reorder. White/Black were split out of Neutral
    /// after D1 shipped, hence their position at the end.
    public enum ColorFamily
    {
        Red, Orange, Brown, Yellow, Green, Blue, Purple, Neutral, White, Black
    }

    /// Pure RGB -> family bucketing over the "main" palette (design D1,
    /// revised to 10 buckets: Black = near-black incl. dark tinted shades,
    /// White = whites/silvers, Neutral = true grays). Ranges are starting
    /// values; tune during the balance pass, keeping Classify total.
    public static class ColorFamilyClassifier
    {
        public static ColorFamily Classify(float r, float g, float b)
        {
            RgbToHsv(r, g, b, out float h, out float s, out float v);

            if (v < 0.20f) return ColorFamily.Black;
            if (s < 0.15f)
                return v >= 0.75f ? ColorFamily.White : ColorFamily.Neutral;

            float deg = h * 360f;
            if (deg < 15f || deg >= 345f) return ColorFamily.Red;
            if (deg < 45f) return v < 0.55f ? ColorFamily.Brown : ColorFamily.Orange;
            if (deg < 70f) return ColorFamily.Yellow;
            if (deg < 170f) return ColorFamily.Green;
            if (deg < 260f) return ColorFamily.Blue;
            return ColorFamily.Purple;
        }

        private static void RgbToHsv(float r, float g, float b,
            out float h, out float s, out float v)
        {
            float max = System.Math.Max(r, System.Math.Max(g, b));
            float min = System.Math.Min(r, System.Math.Min(g, b));
            float delta = max - min;

            v = max;
            s = max <= 0f ? 0f : delta / max;

            if (delta <= 0f) { h = 0f; return; }
            if (max == r) h = ((g - b) / delta % 6f) / 6f;
            else if (max == g) h = ((b - r) / delta + 2f) / 6f;
            else h = ((r - g) / delta + 4f) / 6f;
            if (h < 0f) h += 1f;
        }
    }
}
