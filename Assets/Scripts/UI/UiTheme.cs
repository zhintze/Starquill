using UnityEngine;

namespace Starquill.UI
{
    /// Design tokens for the 1080x1920 portrait canvas.
    /// Every UI size and color derives from here; screens never hard-code values.
    public static class UiTheme
    {
        // Type scale. FontFloor is a hard invariant: no text smaller, anywhere.
        public const float FontDisplay = 44f;
        public const float FontHeading = 38f;
        public const float FontBody = 34f;
        public const float FontCaption = 28f;
        public const float FontFloor = 28f;

        // Spacing (8px grid)
        public const float Space1 = 8f;
        public const float Space2 = 16f;
        public const float Space3 = 24f;
        public const float Space4 = 32f;
        public const float CardPadding = 24f;
        public const float ListGutter = 24f;

        // Touch targets
        public const float TouchMin = 120f;
        public const float NavHeight = 160f;
        public const float ButtonPrimaryHeight = 130f;

        // ItemCard metrics
        public const float CardHeight = 240f;
        public const float CardIcon = 200f;
        public const float DeltaChipWidth = 120f;
        public const float DeltaChipHeight = 72f;

        // Surfaces
        public static readonly Color Background = Hex("1A1A26");
        public static readonly Color Card = Hex("232330");
        public static readonly Color Sheet = Hex("2C2C3A");
        public static readonly Color Scrim = new(0f, 0f, 0f, 0.55f);

        // Accents. Delta colors are reserved for upgrade/downgrade meaning only.
        public static readonly Color AccentGreen = Hex("2E8B57");
        public static readonly Color DeltaUp = new(0.3f, 0.9f, 0.3f);
        public static readonly Color DeltaDown = new(0.9f, 0.3f, 0.3f);
        public static readonly Color DeltaNeutral = new(0.6f, 0.6f, 0.6f);
        public static readonly Color BestGold = new(1f, 0.84f, 0f);

        // Text
        public static readonly Color TextPrimary = Color.white;
        public static readonly Color TextSecondary = new(1f, 1f, 1f, 0.7f);
        public static readonly Color TextDim = new(0.55f, 0.55f, 0.6f);

        private static Color Hex(string h)
        {
            ColorUtility.TryParseHtmlString("#" + h, out var c);
            return c;
        }
    }
}
