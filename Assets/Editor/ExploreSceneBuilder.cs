using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

public static class ExploreSceneBuilder
{
    [MenuItem("Tools/Build Explore Scene")]
    public static void Execute()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in scene");
            return;
        }

        var canvasRT = canvas.GetComponent<RectTransform>();

        // Clean existing components added by this builder
        foreach (var c in canvas.GetComponents<Starquill.UI.ExploreSceneController>())
            Object.DestroyImmediate(c);
        foreach (var c in canvas.GetComponents<Starquill.UI.ScreenManager>())
            Object.DestroyImmediate(c);

        // Clean existing children except EventSystem-related
        for (int i = canvasRT.childCount - 1; i >= 0; i--)
        {
            var child = canvasRT.GetChild(i);
            Undo.DestroyObjectImmediate(child.gameObject);
        }

        // ============================================================
        // === FULL-SCREEN BACKGROUND (fills behind notch/home indicator) ===
        // Prevents black gaps when SafeAreaPanel shrinks for safe area
        // ============================================================
        var bgFill = CreatePanel("BackgroundFill", canvasRT);
        var bgFillRT = bgFill.GetComponent<RectTransform>();
        bgFillRT.anchorMin = Vector2.zero;
        bgFillRT.anchorMax = Vector2.one;
        bgFillRT.offsetMin = Vector2.zero;
        bgFillRT.offsetMax = Vector2.zero;
        bgFill.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 1f); // matches TopBar/BottomNav

        // ============================================================
        // === SAFE AREA PANEL (constrains interactive content) ===
        // ============================================================
        var safeAreaGO = new GameObject("SafeAreaPanel", typeof(RectTransform));
        safeAreaGO.transform.SetParent(canvasRT, false);
        var safeAreaRT = safeAreaGO.GetComponent<RectTransform>();
        safeAreaRT.anchorMin = Vector2.zero;
        safeAreaRT.anchorMax = Vector2.one;
        safeAreaRT.offsetMin = Vector2.zero;
        safeAreaRT.offsetMax = Vector2.zero;
        safeAreaGO.AddComponent<Starquill.UI.SafeAreaAdapter>();

        // ============================================================
        // === EXPLORE PANEL (screen index 0) ===
        // Fills space between TopBar (top 100px) and BottomNav (bottom 120px)
        // ============================================================
        var explorePanel = CreatePanel("ExplorePanel", safeAreaRT);
        var explorePanelRT = explorePanel.GetComponent<RectTransform>();
        explorePanelRT.anchorMin = new Vector2(0, 0);
        explorePanelRT.anchorMax = new Vector2(1, 1);
        explorePanelRT.offsetMin = new Vector2(0, 120); // above BottomNav
        explorePanelRT.offsetMax = new Vector2(0, -100); // below TopBar
        explorePanel.GetComponent<Image>().color = Color.clear;

        // --- CombatAreaPanel inside ExplorePanel ---
        var combatArea = CreatePanel("CombatAreaPanel", explorePanel.transform);
        var combatRT = combatArea.GetComponent<RectTransform>();
        combatRT.anchorMin = new Vector2(0, 0);
        combatRT.anchorMax = new Vector2(1, 1);
        combatRT.offsetMin = new Vector2(0, 130); // VerbBar height
        combatRT.offsetMax = Vector2.zero; // TopBar handled by ExplorePanel

        // BG_Sky - full fill
        var bgSky = CreateRawImage("BG_Sky", combatRT);
        StretchFill(bgSky);
        AddParallaxLayer(bgSky, 5f);

        // BG_Mid - bottom 840px
        var bgMid = CreateRawImage("BG_Mid", combatRT);
        AnchorBottom(bgMid, 840);
        AddParallaxLayer(bgMid, 15f);

        // BG_Ground - bottom 420px
        var bgGround = CreateRawImage("BG_Ground", combatRT);
        AnchorBottom(bgGround, 420);
        AddParallaxLayer(bgGround, 30f);

        // Party Container
        var partyContainer = CreatePanel("PartyContainer", combatRT);
        var partyRT = partyContainer.GetComponent<RectTransform>();
        partyRT.anchorMin = new Vector2(0, 0);
        partyRT.anchorMax = new Vector2(0.55f, 1);
        partyRT.offsetMin = Vector2.zero;
        partyRT.offsetMax = Vector2.zero;
        Object.DestroyImmediate(partyContainer.GetComponent<Image>());

        // Party slots - staggered layout (back row higher/smaller, front row lower/larger)
        var backLeft = CreateRawImage("PartySlot_BackLeft", partyRT);
        SetRawImageRect(backLeft, new Vector2(150, 500), new Vector2(360, 360));

        var backRight = CreateRawImage("PartySlot_BackRight", partyRT);
        SetRawImageRect(backRight, new Vector2(400, 450), new Vector2(360, 360));

        var frontLeft = CreateRawImage("PartySlot_FrontLeft", partyRT);
        SetRawImageRect(frontLeft, new Vector2(100, 150), new Vector2(440, 440));

        var frontRight = CreateRawImage("PartySlot_FrontRight", partyRT);
        SetRawImageRect(frontRight, new Vector2(380, 100), new Vector2(440, 440));

        // Enemy Container
        var enemyContainer = CreatePanel("EnemyContainer", combatRT);
        var enemyRT = enemyContainer.GetComponent<RectTransform>();
        enemyRT.anchorMin = new Vector2(0.65f, 0);
        enemyRT.anchorMax = new Vector2(1, 1);
        enemyRT.offsetMin = Vector2.zero;
        enemyRT.offsetMax = Vector2.zero;
        Object.DestroyImmediate(enemyContainer.GetComponent<Image>());

        // Enemy silhouettes
        var silhouetteColor = new Color(0.15f, 0.15f, 0.2f, 0.8f);
        var enemy0 = CreateImage("EnemySilhouette_0", enemyRT, silhouetteColor);
        SetImageRect(enemy0, new Vector2(-60, 400), new Vector2(240, 360));

        var enemy1 = CreateImage("EnemySilhouette_1", enemyRT, silhouetteColor);
        SetImageRect(enemy1, new Vector2(60, 300), new Vector2(240, 360));

        var enemy2 = CreateImage("EnemySilhouette_2", enemyRT, silhouetteColor);
        SetImageRect(enemy2, new Vector2(-20, 150), new Vector2(240, 360));

        // EnemyDisplayController on enemyContainer
        var enemyDisplayCtrl = enemyContainer.AddComponent<Starquill.UI.EnemyDisplayController>();
        SetPrivateField(enemyDisplayCtrl, "silhouettes", new Image[] {
            enemy0.GetComponent<Image>(),
            enemy1.GetComponent<Image>(),
            enemy2.GetComponent<Image>()
        });

        // FG_Grass - bottom 140px, on top of characters
        var fgGrass = CreateRawImage("FG_Grass", combatRT);
        AnchorBottom(fgGrass, 140);
        AddParallaxLayer(fgGrass, 50f);

        // --- VerbBarPanel inside ExplorePanel ---
        var verbBar = CreatePanel("VerbBarPanel", explorePanel.transform);
        var verbBarRT = verbBar.GetComponent<RectTransform>();
        verbBarRT.anchorMin = new Vector2(0, 0);
        verbBarRT.anchorMax = new Vector2(1, 0);
        verbBarRT.pivot = new Vector2(0.5f, 0);
        verbBarRT.anchoredPosition = Vector2.zero; // ExplorePanel bottom is already above BottomNav
        verbBarRT.sizeDelta = new Vector2(0, 130);
        verbBar.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.18f, 0.85f);

        var verbGrid = new GameObject("VerbGrid", typeof(RectTransform));
        verbGrid.transform.SetParent(verbBar.transform, false);
        var verbGridRT = verbGrid.GetComponent<RectTransform>();
        StretchFill(verbGridRT);
        verbGridRT.offsetMin = new Vector2(10, 10);
        verbGridRT.offsetMax = new Vector2(-10, -10);
        var gridLayout = verbGrid.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(330, 100);
        gridLayout.spacing = new Vector2(15, 0);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 3;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;

        // ============================================================
        // === QUESTS PLACEHOLDER (screen index 1) ===
        // ============================================================
        var questsPanel = CreatePlaceholderPanel("QuestsPlaceholder", safeAreaRT, "Quests\n(Coming Soon)");

        // ============================================================
        // === LOOT PANEL (screen index 2) ===
        // ============================================================
        var lootPanel = CreatePanel("LootPanel", safeAreaRT);
        var lootPanelRT = lootPanel.GetComponent<RectTransform>();
        lootPanelRT.anchorMin = new Vector2(0, 0);
        lootPanelRT.anchorMax = new Vector2(1, 1);
        lootPanelRT.offsetMin = new Vector2(0, 120);
        lootPanelRT.offsetMax = new Vector2(0, -100);
        lootPanel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 1f);

        // Loot Header Bar (60px top)
        var lootHeader = CreatePanel("LootHeader", lootPanel.transform);
        var lootHeaderRT = lootHeader.GetComponent<RectTransform>();
        lootHeaderRT.anchorMin = new Vector2(0, 1);
        lootHeaderRT.anchorMax = new Vector2(1, 1);
        lootHeaderRT.pivot = new Vector2(0.5f, 1);
        lootHeaderRT.anchoredPosition = Vector2.zero;
        lootHeaderRT.sizeDelta = new Vector2(0, 60);
        lootHeader.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        var lootHeaderHL = lootHeader.AddComponent<HorizontalLayoutGroup>();
        lootHeaderHL.spacing = 10;
        lootHeaderHL.padding = new RectOffset(20, 20, 8, 8);
        lootHeaderHL.childAlignment = TextAnchor.MiddleCenter;
        lootHeaderHL.childControlWidth = true;
        lootHeaderHL.childControlHeight = true;
        lootHeaderHL.childForceExpandWidth = false;
        lootHeaderHL.childForceExpandHeight = true;

        var inventoryCountLabel = CreateTMPLabel("InventoryCount", lootHeader.transform,
            "Inventory: 0/50", 22, Color.white, TextAlignmentOptions.MidlineLeft);
        inventoryCountLabel.AddComponent<LayoutElement>().flexibleWidth = 2;

        var optimizeAllBtnObj = new GameObject("OptimizeAllButton", typeof(RectTransform), typeof(Image), typeof(Button));
        optimizeAllBtnObj.transform.SetParent(lootHeader.transform, false);
        optimizeAllBtnObj.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.3f);
        var optAllLE = optimizeAllBtnObj.AddComponent<LayoutElement>();
        optAllLE.preferredWidth = 160;
        var optAllLabel = CreateTMPLabel("Label", optimizeAllBtnObj.transform, "Optimize All", 16, Color.white, TextAlignmentOptions.Center);
        StretchFill(optAllLabel);

        // Loot Item Grid (scrollable, below header)
        var lootGridScroll = new GameObject("LootGridScroll", typeof(RectTransform), typeof(ScrollRect));
        lootGridScroll.transform.SetParent(lootPanel.transform, false);
        var lootGridScrollRT = lootGridScroll.GetComponent<RectTransform>();
        lootGridScrollRT.anchorMin = new Vector2(0, 0);
        lootGridScrollRT.anchorMax = new Vector2(1, 1);
        lootGridScrollRT.offsetMin = new Vector2(0, 0);
        lootGridScrollRT.offsetMax = new Vector2(0, -60);

        var lootGridContainer = new GameObject("ItemGridContainer", typeof(RectTransform));
        lootGridContainer.transform.SetParent(lootGridScroll.transform, false);
        var lootGridContainerRT = lootGridContainer.GetComponent<RectTransform>();
        lootGridContainerRT.anchorMin = new Vector2(0, 1);
        lootGridContainerRT.anchorMax = new Vector2(1, 1);
        lootGridContainerRT.pivot = new Vector2(0.5f, 1);
        lootGridContainerRT.anchoredPosition = Vector2.zero;
        lootGridContainerRT.sizeDelta = new Vector2(0, 1200);

        var lootGrid = lootGridContainer.AddComponent<GridLayoutGroup>();
        lootGrid.cellSize = new Vector2(100, 100);
        lootGrid.spacing = new Vector2(8, 8);
        lootGrid.padding = new RectOffset(10, 10, 10, 10);
        lootGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        lootGrid.constraintCount = 9;
        lootGrid.childAlignment = TextAnchor.UpperLeft;

        var lootScrollRect = lootGridScroll.GetComponent<ScrollRect>();
        lootScrollRect.content = lootGridContainerRT;
        lootScrollRect.horizontal = false;
        lootScrollRect.vertical = true;

        // Item Detail Panel (overlay, starts hidden)
        var itemDetailOverlay = CreatePanel("ItemDetailPanel", lootPanel.transform);
        var itemDetailOverlayRT = itemDetailOverlay.GetComponent<RectTransform>();
        itemDetailOverlayRT.anchorMin = new Vector2(0, 0);
        itemDetailOverlayRT.anchorMax = new Vector2(1, 0.75f);
        itemDetailOverlayRT.offsetMin = new Vector2(10, 10);
        itemDetailOverlayRT.offsetMax = new Vector2(-10, -10);
        itemDetailOverlay.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.16f, 0.98f);

        var detailVL = itemDetailOverlay.AddComponent<VerticalLayoutGroup>();
        detailVL.spacing = 8;
        detailVL.padding = new RectOffset(16, 16, 16, 16);
        detailVL.childControlWidth = true;
        detailVL.childControlHeight = false;
        detailVL.childForceExpandWidth = true;
        detailVL.childForceExpandHeight = false;

        // Detail: item name
        var detailNameLabel = CreateTMPLabel("ItemName", itemDetailOverlay.transform,
            "Item Name", 28, Color.white, TextAlignmentOptions.MidlineLeft);
        detailNameLabel.AddComponent<LayoutElement>().preferredHeight = 40;
        detailNameLabel.GetComponent<TMP_Text>().richText = true;

        // Detail: item stats
        var detailStatsLabel = CreateTMPLabel("ItemStats", itemDetailOverlay.transform,
            "Stats", 16, new Color(0.8f, 0.8f, 0.85f), TextAlignmentOptions.TopLeft);
        detailStatsLabel.AddComponent<LayoutElement>().preferredHeight = 120;
        detailStatsLabel.GetComponent<TMP_Text>().richText = true;

        // Detail: sell value
        var detailSellLabel = CreateTMPLabel("SellValue", itemDetailOverlay.transform,
            "Sell: 0 Gold", 18, new Color(1f, 0.84f, 0f), TextAlignmentOptions.MidlineLeft);
        detailSellLabel.AddComponent<LayoutElement>().preferredHeight = 30;

        // Detail: "Who wants this?" label
        var whoWantsLabel = CreateTMPLabel("WhoWantsLabel", itemDetailOverlay.transform,
            "Who wants this?", 18, new Color(0.6f, 0.6f, 0.65f), TextAlignmentOptions.MidlineLeft);
        whoWantsLabel.AddComponent<LayoutElement>().preferredHeight = 25;

        // Detail: character picker row
        var charPickerScroll = new GameObject("CharPickerScroll", typeof(RectTransform), typeof(ScrollRect));
        charPickerScroll.transform.SetParent(itemDetailOverlay.transform, false);
        charPickerScroll.AddComponent<LayoutElement>().preferredHeight = 90;

        var charPickerContainer = new GameObject("CharPickerContainer", typeof(RectTransform));
        charPickerContainer.transform.SetParent(charPickerScroll.transform, false);
        var charPickerContainerRT = charPickerContainer.GetComponent<RectTransform>();
        charPickerContainerRT.anchorMin = new Vector2(0, 0);
        charPickerContainerRT.anchorMax = new Vector2(0, 1);
        charPickerContainerRT.pivot = new Vector2(0, 0.5f);
        charPickerContainerRT.anchoredPosition = Vector2.zero;
        charPickerContainerRT.sizeDelta = new Vector2(800, 0);

        var charPickerHL = charPickerContainer.AddComponent<HorizontalLayoutGroup>();
        charPickerHL.spacing = 8;
        charPickerHL.childControlWidth = false;
        charPickerHL.childControlHeight = true;
        charPickerHL.childForceExpandWidth = false;
        charPickerHL.childForceExpandHeight = true;

        var charPickerScrollRect = charPickerScroll.GetComponent<ScrollRect>();
        charPickerScrollRect.content = charPickerContainerRT;
        charPickerScrollRect.horizontal = true;
        charPickerScrollRect.vertical = false;

        // Detail: button bar
        var detailBtnBar = new GameObject("ButtonBar", typeof(RectTransform));
        detailBtnBar.transform.SetParent(itemDetailOverlay.transform, false);
        detailBtnBar.AddComponent<LayoutElement>().preferredHeight = 50;
        var detailBtnHL = detailBtnBar.AddComponent<HorizontalLayoutGroup>();
        detailBtnHL.spacing = 10;
        detailBtnHL.childControlWidth = true;
        detailBtnHL.childControlHeight = true;
        detailBtnHL.childForceExpandWidth = true;
        detailBtnHL.childForceExpandHeight = true;

        var sellBtnObj = new GameObject("SellButton", typeof(RectTransform), typeof(Image), typeof(Button));
        sellBtnObj.transform.SetParent(detailBtnBar.transform, false);
        sellBtnObj.GetComponent<Image>().color = new Color(0.6f, 0.2f, 0.2f);
        var sellLabel = CreateTMPLabel("Label", sellBtnObj.transform, "Sell", 18, Color.white, TextAlignmentOptions.Center);
        StretchFill(sellLabel);

        var closeBtnObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtnObj.transform.SetParent(detailBtnBar.transform, false);
        closeBtnObj.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.35f);
        var closeLabel = CreateTMPLabel("Label", closeBtnObj.transform, "Close", 18, Color.white, TextAlignmentOptions.Center);
        StretchFill(closeLabel);

        // ============================================================
        // === PARTY PANEL (screen index 3) ===
        // ============================================================
        var partyPanel = CreatePanel("PartyPanel", safeAreaRT);
        var partyPanelRT = partyPanel.GetComponent<RectTransform>();
        partyPanelRT.anchorMin = new Vector2(0, 0);
        partyPanelRT.anchorMax = new Vector2(1, 1);
        partyPanelRT.offsetMin = new Vector2(0, 120);
        partyPanelRT.offsetMax = new Vector2(0, -100);
        partyPanel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 1f);

        // --- Header Strip (80px anchored top) ---
        var headerStrip = CreatePanel("HeaderStrip", partyPanel.transform);
        var headerRT = headerStrip.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.anchoredPosition = Vector2.zero;
        headerRT.sizeDelta = new Vector2(0, 80);
        headerStrip.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        var headerHL = headerStrip.AddComponent<HorizontalLayoutGroup>();
        headerHL.spacing = 10;
        headerHL.padding = new RectOffset(20, 20, 8, 8);
        headerHL.childAlignment = TextAnchor.MiddleCenter;
        headerHL.childControlWidth = true;
        headerHL.childControlHeight = true;
        headerHL.childForceExpandWidth = false;
        headerHL.childForceExpandHeight = true;

        var partyNameLabel = CreateTMPLabel("NameLabel", headerStrip.transform, "Name", 28, Color.white, TextAlignmentOptions.MidlineLeft);
        partyNameLabel.AddComponent<LayoutElement>().flexibleWidth = 2;

        var partySpeciesLabel = CreateTMPLabel("SpeciesLabel", headerStrip.transform, "Species", 22, new Color(0.7f, 0.7f, 0.8f), TextAlignmentOptions.MidlineLeft);
        partySpeciesLabel.AddComponent<LayoutElement>().flexibleWidth = 1;

        var partyLevelLabel = CreateTMPLabel("LevelLabel", headerStrip.transform, "Lv 1", 24, new Color(0.9f, 0.85f, 0.5f), TextAlignmentOptions.Center);
        partyLevelLabel.AddComponent<LayoutElement>().flexibleWidth = 1;

        var partyXpLabel = CreateTMPLabel("XPLabel", headerStrip.transform, "0/100 XP", 20, new Color(0.6f, 0.8f, 1f), TextAlignmentOptions.Center);
        partyXpLabel.AddComponent<LayoutElement>().flexibleWidth = 1;

        // Level Up Button
        var levelUpBtnObj = new GameObject("LevelUpButton", typeof(RectTransform), typeof(Image), typeof(Button));
        levelUpBtnObj.transform.SetParent(headerStrip.transform, false);
        levelUpBtnObj.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.3f);
        levelUpBtnObj.AddComponent<LayoutElement>().preferredWidth = 80;
        var levelUpBtn = levelUpBtnObj.GetComponent<Button>();

        var levelUpLabel = CreateTMPLabel("Label", levelUpBtnObj.transform, "Level Up", 16, Color.white, TextAlignmentOptions.Center);
        StretchFill(levelUpLabel);

        // Level Up Glow (Image overlay on the button)
        var levelUpGlow = CreateImage("LevelUpGlow", levelUpBtnObj.transform, new Color(1f, 0.9f, 0.3f, 0.5f));
        StretchFill(levelUpGlow);
        levelUpGlow.GetComponent<Image>().enabled = false;

        // --- Focus Area (670px, below header) ---
        var focusArea = CreatePanel("FocusArea", partyPanel.transform);
        var focusRT = focusArea.GetComponent<RectTransform>();
        focusRT.anchorMin = new Vector2(0, 1);
        focusRT.anchorMax = new Vector2(1, 1);
        focusRT.pivot = new Vector2(0.5f, 1);
        focusRT.anchoredPosition = new Vector2(0, -80); // below HeaderStrip
        focusRT.sizeDelta = new Vector2(0, 670);
        focusArea.GetComponent<Image>().color = Color.clear;

        // Portrait Strip (left, 120px wide)
        var portraitStrip = CreatePanel("PortraitStrip", focusArea.transform);
        var portraitStripRT = portraitStrip.GetComponent<RectTransform>();
        portraitStripRT.anchorMin = new Vector2(0, 0);
        portraitStripRT.anchorMax = new Vector2(0, 1);
        portraitStripRT.pivot = new Vector2(0, 0.5f);
        portraitStripRT.anchoredPosition = Vector2.zero;
        portraitStripRT.sizeDelta = new Vector2(120, 0);
        portraitStrip.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.14f, 0.9f);

        var portraitVL = portraitStrip.AddComponent<VerticalLayoutGroup>();
        portraitVL.spacing = 8;
        portraitVL.padding = new RectOffset(5, 5, 10, 10);
        portraitVL.childAlignment = TextAnchor.UpperCenter;
        portraitVL.childControlWidth = true;
        portraitVL.childControlHeight = false;
        portraitVL.childForceExpandWidth = true;
        portraitVL.childForceExpandHeight = false;

        // 4 portrait slots
        var portraitImages = new RawImage[4];
        var highlightRings = new Image[4];
        var portraitNameLabels = new TMP_Text[4];
        var portraitLevelLabels = new TMP_Text[4];
        var portraitButtons = new Button[4];

        for (int i = 0; i < 4; i++)
        {
            var slot = new GameObject($"PortraitSlot_{i}", typeof(RectTransform), typeof(Button));
            slot.transform.SetParent(portraitStrip.transform, false);
            var slotRT = slot.GetComponent<RectTransform>();
            slotRT.sizeDelta = new Vector2(110, 140);
            slot.AddComponent<LayoutElement>().preferredHeight = 140;

            // Highlight ring (background)
            var ring = CreateImage($"Ring_{i}", slot.transform, new Color(0.3f, 0.3f, 0.3f));
            StretchFill(ring);
            var ringRT = ring.GetComponent<RectTransform>();
            ringRT.offsetMin = new Vector2(2, 20);
            ringRT.offsetMax = new Vector2(-2, -2);

            // Portrait RawImage
            var portrait = CreateRawImage($"Portrait_{i}", slot.transform);
            var portRT = portrait.GetComponent<RectTransform>();
            portRT.anchorMin = new Vector2(0, 0);
            portRT.anchorMax = new Vector2(1, 1);
            portRT.offsetMin = new Vector2(6, 24);
            portRT.offsetMax = new Vector2(-6, -6);
            portrait.GetComponent<RawImage>().color = new Color(0.2f, 0.2f, 0.25f);

            // Name label
            var pNameLabel = CreateTMPLabel($"Name_{i}", slot.transform, "", 11, Color.white, TextAlignmentOptions.Center);
            var pNameRT = pNameLabel.GetComponent<RectTransform>();
            pNameRT.anchorMin = new Vector2(0, 0);
            pNameRT.anchorMax = new Vector2(1, 0);
            pNameRT.pivot = new Vector2(0.5f, 0);
            pNameRT.anchoredPosition = new Vector2(0, 8);
            pNameRT.sizeDelta = new Vector2(0, 14);

            // Level label
            var pLevelLabel = CreateTMPLabel($"Level_{i}", slot.transform, "", 10, new Color(0.8f, 0.8f, 0.5f), TextAlignmentOptions.Center);
            var pLevelRT = pLevelLabel.GetComponent<RectTransform>();
            pLevelRT.anchorMin = new Vector2(0, 0);
            pLevelRT.anchorMax = new Vector2(1, 0);
            pLevelRT.pivot = new Vector2(0.5f, 0);
            pLevelRT.anchoredPosition = Vector2.zero;
            pLevelRT.sizeDelta = new Vector2(0, 12);

            portraitImages[i] = portrait.GetComponent<RawImage>();
            highlightRings[i] = ring.GetComponent<Image>();
            portraitNameLabels[i] = pNameLabel.GetComponent<TMP_Text>();
            portraitLevelLabels[i] = pLevelLabel.GetComponent<TMP_Text>();
            portraitButtons[i] = slot.GetComponent<Button>();
        }

        // Paper Doll Area (center, between portrait strip and character info)
        var paperDollArea = CreatePanel("PaperDollArea", focusArea.transform);
        var paperDollAreaRT = paperDollArea.GetComponent<RectTransform>();
        paperDollAreaRT.anchorMin = new Vector2(0, 0);
        paperDollAreaRT.anchorMax = new Vector2(1, 1);
        paperDollAreaRT.offsetMin = new Vector2(120, 0); // right of portrait strip
        paperDollAreaRT.offsetMax = new Vector2(-200, 0); // left of character info
        paperDollArea.GetComponent<Image>().color = Color.clear;

        var paperDollImage = CreateRawImage("PaperDollImage", paperDollArea.transform);
        StretchFill(paperDollImage);
        var paperDollRawImage = paperDollImage.GetComponent<RawImage>();
        paperDollRawImage.color = Color.white;

        // Character Info (right, 200px wide)
        var charInfo = CreatePanel("CharacterInfo", focusArea.transform);
        var charInfoRT = charInfo.GetComponent<RectTransform>();
        charInfoRT.anchorMin = new Vector2(1, 0);
        charInfoRT.anchorMax = new Vector2(1, 1);
        charInfoRT.pivot = new Vector2(1, 0.5f);
        charInfoRT.anchoredPosition = Vector2.zero;
        charInfoRT.sizeDelta = new Vector2(200, 0);
        charInfo.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.14f, 0.6f);

        // --- Stat Bar (60px, below focus area) ---
        var statBar = CreatePanel("StatBar", partyPanel.transform);
        var statBarRT = statBar.GetComponent<RectTransform>();
        statBarRT.anchorMin = new Vector2(0, 1);
        statBarRT.anchorMax = new Vector2(1, 1);
        statBarRT.pivot = new Vector2(0.5f, 1);
        statBarRT.anchoredPosition = new Vector2(0, -750); // 80 (header) + 670 (focus)
        statBarRT.sizeDelta = new Vector2(0, 60);
        statBar.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.8f);

        var statHL = statBar.AddComponent<HorizontalLayoutGroup>();
        statHL.spacing = 5;
        statHL.padding = new RectOffset(15, 15, 5, 5);
        statHL.childAlignment = TextAnchor.MiddleCenter;
        statHL.childControlWidth = true;
        statHL.childControlHeight = true;
        statHL.childForceExpandWidth = true;
        statHL.childForceExpandHeight = true;

        var statNames = new[] { "STR", "DEX", "CON", "INT", "WIS", "CHA" };
        var focusStatLabels = new TMP_Text[6];
        for (int i = 0; i < 6; i++)
        {
            var statLabel = CreateTMPLabel($"Stat_{statNames[i]}", statBar.transform, $"{statNames[i]} 10", 18, Color.white, TextAlignmentOptions.Center);
            statLabel.AddComponent<LayoutElement>().flexibleWidth = 1;
            focusStatLabels[i] = statLabel.GetComponent<TMP_Text>();
        }

        // --- Sub-Tab Bar (80px, below stat bar) ---
        var subTabBar = CreatePanel("SubTabBar", partyPanel.transform);
        var subTabBarRT = subTabBar.GetComponent<RectTransform>();
        subTabBarRT.anchorMin = new Vector2(0, 1);
        subTabBarRT.anchorMax = new Vector2(1, 1);
        subTabBarRT.pivot = new Vector2(0.5f, 1);
        subTabBarRT.anchoredPosition = new Vector2(0, -810); // 750 + 60 (stat bar)
        subTabBarRT.sizeDelta = new Vector2(0, 80);
        subTabBar.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.18f, 0.9f);

        var subTabHL = subTabBar.AddComponent<HorizontalLayoutGroup>();
        subTabHL.spacing = 10;
        subTabHL.padding = new RectOffset(20, 20, 8, 8);
        subTabHL.childAlignment = TextAnchor.MiddleCenter;
        subTabHL.childControlWidth = true;
        subTabHL.childControlHeight = true;
        subTabHL.childForceExpandWidth = true;
        subTabHL.childForceExpandHeight = true;

        var subTabNames = new[] { "Roster", "Equipment", "Actions" };
        var subTabButtons = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            var tabBtnObj = new GameObject($"SubTab_{subTabNames[i]}", typeof(RectTransform), typeof(Image), typeof(Button));
            tabBtnObj.transform.SetParent(subTabBar.transform, false);
            tabBtnObj.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.28f);
            var tabLabel = CreateTMPLabel("Label", tabBtnObj.transform, subTabNames[i], 22, Color.white, TextAlignmentOptions.Center);
            StretchFill(tabLabel);
            subTabButtons[i] = tabBtnObj.GetComponent<Button>();
        }

        // --- Content Area (fill remaining space below sub-tab bar) ---
        var contentArea = CreatePanel("ContentArea", partyPanel.transform);
        var contentRT = contentArea.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 0);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.offsetMin = new Vector2(0, 0); // bottom of panel
        contentRT.offsetMax = new Vector2(0, -890); // 810 + 80 (sub-tab bar)
        contentArea.GetComponent<Image>().color = Color.clear;

        // Roster Content (sub-tab 0)
        var rosterContent = CreatePanel("RosterContent", contentArea.transform);
        StretchFill(rosterContent);
        rosterContent.GetComponent<Image>().color = Color.clear;

        var rosterScrollGO = new GameObject("RosterScrollView", typeof(RectTransform), typeof(ScrollRect));
        rosterScrollGO.transform.SetParent(rosterContent.transform, false);
        StretchFill(rosterScrollGO.GetComponent<RectTransform>());

        var rosterCardContainer = new GameObject("CardContainer", typeof(RectTransform));
        rosterCardContainer.transform.SetParent(rosterScrollGO.transform, false);
        var rosterCardRT = rosterCardContainer.GetComponent<RectTransform>();
        rosterCardRT.anchorMin = new Vector2(0, 1);
        rosterCardRT.anchorMax = new Vector2(1, 1);
        rosterCardRT.pivot = new Vector2(0.5f, 1);
        rosterCardRT.anchoredPosition = Vector2.zero;
        rosterCardRT.sizeDelta = new Vector2(0, 800); // expandable
        var rosterGrid = rosterCardContainer.AddComponent<GridLayoutGroup>();
        rosterGrid.cellSize = new Vector2(160, 200);
        rosterGrid.spacing = new Vector2(10, 10);
        rosterGrid.padding = new RectOffset(10, 10, 10, 10);
        rosterGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        rosterGrid.constraintCount = 5;
        rosterGrid.childAlignment = TextAnchor.UpperLeft;

        var rosterScrollRect = rosterScrollGO.GetComponent<ScrollRect>();
        rosterScrollRect.content = rosterCardRT;
        rosterScrollRect.horizontal = false;
        rosterScrollRect.vertical = true;

        // Equipment Content (sub-tab 1)
        var equipContent = CreatePanel("EquipmentContent", contentArea.transform);
        StretchFill(equipContent);
        equipContent.GetComponent<Image>().color = Color.clear;

        // EquipmentListArea — wraps scroll + optimize button, hidden when drawer is open
        var equipListArea = new GameObject("EquipmentListArea", typeof(RectTransform));
        equipListArea.transform.SetParent(equipContent.transform, false);
        StretchFill(equipListArea);

        // Equipment card list - ScrollRect with VerticalLayoutGroup
        var equipScrollObj = new GameObject("EquipScrollRect", typeof(RectTransform), typeof(ScrollRect));
        equipScrollObj.transform.SetParent(equipListArea.transform, false);
        var equipScrollRT = equipScrollObj.GetComponent<RectTransform>();
        equipScrollRT.anchorMin = new Vector2(0, 0.08f);  // Leave room for Optimize button
        equipScrollRT.anchorMax = new Vector2(1, 1);
        equipScrollRT.offsetMin = Vector2.zero;
        equipScrollRT.offsetMax = Vector2.zero;
        var equipScrollRect = equipScrollObj.GetComponent<ScrollRect>();
        equipScrollRect.horizontal = false;
        equipScrollRect.vertical = true;
        equipScrollRect.scrollSensitivity = 30;
        equipScrollRect.movementType = ScrollRect.MovementType.Clamped;

        // Viewport (mask)
        var equipViewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        equipViewport.transform.SetParent(equipScrollObj.transform, false);
        StretchFill(equipViewport);
        equipViewport.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        equipViewport.GetComponent<Mask>().showMaskGraphic = false;
        equipScrollRect.viewport = equipViewport.GetComponent<RectTransform>();

        // Content container with VerticalLayoutGroup
        var equipCardContainer = new GameObject("CardContainer", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        equipCardContainer.transform.SetParent(equipViewport.transform, false);
        var equipCardContainerRT = equipCardContainer.GetComponent<RectTransform>();
        equipCardContainerRT.anchorMin = new Vector2(0, 1);
        equipCardContainerRT.anchorMax = new Vector2(1, 1);
        equipCardContainerRT.pivot = new Vector2(0.5f, 1);
        equipCardContainerRT.offsetMin = new Vector2(0, 0);
        equipCardContainerRT.offsetMax = new Vector2(0, 0);
        var equipVL = equipCardContainer.GetComponent<VerticalLayoutGroup>();
        equipVL.spacing = 4;
        equipVL.padding = new RectOffset(8, 8, 8, 8);
        equipVL.childAlignment = TextAnchor.UpperCenter;
        equipVL.childControlWidth = true;
        equipVL.childControlHeight = false;
        equipVL.childForceExpandWidth = true;
        equipVL.childForceExpandHeight = false;
        equipCardContainer.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        equipScrollRect.content = equipCardContainerRT;

        var autoEquipBtnObj = new GameObject("AutoEquipButton", typeof(RectTransform), typeof(Image), typeof(Button));
        autoEquipBtnObj.transform.SetParent(equipListArea.transform, false);
        var autoEquipRT = autoEquipBtnObj.GetComponent<RectTransform>();
        autoEquipRT.anchorMin = new Vector2(0.3f, 0);
        autoEquipRT.anchorMax = new Vector2(0.7f, 0);
        autoEquipRT.pivot = new Vector2(0.5f, 0);
        autoEquipRT.anchoredPosition = new Vector2(0, 10);
        autoEquipRT.sizeDelta = new Vector2(0, 40);
        autoEquipBtnObj.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.3f);
        var autoEquipLabel = CreateTMPLabel("Label", autoEquipBtnObj.transform, "Optimize", 18, Color.white, TextAlignmentOptions.Center);
        StretchFill(autoEquipLabel);

        // Equipment Drawer (full overlay with selected slot card, starts hidden)
        var equipDrawerPanel = CreatePanel("EquipDrawerPanel", equipContent.transform);
        var equipDrawerPanelRT = equipDrawerPanel.GetComponent<RectTransform>();
        equipDrawerPanelRT.anchorMin = new Vector2(0, 0);
        equipDrawerPanelRT.anchorMax = new Vector2(1, 1);
        equipDrawerPanelRT.offsetMin = new Vector2(5, 5);
        equipDrawerPanelRT.offsetMax = new Vector2(-5, 85); // extend 85px above to cover sub-tab buttons
        equipDrawerPanel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.14f, 0.98f);

        // Canvas override for z-order (renders above masked scroll content)
        var drawerCanvas = equipDrawerPanel.AddComponent<Canvas>();
        drawerCanvas.overrideSorting = true;
        drawerCanvas.sortingOrder = 10;
        equipDrawerPanel.AddComponent<GraphicRaycaster>();

        var drawerVL = equipDrawerPanel.AddComponent<VerticalLayoutGroup>();
        drawerVL.spacing = 6;
        drawerVL.padding = new RectOffset(10, 10, 10, 10);
        drawerVL.childControlWidth = true;
        drawerVL.childControlHeight = true;
        drawerVL.childForceExpandWidth = true;
        drawerVL.childForceExpandHeight = false;

        // === Selected Slot Card (280px, same size as equipment cards) ===
        var selectedSlotCard = new GameObject("SelectedSlotCard", typeof(RectTransform), typeof(Image));
        selectedSlotCard.transform.SetParent(equipDrawerPanel.transform, false);
        selectedSlotCard.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f);
        var selectedSlotLE = selectedSlotCard.AddComponent<LayoutElement>();
        selectedSlotLE.preferredHeight = 280;
        selectedSlotLE.flexibleWidth = 1;

        // Rarity stripe
        var slotStripe = new GameObject("Stripe", typeof(RectTransform), typeof(Image));
        slotStripe.transform.SetParent(selectedSlotCard.transform, false);
        var slotStripeRT = slotStripe.GetComponent<RectTransform>();
        slotStripeRT.anchorMin = new Vector2(0, 0);
        slotStripeRT.anchorMax = new Vector2(0, 1);
        slotStripeRT.pivot = new Vector2(0, 0.5f);
        slotStripeRT.sizeDelta = new Vector2(6, 0);
        slotStripeRT.anchoredPosition = Vector2.zero;
        slotStripe.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.3f);

        // Slot label (top-left, 0-45%)
        var slotLabelObj = new GameObject("SlotLabel", typeof(RectTransform));
        slotLabelObj.transform.SetParent(selectedSlotCard.transform, false);
        var slotLabelRT = slotLabelObj.GetComponent<RectTransform>();
        slotLabelRT.anchorMin = new Vector2(0, 0.7f);
        slotLabelRT.anchorMax = new Vector2(0.45f, 1f);
        slotLabelRT.offsetMin = new Vector2(16, 0);
        slotLabelRT.offsetMax = new Vector2(0, -8);
        var slotLabelTmp = slotLabelObj.AddComponent<TextMeshProUGUI>();
        slotLabelTmp.text = "Slot";
        slotLabelTmp.fontSize = 14;
        slotLabelTmp.color = new Color(0.5f, 0.5f, 0.55f);
        slotLabelTmp.alignment = TextAlignmentOptions.BottomLeft;

        // Item name (middle-left, 0-45%)
        var slotNameObj = new GameObject("ItemName", typeof(RectTransform));
        slotNameObj.transform.SetParent(selectedSlotCard.transform, false);
        var slotNameRT = slotNameObj.GetComponent<RectTransform>();
        slotNameRT.anchorMin = new Vector2(0, 0.35f);
        slotNameRT.anchorMax = new Vector2(0.45f, 0.7f);
        slotNameRT.offsetMin = new Vector2(16, 0);
        slotNameRT.offsetMax = new Vector2(0, 0);
        var slotNameTmp = slotNameObj.AddComponent<TextMeshProUGUI>();
        slotNameTmp.text = "Empty";
        slotNameTmp.fontSize = 18;
        slotNameTmp.fontStyle = FontStyles.Italic;
        slotNameTmp.color = new Color(0.4f, 0.4f, 0.45f);
        slotNameTmp.alignment = TextAlignmentOptions.MidlineLeft;

        // Item info / rarity (bottom-left, 0-45%)
        var slotInfoObj = new GameObject("ItemInfo", typeof(RectTransform));
        slotInfoObj.transform.SetParent(selectedSlotCard.transform, false);
        var slotInfoRT = slotInfoObj.GetComponent<RectTransform>();
        slotInfoRT.anchorMin = new Vector2(0, 0);
        slotInfoRT.anchorMax = new Vector2(0.45f, 0.35f);
        slotInfoRT.offsetMin = new Vector2(16, 8);
        slotInfoRT.offsetMax = new Vector2(0, 0);
        var slotInfoTmp = slotInfoObj.AddComponent<TextMeshProUGUI>();
        slotInfoTmp.text = "";
        slotInfoTmp.fontSize = 13;
        slotInfoTmp.color = new Color(0.5f, 0.5f, 0.55f);
        slotInfoTmp.alignment = TextAlignmentOptions.TopLeft;

        // Equipment sprite (middle column, 45-70%)
        var slotSpriteObj = new GameObject("EquipSprite", typeof(RectTransform), typeof(Image));
        slotSpriteObj.transform.SetParent(selectedSlotCard.transform, false);
        var slotSpriteRT = slotSpriteObj.GetComponent<RectTransform>();
        slotSpriteRT.anchorMin = new Vector2(0.45f, 0.05f);
        slotSpriteRT.anchorMax = new Vector2(0.70f, 0.95f);
        slotSpriteRT.offsetMin = Vector2.zero;
        slotSpriteRT.offsetMax = Vector2.zero;
        var slotSpriteImg = slotSpriteObj.GetComponent<Image>();
        slotSpriteImg.preserveAspect = true;
        slotSpriteImg.color = Color.clear;

        // Stats (right column, 70-100%)
        var slotStatsObj = new GameObject("Stats", typeof(RectTransform));
        slotStatsObj.transform.SetParent(selectedSlotCard.transform, false);
        var slotStatsRT = slotStatsObj.GetComponent<RectTransform>();
        slotStatsRT.anchorMin = new Vector2(0.70f, 0);
        slotStatsRT.anchorMax = new Vector2(1, 1);
        slotStatsRT.offsetMin = new Vector2(4, 8);
        slotStatsRT.offsetMax = new Vector2(-8, -8);
        var slotStatsTmp = slotStatsObj.AddComponent<TextMeshProUGUI>();
        slotStatsTmp.text = "";
        slotStatsTmp.fontSize = 16;
        slotStatsTmp.alignment = TextAlignmentOptions.Center;
        slotStatsTmp.color = new Color(0.7f, 0.7f, 0.75f);
        slotStatsTmp.richText = true;

        // Drawer summary label
        var drawerSummaryLabel = CreateTMPLabel("DrawerSummary", equipDrawerPanel.transform,
            "0 items for slot", 14, new Color(0.6f, 0.6f, 0.65f), TextAlignmentOptions.MidlineLeft);
        drawerSummaryLabel.AddComponent<LayoutElement>().preferredHeight = 20;

        // Drawer item list (scrollable with proper viewport)
        var drawerListScroll = new GameObject("DrawerListScroll", typeof(RectTransform), typeof(ScrollRect));
        drawerListScroll.transform.SetParent(equipDrawerPanel.transform, false);
        var drawerListScrollLE = drawerListScroll.AddComponent<LayoutElement>();
        drawerListScrollLE.flexibleHeight = 1;
        drawerListScrollLE.preferredHeight = 200;

        var drawerViewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        drawerViewport.transform.SetParent(drawerListScroll.transform, false);
        StretchFill(drawerViewport);
        drawerViewport.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        drawerViewport.GetComponent<Mask>().showMaskGraphic = false;

        var drawerListContainer = new GameObject("ItemListContainer", typeof(RectTransform));
        drawerListContainer.transform.SetParent(drawerViewport.transform, false);
        var drawerListContainerRT = drawerListContainer.GetComponent<RectTransform>();
        drawerListContainerRT.anchorMin = new Vector2(0, 1);
        drawerListContainerRT.anchorMax = new Vector2(1, 1);
        drawerListContainerRT.pivot = new Vector2(0.5f, 1);
        drawerListContainerRT.anchoredPosition = Vector2.zero;
        drawerListContainerRT.sizeDelta = new Vector2(0, 600);

        var drawerListVL = drawerListContainer.AddComponent<VerticalLayoutGroup>();
        drawerListVL.spacing = 4;
        drawerListVL.childControlWidth = true;
        drawerListVL.childControlHeight = false;
        drawerListVL.childForceExpandWidth = true;
        drawerListVL.childForceExpandHeight = false;

        var csf = drawerListContainer.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var drawerListScrollRect = drawerListScroll.GetComponent<ScrollRect>();
        drawerListScrollRect.content = drawerListContainerRT;
        drawerListScrollRect.viewport = drawerViewport.GetComponent<RectTransform>();
        drawerListScrollRect.horizontal = false;
        drawerListScrollRect.vertical = true;
        drawerListScrollRect.scrollSensitivity = 30;
        drawerListScrollRect.movementType = ScrollRect.MovementType.Clamped;

        // Drawer button bar
        var drawerBtnBar = new GameObject("DrawerBtnBar", typeof(RectTransform));
        drawerBtnBar.transform.SetParent(equipDrawerPanel.transform, false);
        drawerBtnBar.AddComponent<LayoutElement>().preferredHeight = 45;
        var drawerBtnHL = drawerBtnBar.AddComponent<HorizontalLayoutGroup>();
        drawerBtnHL.spacing = 10;
        drawerBtnHL.childControlWidth = true;
        drawerBtnHL.childControlHeight = true;
        drawerBtnHL.childForceExpandWidth = true;
        drawerBtnHL.childForceExpandHeight = true;

        var equipBtnObj = new GameObject("EquipButton", typeof(RectTransform), typeof(Image), typeof(Button));
        equipBtnObj.transform.SetParent(drawerBtnBar.transform, false);
        equipBtnObj.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.3f);
        var equipBtnLabel = CreateTMPLabel("Label", equipBtnObj.transform, "Equip", 18, Color.white, TextAlignmentOptions.Center);
        StretchFill(equipBtnLabel);

        var backBtnObj = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
        backBtnObj.transform.SetParent(drawerBtnBar.transform, false);
        backBtnObj.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.35f);
        var backBtnLabel = CreateTMPLabel("Label", backBtnObj.transform, "Back", 18, Color.white, TextAlignmentOptions.Center);
        StretchFill(backBtnLabel);

        // Actions Content (sub-tab 2)
        var actionsContent = CreatePanel("ActionsContent", contentArea.transform);
        StretchFill(actionsContent);
        actionsContent.GetComponent<Image>().color = Color.clear;

        // Pool Summary Bar (~50px, top)
        var poolSummaryBar = new GameObject("PoolSummaryBar", typeof(RectTransform), typeof(Image));
        poolSummaryBar.transform.SetParent(actionsContent.transform, false);
        var poolBarRT = poolSummaryBar.GetComponent<RectTransform>();
        poolBarRT.anchorMin = new Vector2(0, 1);
        poolBarRT.anchorMax = new Vector2(1, 1);
        poolBarRT.pivot = new Vector2(0.5f, 1);
        poolBarRT.sizeDelta = new Vector2(0, 50);
        poolBarRT.anchoredPosition = Vector2.zero;
        poolSummaryBar.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.16f, 0.8f);
        var poolLayout = poolSummaryBar.AddComponent<HorizontalLayoutGroup>();
        poolLayout.spacing = 4;
        poolLayout.padding = new RectOffset(8, 8, 6, 6);
        poolLayout.childControlWidth = true;
        poolLayout.childControlHeight = true;
        poolLayout.childForceExpandWidth = true;
        poolLayout.childForceExpandHeight = true;

        // Equipped Container (below pool bar, fills most of area)
        var equippedContainer = new GameObject("EquippedContainer", typeof(RectTransform));
        equippedContainer.transform.SetParent(actionsContent.transform, false);
        var equippedContainerRT = equippedContainer.GetComponent<RectTransform>();
        equippedContainerRT.anchorMin = new Vector2(0, 0.2f);
        equippedContainerRT.anchorMax = new Vector2(1, 1);
        equippedContainerRT.offsetMin = new Vector2(10, 5);
        equippedContainerRT.offsetMax = new Vector2(-10, -55);
        var equippedVL = equippedContainer.AddComponent<VerticalLayoutGroup>();
        equippedVL.spacing = 8;
        equippedVL.padding = new RectOffset(5, 5, 5, 5);
        equippedVL.childControlWidth = true;
        equippedVL.childControlHeight = false;
        equippedVL.childForceExpandWidth = true;
        equippedVL.childForceExpandHeight = false;

        // Lock Indicator (below equipped cards)
        var lockIndicator = new GameObject("LockIndicator", typeof(RectTransform));
        lockIndicator.transform.SetParent(actionsContent.transform, false);
        var lockRT = lockIndicator.GetComponent<RectTransform>();
        lockRT.anchorMin = new Vector2(0, 0.1f);
        lockRT.anchorMax = new Vector2(1, 0.2f);
        lockRT.offsetMin = new Vector2(15, 0);
        lockRT.offsetMax = new Vector2(-15, 0);

        var lockLabel = CreateTMPLabel("LockLabel", lockIndicator.transform, "2/5 slots", 14,
            new Color(0.5f, 0.5f, 0.55f), TextAlignmentOptions.MidlineLeft);
        var lockLabelRT = lockLabel.GetComponent<RectTransform>();
        lockLabelRT.anchorMin = new Vector2(0, 0);
        lockLabelRT.anchorMax = new Vector2(0.55f, 1);
        lockLabelRT.offsetMin = Vector2.zero;
        lockLabelRT.offsetMax = Vector2.zero;

        // Progress bar for lock indicator
        var sliderObj = new GameObject("LockProgress", typeof(RectTransform), typeof(Slider));
        sliderObj.transform.SetParent(lockIndicator.transform, false);
        var sliderRT = sliderObj.GetComponent<RectTransform>();
        sliderRT.anchorMin = new Vector2(0.56f, 0.25f);
        sliderRT.anchorMax = new Vector2(1, 0.75f);
        sliderRT.offsetMin = Vector2.zero;
        sliderRT.offsetMax = Vector2.zero;

        var slider = sliderObj.GetComponent<Slider>();
        slider.interactable = false;
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = 0;

        // Slider background
        var sliderBg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        sliderBg.transform.SetParent(sliderObj.transform, false);
        var sliderBgRT = sliderBg.GetComponent<RectTransform>();
        sliderBgRT.anchorMin = Vector2.zero;
        sliderBgRT.anchorMax = Vector2.one;
        sliderBgRT.offsetMin = Vector2.zero;
        sliderBgRT.offsetMax = Vector2.zero;
        sliderBg.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f);

        // Slider fill area
        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObj.transform, false);
        var fillAreaRT = fillArea.GetComponent<RectTransform>();
        fillAreaRT.anchorMin = Vector2.zero;
        fillAreaRT.anchorMax = Vector2.one;
        fillAreaRT.offsetMin = Vector2.zero;
        fillAreaRT.offsetMax = Vector2.zero;

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(0.5f, 0.7f, 0.3f);

        slider.fillRect = fillRT;
        slider.targetGraphic = sliderBg.GetComponent<Image>();

        // Drawer Bar (~80px, bottom)
        var drawerBar = new GameObject("DrawerBar", typeof(RectTransform), typeof(Image));
        drawerBar.transform.SetParent(actionsContent.transform, false);
        var drawerBarRT = drawerBar.GetComponent<RectTransform>();
        drawerBarRT.anchorMin = new Vector2(0, 0);
        drawerBarRT.anchorMax = new Vector2(1, 0.1f);
        drawerBarRT.offsetMin = Vector2.zero;
        drawerBarRT.offsetMax = Vector2.zero;
        drawerBar.GetComponent<Image>().color = new Color(0.14f, 0.14f, 0.18f, 0.9f);

        var drawerLabel = CreateTMPLabel("DrawerLabel", drawerBar.transform, "0 verbs available", 16,
            new Color(0.6f, 0.6f, 0.65f), TextAlignmentOptions.Center);
        StretchFill(drawerLabel);

        // Initially hide sub-tab content except Roster (tab 0)
        equipContent.SetActive(false);
        actionsContent.SetActive(false);

        // ============================================================
        // === SHOP PLACEHOLDER (screen index 4) ===
        // ============================================================
        var shopPanel = CreatePlaceholderPanel("ShopPlaceholder", safeAreaRT, "Shop\n(Coming Soon)");

        // ============================================================
        // === TOP BAR PANEL (anchored top, 100px, single row) ===
        // Always visible — stays directly under Canvas
        // ============================================================
        var topBar = CreatePanel("TopBarPanel", safeAreaRT);
        var topBarRT = topBar.GetComponent<RectTransform>();
        topBarRT.anchorMin = new Vector2(0, 1);
        topBarRT.anchorMax = new Vector2(1, 1);
        topBarRT.pivot = new Vector2(0.5f, 1);
        topBarRT.anchoredPosition = Vector2.zero;
        topBarRT.sizeDelta = new Vector2(0, 100);
        topBar.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

        var topBarHL = topBar.AddComponent<HorizontalLayoutGroup>();
        topBarHL.spacing = 20;
        topBarHL.padding = new RectOffset(20, 20, 10, 10);
        topBarHL.childAlignment = TextAnchor.MiddleCenter;
        topBarHL.childControlWidth = true;
        topBarHL.childControlHeight = true;
        topBarHL.childForceExpandWidth = true;
        topBarHL.childForceExpandHeight = true;

        var goldLabel = CreateTMPLabel("GoldLabel", topBar.transform, "1.2M Gold", 36, new Color(1, 0.84f, 0, 1), TextAlignmentOptions.MidlineLeft);
        goldLabel.AddComponent<LayoutElement>().flexibleWidth = 1;

        var fragLabel = CreateTMPLabel("FragmentLabel", topBar.transform, "7/12", 28, new Color(0.2f, 0.8f, 0.7f, 1), TextAlignmentOptions.Center);
        fragLabel.AddComponent<LayoutElement>().flexibleWidth = 1;

        var waveLabel = CreateTMPLabel("WaveLabel", topBar.transform, "Wave 3/5", 28, Color.white, TextAlignmentOptions.Center);
        waveLabel.AddComponent<LayoutElement>().flexibleWidth = 1;

        var questLabel = CreateTMPLabel("QuestLevelLabel", topBar.transform, "Quest Lv 34", 28, Color.white, TextAlignmentOptions.MidlineRight);
        questLabel.AddComponent<LayoutElement>().flexibleWidth = 1;

        // ============================================================
        // === BOTTOM NAV PANEL (anchored bottom, 120px) ===
        // Always visible — stays directly under Canvas
        // ============================================================
        var bottomNav = CreatePanel("BottomNavPanel", safeAreaRT);
        var bottomNavRT = bottomNav.GetComponent<RectTransform>();
        bottomNavRT.anchorMin = new Vector2(0, 0);
        bottomNavRT.anchorMax = new Vector2(1, 0);
        bottomNavRT.pivot = new Vector2(0.5f, 0);
        bottomNavRT.anchoredPosition = Vector2.zero;
        bottomNavRT.sizeDelta = new Vector2(0, 120);
        bottomNav.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 1f);

        var navHL = bottomNav.AddComponent<HorizontalLayoutGroup>();
        navHL.spacing = 5;
        navHL.padding = new RectOffset(10, 10, 10, 10);
        navHL.childControlWidth = true;
        navHL.childControlHeight = true;
        navHL.childForceExpandWidth = true;
        navHL.childForceExpandHeight = true;

        // ============================================================
        // === ADD COMPONENTS ===
        // ============================================================

        // ScreenManager on Canvas
        var screenManager = canvas.AddComponent<Starquill.UI.ScreenManager>();
        SetPrivateField(screenManager, "screenPanels", new GameObject[] {
            explorePanel, questsPanel, lootPanel, partyPanel, shopPanel
        });

        // TopBarDisplay
        var topBarDisplay = topBar.AddComponent<Starquill.UI.TopBarDisplay>();
        SetPrivateField(topBarDisplay, "goldLabel", goldLabel.GetComponent<TMP_Text>());
        SetPrivateField(topBarDisplay, "fragmentLabel", fragLabel.GetComponent<TMP_Text>());
        SetPrivateField(topBarDisplay, "waveLabel", waveLabel.GetComponent<TMP_Text>());
        SetPrivateField(topBarDisplay, "questLevelLabel", questLabel.GetComponent<TMP_Text>());

        // VerbBarDisplay
        var verbBarDisplay = verbBar.AddComponent<Starquill.UI.VerbBarDisplay>();
        SetPrivateField(verbBarDisplay, "cardContainer", verbGrid.transform);

        // VerbCardAnimator on VerbBarPanel
        verbBar.AddComponent<Starquill.UI.VerbCardAnimator>();

        // BottomNavDisplay — wire screenManager
        var bottomNavDisplay = bottomNav.AddComponent<Starquill.UI.BottomNavDisplay>();
        SetPrivateField(bottomNavDisplay, "screenManager", screenManager);

        // DamageNumberSpawner on CombatAreaPanel
        var damageSpawner = combatArea.AddComponent<Starquill.UI.DamageNumberSpawner>();

        // GoldCounterAnimator on TopBarPanel
        var goldAnimator = topBar.AddComponent<Starquill.UI.GoldCounterAnimator>();
        SetPrivateField(goldAnimator, "label", goldLabel.GetComponent<TMP_Text>());

        // PartyScreenController on PartyPanel
        var partyScreenCtrl = partyPanel.AddComponent<Starquill.UI.PartyScreenController>();
        // (PartyScreenController SerializeField wiring is done below after all components exist)

        // CharacterFocusDisplay on PartyPanel
        var focusDisplay = partyPanel.AddComponent<Starquill.UI.CharacterFocusDisplay>();
        SetPrivateField(focusDisplay, "nameLabel", partyNameLabel.GetComponent<TMP_Text>());
        SetPrivateField(focusDisplay, "speciesLabel", partySpeciesLabel.GetComponent<TMP_Text>());
        SetPrivateField(focusDisplay, "levelLabel", partyLevelLabel.GetComponent<TMP_Text>());
        SetPrivateField(focusDisplay, "xpLabel", partyXpLabel.GetComponent<TMP_Text>());
        SetPrivateField(focusDisplay, "levelUpButton", levelUpBtn);
        SetPrivateField(focusDisplay, "levelUpGlow", levelUpGlow.GetComponent<Image>());
        SetPrivateField(focusDisplay, "paperDollImage", paperDollRawImage);
        SetPrivateField(focusDisplay, "statLabels", focusStatLabels);

        // PartyPortraitStrip on PortraitStrip
        var portraitStripComp = portraitStrip.AddComponent<Starquill.UI.PartyPortraitStrip>();
        SetPrivateField(portraitStripComp, "portraits", portraitImages);
        SetPrivateField(portraitStripComp, "highlightRings", highlightRings);
        SetPrivateField(portraitStripComp, "nameLabels", portraitNameLabels);
        SetPrivateField(portraitStripComp, "levelLabels", portraitLevelLabels);
        SetPrivateField(portraitStripComp, "buttons", portraitButtons);

        // RosterGridDisplay on RosterContent
        var rosterGridDisplay = rosterContent.AddComponent<Starquill.UI.RosterGridDisplay>();
        SetPrivateField(rosterGridDisplay, "cardContainer", rosterCardContainer.transform);

        // EquipmentSlotsDisplay on EquipmentContent
        var equipSlotsDisplay = equipContent.AddComponent<Starquill.UI.EquipmentSlotsDisplay>();
        SetPrivateField(equipSlotsDisplay, "cardContainer", equipCardContainer.transform);
        SetPrivateField(equipSlotsDisplay, "autoEquipButton", autoEquipBtnObj.GetComponent<Button>());

        // EquipmentDrawer on EquipmentContent
        var equipDrawer = equipContent.AddComponent<Starquill.UI.EquipmentDrawer>();
        SetPrivateField(equipDrawer, "drawerPanel", equipDrawerPanel);
        SetPrivateField(equipDrawer, "summaryLabel", drawerSummaryLabel.GetComponent<TMP_Text>());
        SetPrivateField(equipDrawer, "itemListContainer", drawerListContainer.transform);
        SetPrivateField(equipDrawer, "equipButton", equipBtnObj.GetComponent<Button>());
        SetPrivateField(equipDrawer, "backButton", backBtnObj.GetComponent<Button>());
        SetPrivateField(equipDrawer, "selectedSlotStripe", slotStripe.GetComponent<Image>());
        SetPrivateField(equipDrawer, "selectedSlotLabel", slotLabelTmp as TMP_Text);
        SetPrivateField(equipDrawer, "selectedSlotNameText", slotNameTmp as TMP_Text);
        SetPrivateField(equipDrawer, "selectedSlotInfoText", slotInfoTmp as TMP_Text);
        SetPrivateField(equipDrawer, "selectedSlotSprite", slotSpriteImg);
        SetPrivateField(equipDrawer, "selectedSlotStats", slotStatsTmp as TMP_Text);

        // ActionLoadoutDisplay on ActionsContent
        var actionLoadoutDisplay = actionsContent.AddComponent<Starquill.UI.ActionLoadoutDisplay>();
        SetPrivateField(actionLoadoutDisplay, "equippedContainer", equippedContainer.transform);
        SetPrivateField(actionLoadoutDisplay, "poolSummaryContainer", poolSummaryBar.transform);
        SetPrivateField(actionLoadoutDisplay, "drawerBarLabel", drawerLabel.GetComponent<TMPro.TMP_Text>());
        SetPrivateField(actionLoadoutDisplay, "lockIndicatorLabel", lockLabel.GetComponent<TMPro.TMP_Text>());
        SetPrivateField(actionLoadoutDisplay, "lockProgressBar", slider);
        SetPrivateField(actionLoadoutDisplay, "lockIndicatorObj", lockIndicator);

        // Wire PartyScreenController SerializeField references (all components now exist)
        SetPrivateField(partyScreenCtrl, "focusDisplay", focusDisplay);
        SetPrivateField(partyScreenCtrl, "portraitStrip", portraitStripComp);
        SetPrivateField(partyScreenCtrl, "rosterGrid", rosterGridDisplay);
        SetPrivateField(partyScreenCtrl, "equipmentSlots", equipSlotsDisplay);
        SetPrivateField(partyScreenCtrl, "equipmentDrawer", equipDrawer);
        SetPrivateField(partyScreenCtrl, "actionLoadout", actionLoadoutDisplay);
        SetPrivateField(partyScreenCtrl, "screenManager", screenManager);
        SetPrivateField(partyScreenCtrl, "rosterContent", rosterContent);
        SetPrivateField(partyScreenCtrl, "equipmentContent", equipContent);
        SetPrivateField(partyScreenCtrl, "equipmentListArea", equipListArea);
        SetPrivateField(partyScreenCtrl, "actionsContent", actionsContent);
        SetPrivateField(partyScreenCtrl, "portraitStripObj", portraitStrip);
        SetPrivateField(partyScreenCtrl, "characterInfoObj", charInfo);
        SetPrivateField(partyScreenCtrl, "subTabButtons", subTabButtons);

        // ExploreSceneController on Canvas
        var controller = canvas.AddComponent<Starquill.UI.ExploreSceneController>();
        SetPrivateField(controller, "topBar", topBarDisplay);
        SetPrivateField(controller, "verbBar", verbBarDisplay);
        SetPrivateField(controller, "bottomNav", bottomNavDisplay);
        SetPrivateField(controller, "partySlots", new RawImage[] {
            backLeft.GetComponent<RawImage>(),
            backRight.GetComponent<RawImage>(),
            frontLeft.GetComponent<RawImage>(),
            frontRight.GetComponent<RawImage>()
        });
        SetPrivateField(controller, "bgSky", bgSky.GetComponent<Starquill.UI.ParallaxLayer>());
        SetPrivateField(controller, "bgMid", bgMid.GetComponent<Starquill.UI.ParallaxLayer>());
        SetPrivateField(controller, "bgGround", bgGround.GetComponent<Starquill.UI.ParallaxLayer>());
        SetPrivateField(controller, "fgGrass", fgGrass.GetComponent<Starquill.UI.ParallaxLayer>());
        SetPrivateField(controller, "enemyDisplay", enemyDisplayCtrl);
        SetPrivateField(controller, "damageNumbers", damageSpawner);
        SetPrivateField(controller, "goldCounter", goldAnimator);

        // LootScreenController on LootPanel
        var lootScreenCtrl = lootPanel.AddComponent<Starquill.UI.LootScreenController>();
        SetPrivateField(lootScreenCtrl, "inventoryCountLabel", inventoryCountLabel.GetComponent<TMP_Text>());
        SetPrivateField(lootScreenCtrl, "itemGridContainer", lootGridContainer.transform);
        SetPrivateField(lootScreenCtrl, "optimizeAllButton", optimizeAllBtnObj.GetComponent<Button>());
        SetPrivateField(lootScreenCtrl, "screenManager", screenManager);

        // ItemDetailPanel on LootPanel
        var itemDetail = lootPanel.AddComponent<Starquill.UI.ItemDetailPanel>();
        SetPrivateField(itemDetail, "panel", itemDetailOverlay);
        SetPrivateField(itemDetail, "itemNameLabel", detailNameLabel.GetComponent<TMP_Text>());
        SetPrivateField(itemDetail, "itemStatsLabel", detailStatsLabel.GetComponent<TMP_Text>());
        SetPrivateField(itemDetail, "sellValueLabel", detailSellLabel.GetComponent<TMP_Text>());
        SetPrivateField(itemDetail, "sellButton", sellBtnObj.GetComponent<Button>());
        SetPrivateField(itemDetail, "closeButton", closeBtnObj.GetComponent<Button>());
        SetPrivateField(itemDetail, "characterPickerContainer", charPickerContainer.transform);

        // Wire ItemDetailPanel to LootScreenController
        SetPrivateField(lootScreenCtrl, "itemDetailPanel", itemDetail);

        // Wire ExploreSceneController reference on PartyScreenController (now that controller exists)
        SetPrivateField(partyScreenCtrl, "exploreController", controller);

        // Initially show only ExplorePanel (index 0), hide others
        screenManager.ShowScreen(0);

        // Ensure EventSystem has StandaloneInputModule for Canvas UI buttons
        var eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem != null && eventSystem.GetComponent<StandaloneInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            EditorUtility.SetDirty(eventSystem.gameObject);
        }

        EditorUtility.SetDirty(canvas);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("ExploreScene built successfully!");
    }

    static GameObject CreatePanel(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        return go;
    }

    static GameObject CreatePlaceholderPanel(string name, Transform parent, string labelText)
    {
        var go = CreatePanel(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(0, 120);
        rt.offsetMax = new Vector2(0, -100);
        go.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 1f);

        var label = CreateTMPLabel("Label", go.transform, labelText, 36, new Color(0.5f, 0.5f, 0.5f), TextAlignmentOptions.Center);
        StretchFill(label);

        return go;
    }

    static GameObject CreateRawImage(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(parent, false);
        go.GetComponent<RawImage>().color = new Color(1, 1, 1, 0.5f);
        return go;
    }

    static GameObject CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    static GameObject CreateTMPLabel(string name, Transform parent, string text, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        return go;
    }

    static void StretchFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void StretchFill(GameObject go)
    {
        StretchFill(go.GetComponent<RectTransform>());
    }

    static void AnchorBottom(GameObject go, float height)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0, height);
    }

    static void SetRawImageRect(GameObject go, Vector2 position, Vector2 size)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    static void SetImageRect(GameObject go, Vector2 position, Vector2 size)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0);
        rt.anchorMax = new Vector2(0.5f, 0);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    static void AddParallaxLayer(GameObject go, float speed)
    {
        var layer = go.AddComponent<Starquill.UI.ParallaxLayer>();
        layer.ScrollSpeed = speed;
    }

    static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            field.SetValue(target, value);
        else
            Debug.LogWarning($"Field '{fieldName}' not found on {target.GetType().Name}");
    }
}
