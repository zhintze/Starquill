using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Destinations;
using Starquill.Managers;

namespace Starquill.UI
{
    /// Fills a BottomSheet with key detail (mirrors ItemDetailSheet): title,
    /// modifier chips, difficulty pips, dungeon preview lines, then
    /// OPEN DUNGEON / FUSE / SELL actions.
    public static class KeyDetailSheet
    {
        public static BottomSheet Show(Transform canvasRoot, KeyInstance key)
        {
            BottomSheet sheet = null;
            sheet = BottomSheet.Show(canvasRoot,
                content => Fill(content, canvasRoot, key, () => sheet));
            return sheet;
        }

        private static void Fill(Transform content, Transform canvasRoot, KeyInstance key,
            Func<BottomSheet> getSheet)
        {
            var gm = GameManager.Instance;
            var config = gm != null ? gm.economyConfig : null;
            if (gm == null || config == null) return;

            var accent = KeyPresenter.AccentColor(key);

            // --- Header ---
            var name = UiFactory.Text(content, KeyPresenter.Title(key), UiFactory.TextStyle.Heading);
            name.color = key.ColorFamily.HasValue ? accent : UiTheme.TextPrimary;
            name.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontHeading + 12f;

            // --- Modifier chips ---
            var chipRow = new GameObject("Modifiers", typeof(RectTransform));
            chipRow.transform.SetParent(content, false);
            chipRow.AddComponent<LayoutElement>().preferredHeight = 72f;
            var chipLayout = chipRow.AddComponent<HorizontalLayoutGroup>();
            chipLayout.spacing = UiTheme.Space2;
            chipLayout.childControlWidth = false;
            chipLayout.childControlHeight = false;
            chipLayout.childAlignment = TextAnchor.MiddleLeft;

            var chipGray = new Color(0.3f, 0.3f, 0.38f);
            if (key.ColorFamily.HasValue)
                UiFactory.Chip(chipRow.transform, key.ColorFamily.Value.ToString(), accent, 220f, 64f);
            if (key.Slot.HasValue)
                UiFactory.Chip(chipRow.transform, KeyInstance.SlotWord(key.Slot.Value), chipGray, 240f, 64f);
            if (key.Archetype != null)
                UiFactory.Chip(chipRow.transform, key.Archetype.Name, chipGray, 300f, 64f);
            if (key.ModifierCount == 0)
                UiFactory.Chip(chipRow.transform, "Plain", chipGray, 200f, 64f);

            string archetypeLine = KeyPresenter.ArchetypeLine(key);
            if (!string.IsNullOrEmpty(archetypeLine))
                Caption(content, archetypeLine);

            // --- Difficulty pips ---
            var pipsRow = new GameObject("Difficulty", typeof(RectTransform));
            pipsRow.transform.SetParent(content, false);
            pipsRow.AddComponent<LayoutElement>().preferredHeight = 36f;
            var pips = UiFactory.PipRow(pipsRow.transform,
                KeyPresenter.DifficultyPips(key), config.keyMaxDifficulty, 28f);
            var pipsRT = pips.GetComponent<RectTransform>();
            pipsRT.anchorMin = new Vector2(0, 0);
            pipsRT.anchorMax = new Vector2(0, 1);
            pipsRT.pivot = new Vector2(0, 0.5f);

            // --- Dungeon preview ---
            PreviewLine(content, "Rush length", KeyPresenter.DurationText(key, config));
            PreviewLine(content, "Rarity floor", KeyPresenter.FloorText(key));
            PreviewLine(content, "Enemy strength", KeyPresenter.EnemyStrengthText(key, config));

            // Failure reason for OPEN DUNGEON; blank until needed.
            var reason = UiFactory.Text(content, "", UiFactory.TextStyle.Caption);
            reason.color = UiTheme.DeltaDown;
            reason.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontCaption + 6f;

            // --- Actions ---
            UiFactory.Button(content, "OPEN DUNGEON", UiFactory.ButtonKind.Primary, () =>
            {
                if (gm.StartDungeon(key)) getSheet()?.Close();
                else reason.text = "Finish the current quest offer or run first";
            });

            var actionRow = new GameObject("Actions", typeof(RectTransform));
            actionRow.transform.SetParent(content, false);
            var actionLE = actionRow.AddComponent<LayoutElement>();
            actionLE.preferredHeight = UiTheme.TouchMin;
            actionLE.flexibleHeight = 0;
            var actionLayout = actionRow.AddComponent<HorizontalLayoutGroup>();
            actionLayout.spacing = UiTheme.Space2;
            actionLayout.childControlWidth = true;
            actionLayout.childControlHeight = true;
            actionLayout.childForceExpandWidth = true;
            actionLayout.childForceExpandHeight = false;

            UiFactory.Button(actionRow.transform, "FUSE", UiFactory.ButtonKind.Secondary, () =>
                KeyFusionSheet.Show(canvasRoot, key, onFused: () => getSheet()?.Close()));

            UiFactory.Button(actionRow.transform,
                KeyPresenter.SellText(key, gm.questLevel, config),
                UiFactory.ButtonKind.Destructive, () =>
            {
                gm.SellKey(key);
                getSheet()?.Close();
            });
        }

        private static void Caption(Transform parent, string text)
        {
            var tmp = UiFactory.Text(parent, text, UiFactory.TextStyle.Caption);
            tmp.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontCaption + 8f;
        }

        private static void PreviewLine(Transform parent, string label, string value)
        {
            var tmp = UiFactory.Text(parent,
                UiFactory.ColorTag(label, UiTheme.TextDim) + "   " + value,
                UiFactory.TextStyle.Body);
            tmp.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontBody + 8f;
        }
    }
}
