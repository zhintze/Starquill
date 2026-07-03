using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Characters;
using Starquill.Core;
using Starquill.Display;
using Starquill.Equipment;
using Starquill.Managers;

namespace Starquill.UI
{
    /// Full pre-purchase view for a tavern recruit (BottomSheet pattern):
    /// portrait, name, species, level, all six TOTAL stats (gear included),
    /// equipped verbs, and a RECRUIT button gated on gold.
    public static class TavernRecruitSheet
    {
        public static BottomSheet Show(Transform canvasRoot, int index)
        {
            BottomSheet sheet = null;
            sheet = BottomSheet.Show(canvasRoot,
                content => Fill(content, index, () => sheet),
                heightFraction: 0.7f);
            return sheet;
        }

        private static void Fill(Transform content, int index, System.Func<BottomSheet> getSheet)
        {
            var gm = GameManager.Instance;
            var tavern = gm != null ? gm.Tavern : null;
            if (tavern == null || index < 0 || index >= tavern.Recruits.Count) return;

            var recruit = tavern.Recruits[index];
            double price = tavern.Prices[index];
            bool purchased = tavern.Purchased[index];

            // --- Header: portrait + identity ---
            var header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(content, false);
            header.AddComponent<LayoutElement>().preferredHeight = 220f;

            var portrait = new GameObject("Portrait", typeof(RectTransform), typeof(RawImage));
            portrait.transform.SetParent(header.transform, false);
            var portraitRT = portrait.GetComponent<RectTransform>();
            portraitRT.anchorMin = new Vector2(0, 0.5f);
            portraitRT.anchorMax = new Vector2(0, 0.5f);
            portraitRT.pivot = new Vector2(0, 0.5f);
            portraitRT.sizeDelta = new Vector2(220f, 220f);
            RenderPortrait(recruit, portrait.GetComponent<RawImage>(), content);

            var name = UiFactory.Text(header.transform, recruit.displayName, UiFactory.TextStyle.Heading);
            var nameRT = name.rectTransform;
            nameRT.anchorMin = new Vector2(0, 0.5f);
            nameRT.anchorMax = new Vector2(1, 1);
            nameRT.offsetMin = new Vector2(220f + UiTheme.Space3, 0);
            nameRT.offsetMax = Vector2.zero;

            var subtitle = UiFactory.Text(header.transform,
                $"{TavernPresenter.SpeciesLabel(recruit.speciesId)} · Lv {recruit.level}",
                UiFactory.TextStyle.Caption);
            var subRT = subtitle.rectTransform;
            subRT.anchorMin = new Vector2(0, 0);
            subRT.anchorMax = new Vector2(1, 0.5f);
            subRT.offsetMin = new Vector2(220f + UiTheme.Space3, 0);
            subRT.offsetMax = Vector2.zero;
            subtitle.alignment = TextAlignmentOptions.TopLeft;

            // --- Total stats (gear included), two rows of three ---
            SectionLabel(content, "STATS");
            var totals = recruit.GetTotalStats();
            var statTypes = new[]
            {
                StatType.STR, StatType.DEX, StatType.CON,
                StatType.INT, StatType.WIS, StatType.CHA
            };
            for (int row = 0; row < 2; row++)
            {
                var statRow = new GameObject($"StatRow_{row}", typeof(RectTransform));
                statRow.transform.SetParent(content, false);
                statRow.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontBody + 10f;
                var layout = statRow.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = UiTheme.Space2;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = true;

                for (int col = 0; col < 3; col++)
                {
                    var stat = statTypes[row * 3 + col];
                    UiFactory.Text(statRow.transform,
                        TavernPresenter.StatLine(stat, totals.GetStat(stat)),
                        UiFactory.TextStyle.Body);
                }
            }

            // --- Equipped verbs with their stat types ---
            SectionLabel(content, "VERBS");
            if (recruit.equippedVerbs.Count == 0)
            {
                Caption(content, "None equipped");
            }
            foreach (var verb in recruit.equippedVerbs)
            {
                if (verb == null) continue;
                var line = UiFactory.Text(content,
                    TavernPresenter.VerbLine(verb.displayName, verb.statType),
                    UiFactory.TextStyle.Body);
                line.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontBody + 8f;
            }

            // --- Gear summary ---
            int gearCount = 0;
            foreach (var item in recruit.equipment)
                if (item != null) gearCount++;
            Caption(content, $"Arrives with {gearCount} equipped items");

            // Failure reason (e.g. roster full); blank until needed.
            var reason = UiFactory.Text(content, "", UiFactory.TextStyle.Caption);
            reason.color = UiTheme.DeltaDown;
            reason.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontCaption + 6f;

            // --- Recruit action ---
            var handle = UiFactory.Button(content,
                TavernPresenter.RecruitLabel(price), UiFactory.ButtonKind.Primary, () =>
            {
                var g = GameManager.Instance;
                if (g != null && g.RecruitFromTavern(index)) getSheet()?.Close();
                else reason.text = "Roster is full";
            });
            if (purchased)
                handle.SetEnabled(false, "Recruited");
            else
                handle.SetEnabled(gm.gold >= price,
                    $"Need {NumberFormatter.FormatCompact(price)}g");
        }

        /// Head-crop portrait via the shared compositing pipeline. The
        /// renderer lives under the sheet root, so closing the sheet
        /// destroys it (and releases its RenderTexture).
        private static void RenderPortrait(CharacterInstance recruit, RawImage target, Transform content)
        {
            var registry = DisplayDataRegistry.Instance;
            if (registry.Species.Count == 0) registry.LoadAll();
            if (!registry.Species.TryGetValue(recruit.speciesId, out var speciesData)) return;

            var obj = new GameObject("TavernPortraitRenderer");
            var sheetRoot = content.GetComponentInParent<BottomSheet>();
            obj.transform.SetParent(sheetRoot != null ? sheetRoot.transform : content, false);
            var renderer = obj.AddComponent<CharacterPortraitRenderer>();
            renderer.Initialize(new ImageResolver(), 220);
            renderer.SetHeadCrop(speciesData.HeadYOffset, speciesData.HeadZoom);

            var instance = recruit.GetOrCreateAppearance(speciesData, registry);
            var equipList = new List<EquipmentDisplayInfo>();
            foreach (var eq in recruit.equipment)
            {
                if (eq == null) continue;
                equipList.Add(new EquipmentDisplayInfo
                {
                    ItemType = eq.ItemType,
                    ItemNum = eq.ItemNum,
                    BaseColor = eq.BaseColor,
                    VarianceColors = eq.VarianceColors as Dictionary<int, Color>
                        ?? new Dictionary<int, Color>(eq.VarianceColors),
                    IsOffhand = eq.Slot == EquipmentSlot.OffHand,
                    LayerVariants = eq.LayerVariants
                });
            }

            renderer.RebuildFromData(instance, speciesData, equipList, new DisplayBuilder(registry));
            target.texture = renderer.Texture;
        }

        private static void SectionLabel(Transform parent, string text)
        {
            var tmp = UiFactory.Text(parent, text, UiFactory.TextStyle.CaptionDim);
            tmp.fontStyle = FontStyles.Bold;
            tmp.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontCaption + 12f;
        }

        private static void Caption(Transform parent, string text)
        {
            var tmp = UiFactory.Text(parent, text, UiFactory.TextStyle.Caption);
            tmp.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontCaption + 8f;
        }
    }
}
