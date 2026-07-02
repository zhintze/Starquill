using System.Globalization;
using Starquill.Core;
using Starquill.Data;
using Starquill.Destinations;
using UnityEngine;

namespace Starquill.UI
{
    /// Pure formatting for key/dungeon UI (mirrors QuestPresenter): the
    /// unit-testable layer between the Destinations backend and the pouch
    /// cards / detail / fusion sheets. No scene or UnityEngine.UI dependency.
    public static class KeyPresenter
    {
        private static readonly Color Gray = new(0.55f, 0.55f, 0.55f);

        public static string Title(KeyInstance key) => key.DisplayName;

        /// e.g. "Difficulty 3 · Epic floor · 3:10 rush"
        public static string SubLine(KeyInstance key, EconomyConfig config)
            => $"Difficulty {key.Difficulty} · {FloorText(key)} floor · {DurationText(key, config)} rush";

        /// Dungeon rush length as m:ss (matches DungeonGenerator.Generate).
        public static string DurationText(KeyInstance key, EconomyConfig config)
        {
            int seconds = Mathf.RoundToInt(config.dungeonDurationBase
                + config.dungeonDurationPerD * (key.Difficulty - 1));
            return $"{seconds / 60}:{seconds % 60:00}";
        }

        public static string FloorText(KeyInstance key)
            => DungeonGenerator.FloorForDifficulty(key.Difficulty).ToString();

        /// Enemy HP bonus over a normal wave, e.g. "+120%" at D3.
        public static string EnemyStrengthText(KeyInstance key, EconomyConfig config)
            => $"+{Mathf.RoundToInt(config.dungeonEnemyMultPerD * (key.Difficulty - 1) * 100f)}%";

        /// "Juggernaut gear rolls STR + CON", or "" without a class modifier.
        public static string ArchetypeLine(KeyInstance key)
        {
            var arch = key.Archetype;
            return arch == null ? ""
                : $"{arch.Name} gear rolls {arch.StatA} + {arch.StatB}";
        }

        /// Representative UI accent per color family. Neutral and keys
        /// without a color modifier share the same gray.
        public static Color AccentColor(KeyInstance key)
        {
            if (!key.ColorFamily.HasValue) return Gray;
            return key.ColorFamily.Value switch
            {
                ColorFamily.Red => new Color(0.85f, 0.27f, 0.27f),
                ColorFamily.Orange => new Color(0.90f, 0.55f, 0.20f),
                ColorFamily.Brown => new Color(0.55f, 0.38f, 0.20f),
                ColorFamily.Yellow => new Color(0.88f, 0.78f, 0.25f),
                ColorFamily.Green => new Color(0.30f, 0.75f, 0.35f),
                ColorFamily.Blue => new Color(0.30f, 0.52f, 0.90f),
                ColorFamily.Purple => new Color(0.65f, 0.38f, 0.85f),
                _ => Gray // Neutral
            };
        }

        /// Filled pip count; the view draws pips as Images (glyph rule).
        public static int DifficultyPips(KeyInstance key) => key.Difficulty;

        /// Sell button label, e.g. "SELL +1,250g".
        public static string SellText(KeyInstance key, int questLevel, EconomyConfig config)
        {
            double value = KeyPouch.SellValue(key, questLevel, config.keySellBase);
            return $"SELL +{value.ToString("N0", CultureInfo.InvariantCulture)}g";
        }
    }
}
