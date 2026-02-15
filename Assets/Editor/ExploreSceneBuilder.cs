using UnityEngine;
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

        // Clean existing children except EventSystem-related
        for (int i = canvasRT.childCount - 1; i >= 0; i--)
        {
            var child = canvasRT.GetChild(i);
            Undo.DestroyObjectImmediate(child.gameObject);
        }

        // === COMBAT AREA PANEL (middle fill) ===
        var combatArea = CreatePanel("CombatAreaPanel", canvasRT);
        var combatRT = combatArea.GetComponent<RectTransform>();
        combatRT.anchorMin = new Vector2(0, 0);
        combatRT.anchorMax = new Vector2(1, 1);
        combatRT.offsetMin = new Vector2(0, 250); // bottom = VerbBar(130) + BottomNav(120)
        combatRT.offsetMax = new Vector2(0, -100); // top = TopBar(100)

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

        // === TOP BAR PANEL (anchored top, 100px, single row) ===
        var topBar = CreatePanel("TopBarPanel", canvasRT);
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

        // === VERB BAR PANEL (above bottom nav, 130px, single row) ===
        var verbBar = CreatePanel("VerbBarPanel", canvasRT);
        var verbBarRT = verbBar.GetComponent<RectTransform>();
        verbBarRT.anchorMin = new Vector2(0, 0);
        verbBarRT.anchorMax = new Vector2(1, 0);
        verbBarRT.pivot = new Vector2(0.5f, 0);
        verbBarRT.anchoredPosition = new Vector2(0, 120);
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

        // === BOTTOM NAV PANEL (anchored bottom, 120px) ===
        var bottomNav = CreatePanel("BottomNavPanel", canvasRT);
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

        // === ADD COMPONENTS ===
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

        // BottomNavDisplay
        var bottomNavDisplay = bottomNav.AddComponent<Starquill.UI.BottomNavDisplay>();

        // DamageNumberSpawner on CombatAreaPanel
        var damageSpawner = combatArea.AddComponent<Starquill.UI.DamageNumberSpawner>();

        // GoldCounterAnimator on TopBarPanel
        var goldAnimator = topBar.AddComponent<Starquill.UI.GoldCounterAnimator>();
        SetPrivateField(goldAnimator, "label", goldLabel.GetComponent<TMP_Text>());

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
