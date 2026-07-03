using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Characters;
using Starquill.Combat;
using Starquill.Display;
using Starquill.Equipment;
using Starquill.Managers;
using Starquill.Services;

namespace Starquill.UI
{
    /// Shop screen (nav index 4): sectioned scroll — BOOSTS / CHEST / PREMIUM.
    /// Rebuilds once per second while visible so countdowns stay live.
    public class ShopScreenController : MonoBehaviour
    {
        [SerializeField] private Transform content;
        [SerializeField] private ScreenManager screenManager;

        private bool initialized;
        private bool visible;
        private float repaintTimer;

        // Tavern portrait renderers persist on this controller (content is
        // torn down every repaint); indexed parallel to the tavern rows.
        private DisplayDataRegistry tavernRegistry;
        private DisplayBuilder tavernBuilder;
        private readonly List<CharacterPortraitRenderer> tavernRenderers = new();
        private readonly List<string> tavernRenderedIds = new();

        private IEnumerator Start()
        {
            yield return null;
            Initialize();
            Refresh();
        }

        public void Initialize()
        {
            if (initialized) return;
            initialized = true;

            if (screenManager != null)
                screenManager.OnScreenChanged += HandleScreenChanged;

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnBoostsChanged += Refresh;
                gm.OnTavernChanged += () => { if (visible) Refresh(); };
                gm.OnGoldChanged += _ => { if (visible) Refresh(); };
            }
        }

        private void HandleScreenChanged(int index)
        {
            visible = index == 4;
            if (visible) Refresh();
        }

        private void Update()
        {
            if (!visible) return;
            repaintTimer += Time.deltaTime;
            if (repaintTimer >= 1f)
            {
                repaintTimer = 0f;
                Refresh();
            }
        }

        public void Refresh()
        {
            if (content == null) return;
            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);

            var gm = GameManager.Instance;
            if (gm == null) return;

            double now = GameManager.UnixNow;

            SectionHeader("BOOSTS");
            BoostCard(gm, BoostType.AutoFireVerbs, "Auto-Fire Verbs",
                "Drawn Verbs fire on their own after 2 seconds.", now);
            BoostCard(gm, BoostType.VerbSpeedUp, "Verb Speed-Up",
                "Cooldowns halved; new Verbs drawn twice as fast.", now);

            SectionHeader("TAVERN");
            TavernCard(gm, now);

            SectionHeader("CHEST");
            ChestCard(gm, now);

            SectionHeader("PREMIUM");
            RemoveAdsCard(gm);
        }

        private void SectionHeader(string title)
        {
            var header = new GameObject($"Section_{title}", typeof(RectTransform));
            header.transform.SetParent(content, false);
            header.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontHeading + 28f;
            var tmp = UiFactory.Text(header.transform, title, UiFactory.TextStyle.Heading);
            tmp.color = UiTheme.TextSecondary;
            UiFactory.StretchFill(tmp.rectTransform);
            tmp.alignment = TextAlignmentOptions.BottomLeft;

            var rule = new GameObject("Rule", typeof(RectTransform), typeof(Image));
            rule.transform.SetParent(header.transform, false);
            var ruleRT = rule.GetComponent<RectTransform>();
            ruleRT.anchorMin = new Vector2(0, 0);
            ruleRT.anchorMax = new Vector2(1, 0);
            ruleRT.pivot = new Vector2(0.5f, 0);
            ruleRT.sizeDelta = new Vector2(0, 3);
            rule.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
        }

        private GameObject Card(out VerticalLayoutGroup layout)
        {
            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(content, false);
            card.GetComponent<Image>().color = UiTheme.Card;
            layout = card.AddComponent<VerticalLayoutGroup>();
            layout.spacing = UiTheme.Space2;
            layout.padding = new RectOffset((int)UiTheme.CardPadding, (int)UiTheme.CardPadding,
                (int)UiTheme.Space3, (int)UiTheme.Space3);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = card.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return card;
        }

        private void BoostCard(GameManager gm, BoostType type, string title, string effect, double now)
        {
            var card = Card(out _);

            TitleLine(card.transform, title, UiTheme.TextPrimary);
            CaptionLine(card.transform, effect);

            bool active = gm.Boosts.IsActive(type, now);
            double cost = gm.BoostCost(type);
            double remaining = gm.Boosts.Remaining(type, now);

            var handle = UiFactory.Button(card.transform,
                ShopPresenter.BoostButtonLabel(cost, active, remaining),
                active ? UiFactory.ButtonKind.Secondary : UiFactory.ButtonKind.Primary,
                () => gm.BuyBoost(type));
            handle.SetEnabled(ShopPresenter.BoostBuyEnabled(cost, gm.gold, active),
                active ? null : $"Need {NumberFormatter.FormatCompact(cost)}g");
        }

        /// Six recruit rows on a 6-hour rotation. Purchased rows stay
        /// visible (dimmed, "Recruited") until the slot rolls over.
        private void TavernCard(GameManager gm, double now)
        {
            var card = Card(out _);

            var tavern = gm.Tavern;
            CaptionLine(card.transform,
                TavernPresenter.CountdownText(gm.TavernSlotEndsAt - now));

            if (tavern == null || tavern.Recruits.Count == 0)
            {
                CaptionLine(card.transform, "The tavern is empty right now.");
                return;
            }

            for (int i = 0; i < tavern.Recruits.Count; i++)
            {
                int index = i;
                bool purchased = tavern.Purchased[i];

                var row = new GameObject($"Recruit_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                row.transform.SetParent(card.transform, false);
                var le = row.AddComponent<LayoutElement>();
                le.preferredHeight = UiTheme.TouchMin;
                le.flexibleHeight = 0;
                row.GetComponent<Image>().color = purchased
                    ? new Color(0.16f, 0.16f, 0.2f)
                    : new Color(0.3f, 0.3f, 0.38f);

                var btn = row.GetComponent<Button>();
                btn.interactable = !purchased;
                if (!purchased)
                    btn.onClick.AddListener(() => TavernRecruitSheet.Show(transform.root, index));

                // Head portrait (roster style), rendered bare-headed so the
                // face is visible; dimmed once recruited.
                var portraitObj = new GameObject("Portrait", typeof(RectTransform), typeof(RawImage));
                portraitObj.transform.SetParent(row.transform, false);
                var portraitRT = portraitObj.GetComponent<RectTransform>();
                portraitRT.anchorMin = new Vector2(0, 0.5f);
                portraitRT.anchorMax = new Vector2(0, 0.5f);
                portraitRT.pivot = new Vector2(0, 0.5f);
                portraitRT.anchoredPosition = new Vector2(8f, 0);
                portraitRT.sizeDelta = new Vector2(104f, 104f);
                var portraitImg = portraitObj.GetComponent<RawImage>();
                portraitImg.texture = TavernPortrait(index, tavern.Recruits[i]);
                portraitImg.color = purchased ? new Color(0.5f, 0.5f, 0.55f) : Color.white;

                var title = UiFactory.Text(row.transform,
                    TavernPresenter.RowTitle(tavern.Recruits[i]), UiFactory.TextStyle.Body);
                title.color = purchased ? UiTheme.TextDim : UiTheme.TextPrimary;
                var titleRT = title.rectTransform;
                titleRT.anchorMin = new Vector2(0, 0);
                titleRT.anchorMax = new Vector2(0.68f, 1);
                titleRT.offsetMin = new Vector2(112f + UiTheme.Space2, 0);
                titleRT.offsetMax = Vector2.zero;
                title.textWrappingMode = TextWrappingModes.NoWrap;
                title.overflowMode = TextOverflowModes.Ellipsis;

                var price = UiFactory.Text(row.transform,
                    purchased ? "Recruited" : TavernPresenter.PriceText(tavern.Prices[i]),
                    UiFactory.TextStyle.Body, TextAlignmentOptions.MidlineRight);
                price.color = purchased ? UiTheme.TextDim : UiTheme.BestGold;
                var priceRT = price.rectTransform;
                priceRT.anchorMin = new Vector2(0.68f, 0);
                priceRT.anchorMax = new Vector2(1, 1);
                priceRT.offsetMin = Vector2.zero;
                priceRT.offsetMax = new Vector2(-UiTheme.Space2, 0);
            }
        }

        /// Bare-headed head-crop portrait for a tavern row. Renderers (and
        /// their RenderTextures) are pooled per index and only re-render
        /// when the recruit occupying the index changes (rotation), not on
        /// the once-a-second repaint.
        private Texture TavernPortrait(int index, CharacterInstance recruit)
        {
            if (tavernRegistry == null)
            {
                tavernRegistry = DisplayDataRegistry.Instance;
                if (tavernRegistry.Species.Count == 0) tavernRegistry.LoadAll();
                tavernBuilder = new DisplayBuilder(tavernRegistry);
            }
            if (!tavernRegistry.Species.TryGetValue(recruit.speciesId, out var speciesData))
                return null;

            while (tavernRenderers.Count <= index)
            {
                var obj = new GameObject($"TavernPortrait_{tavernRenderers.Count}");
                obj.transform.SetParent(transform);
                var r = obj.AddComponent<CharacterPortraitRenderer>();
                r.Initialize(new ImageResolver(), 150);
                tavernRenderers.Add(r);
                tavernRenderedIds.Add(null);
            }

            string key = string.IsNullOrEmpty(recruit.id) ? recruit.displayName : recruit.id;
            if (tavernRenderedIds[index] != key)
            {
                var renderer = tavernRenderers[index];
                renderer.SetHeadCrop(speciesData.HeadYOffset, speciesData.HeadZoom);
                var instance = recruit.GetOrCreateAppearance(speciesData, tavernRegistry);
                renderer.RebuildFromData(instance, speciesData,
                    EquipmentDisplayMapper.ToDisplayList(recruit.equipment, bareHead: true),
                    tavernBuilder);
                tavernRenderedIds[index] = key;
            }
            return tavernRenderers[index].Texture;
        }

        private void ChestCard(GameManager gm, double now)
        {
            var card = Card(out _);

            TitleLine(card.transform, "Exploration Chest", UiTheme.BestGold);
            CaptionLine(card.transform, "A free haul of gold and gear every 4 hours.");
            CaptionLine(card.transform, ShopPresenter.ChestLabel(gm.ChestReadyAt, now));

            if (gm.ChestReady)
            {
                var row = new GameObject("Actions", typeof(RectTransform));
                row.transform.SetParent(card.transform, false);
                var le = row.AddComponent<LayoutElement>();
                le.preferredHeight = UiTheme.ButtonPrimaryHeight;
                le.flexibleHeight = 0;
                var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
                rowLayout.spacing = UiTheme.Space2;
                rowLayout.childControlWidth = true;
                rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = true;
                rowLayout.childForceExpandHeight = false;

                UiFactory.Button(row.transform, "CLAIM", UiFactory.ButtonKind.Primary,
                    () => gm.ClaimChest(false));
                var adHandle = UiFactory.Button(row.transform, "2x (watch ad)", UiFactory.ButtonKind.Secondary,
                    () => gm.Ads.ShowRewarded(AdPlacement.ChestDouble, ok => gm.ClaimChest(ok)));
                adHandle.SetEnabled(gm.Ads.IsReady(AdPlacement.ChestDouble), "Ad not ready");
            }
        }

        private void RemoveAdsCard(GameManager gm)
        {
            var card = Card(out _);
            TitleLine(card.transform, "Remove Ads", UiTheme.TextPrimary);
            CaptionLine(card.transform, "No more interstitials, forever. Rewarded bonuses stay.");

            if (gm.RemoveAdsOwned)
            {
                var owned = UiFactory.Text(card.transform, "OWNED — thank you!", UiFactory.TextStyle.Body);
                owned.color = UiTheme.AccentGreen;
                owned.fontStyle = FontStyles.Bold;
                owned.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontBody + 10f;
            }
            else
            {
                UiFactory.Button(card.transform, "Purchase", UiFactory.ButtonKind.Primary,
                    () => gm.PurchaseRemoveAds(_ => Refresh()));
            }
        }

        private static void TitleLine(Transform parent, string text, Color color)
        {
            var tmp = UiFactory.Text(parent, text, UiFactory.TextStyle.Body);
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
            tmp.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontBody + 8f;
        }

        private static void CaptionLine(Transform parent, string text)
        {
            var tmp = UiFactory.Text(parent, text, UiFactory.TextStyle.CaptionDim);
            tmp.gameObject.AddComponent<LayoutElement>().preferredHeight = UiTheme.FontCaption + 8f;
        }

        private void OnDestroy()
        {
            if (screenManager != null)
                screenManager.OnScreenChanged -= HandleScreenChanged;

            foreach (var r in tavernRenderers)
                if (r != null) Destroy(r.gameObject);
            tavernRenderers.Clear();
            tavernRenderedIds.Clear();
        }
    }
}
