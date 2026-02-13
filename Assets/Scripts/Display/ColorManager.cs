using System.Collections.Generic;
using UnityEngine;

namespace Starquill.Display
{
    public class ColorManager
    {
        private Dictionary<string, Color[]> palettes = new();
        private static readonly Color[] FallbackPalette = { Color.white };

        public void LoadFromJson(string json)
        {
            palettes.Clear();
            var dict = ParsePaletteJson(json);
            foreach (var kvp in dict)
            {
                var colors = new Color[kvp.Value.Length];
                for (int i = 0; i < kvp.Value.Length; i++)
                    colors[i] = ParseHex(kvp.Value[i]);
                palettes[kvp.Key] = colors;
            }
        }

        public void LoadFromResources(string resourcePath = "Data/color_palettes")
        {
            var textAsset = Resources.Load<TextAsset>(resourcePath);
            if (textAsset != null)
                LoadFromJson(textAsset.text);
            else
                Debug.LogWarning($"ColorManager: Could not load {resourcePath}");
        }

        public Color[] GetPalette(string name)
        {
            if (palettes.TryGetValue(name, out var palette))
                return palette;
            if (palettes.TryGetValue("main", out var main))
                return main;
            return FallbackPalette;
        }

        public Color GetRandomColor(string paletteName)
        {
            var palette = GetPalette(paletteName);
            return palette[Random.Range(0, palette.Length)];
        }

        public Color GetRandomColor(string paletteName, System.Random rng)
        {
            var palette = GetPalette(paletteName);
            return palette[rng.Next(palette.Length)];
        }

        public Color[] ResolveColorField(string[] field)
        {
            if (field == null || field.Length == 0)
                return FallbackPalette;

            if (field[0].Length >= 6 && IsHexString(field[0]))
            {
                var colors = new Color[field.Length];
                for (int i = 0; i < field.Length; i++)
                    colors[i] = ParseHex(field[i]);
                return colors;
            }

            return GetPalette(field[0]);
        }

        public static Color ParseHex(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return Color.white;

            hex = hex.TrimStart('#');
            if (hex.Length < 6) return Color.white;

            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }

        private static bool IsHexString(string s)
        {
            foreach (char c in s)
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                    return false;
            return true;
        }

        private static Dictionary<string, string[]> ParsePaletteJson(string json)
        {
            var result = new Dictionary<string, string[]>();
            json = json.Trim();
            if (!json.StartsWith("{")) return result;

            json = json.Substring(1, json.Length - 2);
            int i = 0;
            while (i < json.Length)
            {
                int keyStart = json.IndexOf('"', i);
                if (keyStart < 0) break;
                int keyEnd = json.IndexOf('"', keyStart + 1);
                string key = json.Substring(keyStart + 1, keyEnd - keyStart - 1);

                int arrStart = json.IndexOf('[', keyEnd);
                int arrEnd = json.IndexOf(']', arrStart);
                string arrContent = json.Substring(arrStart + 1, arrEnd - arrStart - 1);

                var values = new List<string>();
                int j = 0;
                while (j < arrContent.Length)
                {
                    int valStart = arrContent.IndexOf('"', j);
                    if (valStart < 0) break;
                    int valEnd = arrContent.IndexOf('"', valStart + 1);
                    values.Add(arrContent.Substring(valStart + 1, valEnd - valStart - 1));
                    j = valEnd + 1;
                }

                result[key] = values.ToArray();
                i = arrEnd + 1;
            }
            return result;
        }
    }
}
