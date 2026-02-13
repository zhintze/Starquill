using UnityEngine;

namespace Starquill.Display
{
    public class DisplayPiece
    {
        public int Layer { get; set; }
        public string SpritePath { get; set; }
        public Color TintColor { get; set; } = Color.white;
        public Vector2 Offset { get; set; } = Vector2.zero;
        public Vector2 Scale { get; set; } = Vector2.one;
        public float Rotation { get; set; }
        public bool FlipH { get; set; }
        public bool IsOffhandWeapon { get; set; }

        public DisplayPiece(int layer, string spritePath)
        {
            Layer = layer;
            SpritePath = spritePath;
        }

        public DisplayPiece(int layer, string spritePath, Color tintColor)
        {
            Layer = layer;
            SpritePath = spritePath;
            TintColor = tintColor;
        }
    }
}
