using System;
using Starquill.Characters;
using Starquill.Core;
using UnityEngine;

namespace Starquill.UI
{
    /// Pure formatting for the tavern section and recruit sheet (mirrors
    /// ShopPresenter/KeyPresenter): no scene or UnityEngine.UI dependency.
    public static class TavernPresenter
    {
        /// "Bram · Dwarf Lv 7" (species omitted when unknown).
        public static string RowTitle(CharacterInstance recruit)
        {
            string species = SpeciesLabel(recruit.speciesId);
            return string.IsNullOrEmpty(species)
                ? $"{recruit.displayName} · Lv {recruit.level}"
                : $"{recruit.displayName} · {species} Lv {recruit.level}";
        }

        /// Species ids are lowercase keys ("dwarf"); display capitalized.
        public static string SpeciesLabel(string speciesId)
        {
            if (string.IsNullOrEmpty(speciesId)) return "";
            return char.ToUpperInvariant(speciesId[0]) + speciesId.Substring(1);
        }

        public static string PriceText(double price)
        {
            return NumberFormatter.FormatCompact(price) + "g";
        }

        /// "New faces in 5:59:59" (h:mm:ss, clamped at zero).
        public static string CountdownText(double secondsLeft)
        {
            if (secondsLeft < 0) secondsLeft = 0;
            var t = TimeSpan.FromSeconds(secondsLeft);
            return $"New faces in {(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}";
        }

        /// Colored stat label + value for the recruit sheet, e.g. "STR 24".
        public static string StatLine(StatType stat, int value)
        {
            return UiFactory.ColorTag(stat.ToString(), StatTypeColors.GetColor(stat))
                + $"  {value}";
        }

        /// "Slash · STR" with the stat type colored.
        public static string VerbLine(string verbName, StatType stat)
        {
            return verbName + " · "
                + UiFactory.ColorTag(stat.ToString(), StatTypeColors.GetColor(stat));
        }

        public static string RecruitLabel(double price)
        {
            return $"RECRUIT · {NumberFormatter.FormatCompact(price)}";
        }
    }
}
