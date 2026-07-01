# Sprint 3: Explore Screen UI — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build the Explore screen as a static UI layout with live paper doll rendering, parallax scrolling background, placeholder verb cards, and navigation bar.

**Architecture:** Canvas + TextMeshPro UI with a new `Starquill.UI` assembly. Parallax scrolling via RawImage UV offset manipulation. Four CharacterDisplay instances from Sprint 2 render party paper dolls into staggered RawImages. All content is placeholder data — no GameManager wiring.

**Tech Stack:** Unity 6 (6000.3.8f1), Canvas UI, TextMeshPro (via ugui 2.0.0), RawImage UV scrolling

**Design doc:** `docs/plans/2026-02-13-explore-screen-design.md`

---

### Task 1: Create Starquill.UI Assembly Definition

**Files:**
- Create: `Assets/Scripts/UI/Starquill.UI.asmdef`
- Modify: `Assets/Tests/EditMode/EditModeTests.asmdef`

**Step 1: Create the UI assembly definition**

Create `Assets/Scripts/UI/Starquill.UI.asmdef`:

```json
{
    "name": "Starquill.UI",
    "rootNamespace": "Starquill.UI",
    "references": [
        "Starquill.Core",
        "Starquill.Data",
        "Starquill.Display",
        "Unity.TextMeshPro"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "autoReferenced": true
}
```

**Step 2: Update EditModeTests assembly to reference Starquill.UI**

In `Assets/Tests/EditMode/EditModeTests.asmdef`, add `"Starquill.UI"` to the `references` array, after `"Starquill.Display"`.

**Step 3: Verify compilation**

Run: `Unity -batchmode -nographics -projectPath /home/keroppi/Development/Starquill -quit -logFile - 2>&1 | tail -5`
Expected: Exit code 0, no compilation errors referencing Starquill.UI

**Step 4: Commit**

```bash
git add Assets/Scripts/UI/Starquill.UI.asmdef Assets/Scripts/UI/Starquill.UI.asmdef.meta Assets/Tests/EditMode/EditModeTests.asmdef
git commit -m "Add Starquill.UI assembly definition and update test references"
```

---

### Task 2: ParallaxMath with Tests (TDD)

**Files:**
- Create: `Assets/Scripts/UI/ParallaxMath.cs`
- Create: `Assets/Tests/EditMode/UI/ParallaxMathTests.cs`

**Step 1: Write the failing tests**

Create `Assets/Tests/EditMode/UI/ParallaxMathTests.cs`:

```csharp
using NUnit.Framework;
using Starquill.UI;

namespace Starquill.Tests.UI
{
    [TestFixture]
    public class ParallaxMathTests
    {
        [Test]
        public void CalculateUVWidth_ReturnsRatioOfDisplayToTexture()
        {
            float result = ParallaxMath.CalculateUVWidth(1080f, 2160f);
            Assert.AreEqual(0.5f, result, 0.001f);
        }

        [Test]
        public void CalculateUVWidth_SameSizeReturnsOne()
        {
            float result = ParallaxMath.CalculateUVWidth(1080f, 1080f);
            Assert.AreEqual(1f, result, 0.001f);
        }

        [Test]
        public void CalculateUVWidth_ZeroTextureWidthReturnsOne()
        {
            float result = ParallaxMath.CalculateUVWidth(1080f, 0f);
            Assert.AreEqual(1f, result, 0.001f);
        }

        [Test]
        public void AdvanceOffset_MovesForward()
        {
            float result = ParallaxMath.AdvanceOffset(0f, 30f, 1f, 2160f);
            Assert.AreEqual(30f / 2160f, result, 0.0001f);
        }

        [Test]
        public void AdvanceOffset_WrapsAtOne()
        {
            float result = ParallaxMath.AdvanceOffset(0.95f, 2160f, 1f, 2160f);
            // 0.95 + 1.0 = 1.95, wraps to 0.95
            Assert.AreEqual(0.95f, result, 0.001f);
        }

        [Test]
        public void AdvanceOffset_HandlesNegativeSpeed()
        {
            float result = ParallaxMath.AdvanceOffset(0.1f, -30f, 1f, 2160f);
            float expected = 0.1f - 30f / 2160f;
            Assert.AreEqual(expected, result, 0.0001f);
        }

        [Test]
        public void AdvanceOffset_NegativeWrapsToPositive()
        {
            float result = ParallaxMath.AdvanceOffset(0.01f, -2160f, 1f, 2160f);
            // 0.01 - 1.0 = -0.99, wraps to 0.01
            Assert.AreEqual(0.01f, result, 0.001f);
        }

        [Test]
        public void AdvanceOffset_ZeroTextureWidthReturnsCurrentOffset()
        {
            float result = ParallaxMath.AdvanceOffset(0.5f, 30f, 1f, 0f);
            Assert.AreEqual(0.5f, result, 0.001f);
        }
    }
}
```

**Step 2: Run tests to verify they fail**

Open Unity Editor > Window > General > Test Runner > EditMode > Run All.
Expected: 8 failures — `ParallaxMath` class not found.

**Step 3: Write the implementation**

Create `Assets/Scripts/UI/ParallaxMath.cs`:

```csharp
namespace Starquill.UI
{
    public static class ParallaxMath
    {
        public static float CalculateUVWidth(float displayWidth, float textureWidth)
        {
            if (textureWidth <= 0f) return 1f;
            return displayWidth / textureWidth;
        }

        public static float AdvanceOffset(float currentOffset, float scrollSpeed, float deltaTime, float textureWidth)
        {
            if (textureWidth <= 0f) return currentOffset;
            float newOffset = currentOffset + scrollSpeed * deltaTime / textureWidth;
            newOffset %= 1f;
            if (newOffset < 0f) newOffset += 1f;
            return newOffset;
        }
    }
}
```

**Step 4: Run tests to verify they pass**

Open Unity Editor > Test Runner > EditMode > Run All.
Expected: All 8 ParallaxMath tests pass (83 total with Sprint 1+2).

**Step 5: Commit**

```bash
git add Assets/Scripts/UI/ParallaxMath.cs Assets/Scripts/UI/ParallaxMath.cs.meta Assets/Tests/EditMode/UI/ Assets/Tests/EditMode/UI.meta
git commit -m "Add ParallaxMath with UV scroll math and 8 tests"
```

---

### Task 3: ParallaxLayer MonoBehaviour

**Files:**
- Create: `Assets/Scripts/UI/ParallaxLayer.cs`

**Step 1: Write the implementation**

Create `Assets/Scripts/UI/ParallaxLayer.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace Starquill.UI
{
    [RequireComponent(typeof(RawImage))]
    public class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] private float scrollSpeed = 30f;

        private RawImage rawImage;
        private float offset;
        private float uvWidth = 1f;

        public float ScrollSpeed
        {
            get => scrollSpeed;
            set => scrollSpeed = value;
        }

        private void Start()
        {
            rawImage = GetComponent<RawImage>();
            if (rawImage.texture != null)
            {
                uvWidth = ParallaxMath.CalculateUVWidth(
                    rawImage.rectTransform.rect.width,
                    rawImage.texture.width);
                rawImage.uvRect = new Rect(0, 0, uvWidth, 1);
            }
        }

        private void Update()
        {
            if (rawImage == null || rawImage.texture == null) return;
            offset = ParallaxMath.AdvanceOffset(offset, scrollSpeed, Time.deltaTime, rawImage.texture.width);
            rawImage.uvRect = new Rect(offset, 0, uvWidth, 1);
        }

        public void SetTexture(Texture2D texture)
        {
            if (rawImage == null) rawImage = GetComponent<RawImage>();
            rawImage.texture = texture;
            uvWidth = ParallaxMath.CalculateUVWidth(
                rawImage.rectTransform.rect.width,
                texture.width);
            rawImage.uvRect = new Rect(0, 0, uvWidth, 1);
        }
    }
}
```

**Step 2: Verify compilation**

Run: `Unity -batchmode -nographics -projectPath /home/keroppi/Development/Starquill -quit -logFile - 2>&1 | tail -5`
Expected: No compilation errors.

**Step 3: Commit**

```bash
git add Assets/Scripts/UI/ParallaxLayer.cs Assets/Scripts/UI/ParallaxLayer.cs.meta
git commit -m "Add ParallaxLayer with RawImage UV scrolling"
```

---

### Task 4: TopBarDisplay MonoBehaviour

**Files:**
- Create: `Assets/Scripts/UI/TopBarDisplay.cs`

**Step 1: Write the implementation**

Create `Assets/Scripts/UI/TopBarDisplay.cs`:

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Starquill.UI
{
    public class TopBarDisplay : MonoBehaviour
    {
        [Header("Top Row")]
        [SerializeField] private TMP_Text goldLabel;
        [SerializeField] private TMP_Text levelLabel;

        [Header("Fragment Bar")]
        [SerializeField] private Image fragmentBarFill;
        [SerializeField] private TMP_Text fragmentLabel;

        [Header("Wave Info")]
        [SerializeField] private TMP_Text waveLabel;
        [SerializeField] private TMP_Text questLevelLabel;

        public void SetGold(string formatted)
        {
            if (goldLabel != null) goldLabel.text = formatted;
        }

        public void SetLevel(int level)
        {
            if (levelLabel != null) levelLabel.text = $"Lv {level}";
        }

        public void SetFragments(int current, int max)
        {
            if (fragmentLabel != null) fragmentLabel.text = $"{current}/{max}";
            if (fragmentBarFill != null) fragmentBarFill.fillAmount = max > 0 ? (float)current / max : 0f;
        }

        public void SetWaveInfo(int wave, int maxWave)
        {
            if (waveLabel != null) waveLabel.text = $"Wave {wave}/{maxWave}";
        }

        public void SetQuestLevel(int level)
        {
            if (questLevelLabel != null) questLevelLabel.text = $"Quest Lv {level}";
        }
    }
}
```

**Step 2: Verify compilation**

Run: `Unity -batchmode -nographics -projectPath /home/keroppi/Development/Starquill -quit -logFile - 2>&1 | tail -5`
Expected: No compilation errors.

**Step 3: Commit**

```bash
git add Assets/Scripts/UI/TopBarDisplay.cs Assets/Scripts/UI/TopBarDisplay.cs.meta
git commit -m "Add TopBarDisplay for gold, level, fragments, and wave info"
```

---

### Task 5: VerbBarDisplay MonoBehaviour

**Files:**
- Create: `Assets/Scripts/UI/VerbBarDisplay.cs`

**Step 1: Write the implementation**

The VerbBarDisplay creates verb card UI elements inside a GridLayoutGroup at runtime. Each card has a colored background Image and two TMP_Text children (verb name, cooldown timer).

Create `Assets/Scripts/UI/VerbBarDisplay.cs`:

```csharp
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Starquill.UI
{
    public class VerbBarDisplay : MonoBehaviour
    {
        [SerializeField] private Transform cardContainer;
        [SerializeField] private Vector2 cardSize = new Vector2(300, 100);
        [SerializeField] private int maxCards = 6;

        private readonly List<GameObject> cards = new();

        public void PopulateWithPlaceholders()
        {
            ClearCards();

            var placeholders = new[]
            {
                ("Bash", new Color(0.9f, 0.5f, 0.1f)),
                ("Analyze", new Color(0.2f, 0.5f, 0.9f)),
                ("Slash", new Color(0.7f, 0.7f, 0.7f)),
                ("Heal", new Color(0.2f, 0.8f, 0.4f)),
                ("Shield", new Color(0.6f, 0.4f, 0.2f))
            };

            for (int i = 0; i < placeholders.Length && i < maxCards; i++)
            {
                var (verbName, color) = placeholders[i];
                CreateCard(verbName, color, $"{3 + i * 2}s left");
            }
        }

        private void CreateCard(string verbName, Color bgColor, string cooldownText)
        {
            var parent = cardContainer != null ? cardContainer : transform;

            var card = new GameObject($"VerbCard_{cards.Count}");
            card.transform.SetParent(parent, false);

            var cardRT = card.AddComponent<RectTransform>();
            cardRT.sizeDelta = cardSize;

            var bg = card.AddComponent<Image>();
            bg.color = bgColor;

            var nameObj = new GameObject("VerbName");
            nameObj.transform.SetParent(card.transform, false);
            var nameRT = nameObj.AddComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 0.4f);
            nameRT.anchorMax = new Vector2(1, 1);
            nameRT.offsetMin = Vector2.zero;
            nameRT.offsetMax = Vector2.zero;
            var nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
            nameTMP.text = verbName;
            nameTMP.fontSize = 28;
            nameTMP.alignment = TextAlignmentOptions.Center;
            nameTMP.color = Color.white;

            var cdObj = new GameObject("Cooldown");
            cdObj.transform.SetParent(card.transform, false);
            var cdRT = cdObj.AddComponent<RectTransform>();
            cdRT.anchorMin = new Vector2(0, 0);
            cdRT.anchorMax = new Vector2(1, 0.4f);
            cdRT.offsetMin = Vector2.zero;
            cdRT.offsetMax = Vector2.zero;
            var cdTMP = cdObj.AddComponent<TextMeshProUGUI>();
            cdTMP.text = cooldownText;
            cdTMP.fontSize = 18;
            cdTMP.alignment = TextAlignmentOptions.Center;
            cdTMP.color = new Color(1, 1, 1, 0.7f);

            cards.Add(card);
        }

        private void ClearCards()
        {
            foreach (var card in cards)
                if (card != null) Destroy(card);
            cards.Clear();
        }
    }
}
```

**Step 2: Verify compilation**

Run: `Unity -batchmode -nographics -projectPath /home/keroppi/Development/Starquill -quit -logFile - 2>&1 | tail -5`
Expected: No compilation errors.

**Step 3: Commit**

```bash
git add Assets/Scripts/UI/VerbBarDisplay.cs Assets/Scripts/UI/VerbBarDisplay.cs.meta
git commit -m "Add VerbBarDisplay with placeholder verb card creation"
```

---

### Task 6: BottomNavDisplay MonoBehaviour

**Files:**
- Create: `Assets/Scripts/UI/BottomNavDisplay.cs`

**Step 1: Write the implementation**

Create `Assets/Scripts/UI/BottomNavDisplay.cs`:

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Starquill.UI
{
    public class BottomNavDisplay : MonoBehaviour
    {
        [SerializeField] private Button[] navButtons;
        [SerializeField] private Color activeColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 0.5f);

        private int activeTab;

        public void SetActiveTab(int index)
        {
            activeTab = index;
            for (int i = 0; i < navButtons.Length; i++)
            {
                if (navButtons[i] == null) continue;
                var colors = navButtons[i].colors;
                colors.normalColor = i == index ? activeColor : inactiveColor;
                navButtons[i].colors = colors;

                var label = navButtons[i].GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.color = i == index ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            }
        }

        public void CreatePlaceholderButtons()
        {
            var tabNames = new[] { "Explore", "Quests", "Loot", "Party", "Shop" };
            navButtons = new Button[tabNames.Length];

            for (int i = 0; i < tabNames.Length; i++)
            {
                var btnObj = new GameObject($"NavBtn_{tabNames[i]}");
                btnObj.transform.SetParent(transform, false);

                var rt = btnObj.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(200, 100);

                var img = btnObj.AddComponent<Image>();
                img.color = inactiveColor;

                var btn = btnObj.AddComponent<Button>();
                navButtons[i] = btn;

                var labelObj = new GameObject("Label");
                labelObj.transform.SetParent(btnObj.transform, false);
                var labelRT = labelObj.AddComponent<RectTransform>();
                labelRT.anchorMin = Vector2.zero;
                labelRT.anchorMax = Vector2.one;
                labelRT.offsetMin = Vector2.zero;
                labelRT.offsetMax = Vector2.zero;
                var tmp = labelObj.AddComponent<TextMeshProUGUI>();
                tmp.text = tabNames[i];
                tmp.fontSize = 24;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(0.7f, 0.7f, 0.7f);
            }
        }
    }
}
```

**Step 2: Verify compilation**

Run: `Unity -batchmode -nographics -projectPath /home/keroppi/Development/Starquill -quit -logFile - 2>&1 | tail -5`
Expected: No compilation errors.

**Step 3: Commit**

```bash
git add Assets/Scripts/UI/BottomNavDisplay.cs Assets/Scripts/UI/BottomNavDisplay.cs.meta
git commit -m "Add BottomNavDisplay with tab highlighting"
```

---

### Task 7: ExploreSceneController

**Files:**
- Create: `Assets/Scripts/UI/ExploreSceneController.cs`

This is the scene orchestrator. It creates CharacterDisplay instances for the party, generates placeholder parallax textures, and populates all UI panels with placeholder data.

**Step 1: Write the implementation**

Create `Assets/Scripts/UI/ExploreSceneController.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Display;

namespace Starquill.UI
{
    public class ExploreSceneController : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private TopBarDisplay topBar;
        [SerializeField] private VerbBarDisplay verbBar;
        [SerializeField] private BottomNavDisplay bottomNav;

        [Header("Party Slots")]
        [SerializeField] private RawImage[] partySlots;

        [Header("Parallax Layers")]
        [SerializeField] private ParallaxLayer bgSky;
        [SerializeField] private ParallaxLayer bgMid;
        [SerializeField] private ParallaxLayer bgGround;
        [SerializeField] private ParallaxLayer fgGrass;

        [Header("Enemy Silhouettes")]
        [SerializeField] private Image[] enemySilhouettes;

        private readonly List<CharacterDisplay> characterDisplays = new();
        private readonly List<Texture2D> placeholderTextures = new();

        private void Start()
        {
            SetupPlaceholderParallax();
            SetupPlaceholderParty();
            SetupPlaceholderEnemies();
            SetupPlaceholderUI();
        }

        private void SetupPlaceholderParallax()
        {
            if (bgSky != null)
                bgSky.SetTexture(CreatePlaceholderTexture(2160, 1400, new Color(0.4f, 0.6f, 0.9f), 400));
            if (bgMid != null)
                bgMid.SetTexture(CreatePlaceholderTexture(2160, 840, new Color(0.2f, 0.5f, 0.3f), 200));
            if (bgGround != null)
                bgGround.SetTexture(CreatePlaceholderTexture(2160, 420, new Color(0.45f, 0.35f, 0.2f), 150));
            if (fgGrass != null)
                fgGrass.SetTexture(CreatePlaceholderTexture(2160, 280, new Color(0.3f, 0.7f, 0.2f, 0.6f), 100));
        }

        private void SetupPlaceholderParty()
        {
            if (partySlots == null || partySlots.Length == 0) return;

            var registry = DisplayDataRegistry.Instance;
            var speciesKeys = registry.GetSpeciesKeys();
            if (speciesKeys.Count == 0) return;

            var builder = new DisplayBuilder();
            var resolver = new ImageResolver();

            for (int i = 0; i < partySlots.Length && i < 4; i++)
            {
                if (partySlots[i] == null) continue;

                var speciesKey = speciesKeys[i % speciesKeys.Count];
                var speciesData = registry.GetSpeciesDisplayData(speciesKey);
                if (speciesData == null) continue;

                var instance = SpeciesInstanceData.CreateFrom(speciesData, registry);
                var pieces = builder.Build(instance, speciesData, new List<EquipmentDisplayInfo>());

                var displayObj = new GameObject($"CharDisplay_{i}");
                displayObj.transform.SetParent(transform);
                var display = displayObj.AddComponent<CharacterDisplay>();
                display.Initialize(resolver);
                display.SetPieces(pieces);

                partySlots[i].texture = display.Texture;
                characterDisplays.Add(display);
            }
        }

        private void SetupPlaceholderEnemies()
        {
            if (enemySilhouettes == null) return;
            foreach (var silhouette in enemySilhouettes)
            {
                if (silhouette == null) continue;
                silhouette.color = new Color(0.15f, 0.15f, 0.2f, 0.8f);
            }
        }

        private void SetupPlaceholderUI()
        {
            if (topBar != null)
            {
                topBar.SetGold("1.2M");
                topBar.SetLevel(34);
                topBar.SetFragments(7, 12);
                topBar.SetWaveInfo(3, 5);
                topBar.SetQuestLevel(34);
            }

            if (verbBar != null)
                verbBar.PopulateWithPlaceholders();

            if (bottomNav != null)
            {
                bottomNav.CreatePlaceholderButtons();
                bottomNav.SetActiveTab(0);
            }
        }

        private Texture2D CreatePlaceholderTexture(int width, int height, Color baseColor, int stripeSpacing)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;

            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color c = baseColor;
                    // Vertical stripes for visible scroll movement
                    if (stripeSpacing > 0 && x % stripeSpacing < stripeSpacing / 10)
                        c = Color.Lerp(c, Color.white, 0.15f);
                    // Vertical gradient (lighter at top)
                    float gradientT = (float)y / height;
                    c = Color.Lerp(c, Color.Lerp(c, Color.white, 0.2f), gradientT);
                    pixels[y * width + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            placeholderTextures.Add(tex);
            return tex;
        }

        private void OnDestroy()
        {
            foreach (var tex in placeholderTextures)
                if (tex != null) Destroy(tex);
            placeholderTextures.Clear();
        }
    }
}
```

**Step 2: Verify compilation**

Run: `Unity -batchmode -nographics -projectPath /home/keroppi/Development/Starquill -quit -logFile - 2>&1 | tail -5`
Expected: No compilation errors.

**Step 3: Commit**

```bash
git add Assets/Scripts/UI/ExploreSceneController.cs Assets/Scripts/UI/ExploreSceneController.cs.meta
git commit -m "Add ExploreSceneController with placeholder party, parallax, and UI"
```

---

### Task 8: Run All Tests

**Step 1: Open Unity Editor and run all EditMode tests**

Open Unity Editor > Window > General > Test Runner > EditMode > Run All.

Expected: 83 tests pass (49 Sprint 1 + 26 Sprint 2 + 8 Sprint 3 ParallaxMath).

If any tests fail, fix before proceeding.

---

### Task 9: Scene Setup in Unity Editor (Manual)

This task is performed manually in the Unity Editor. Follow each step precisely.

**Step 1: Create the scene**

File > New Scene > Basic (Built-in). Save as `Assets/Scenes/ExploreScene.unity`.

**Step 2: Delete default objects**

Delete: Main Camera, Directional Light (Canvas will add its own camera handling).

**Step 3: Create EventSystem**

GameObject > UI > EventSystem.
- Delete the `StandaloneInputModule` component.
- Add component: `InputSystemUIInputModule` (search "Input System UI").

**Step 4: Create Canvas**

GameObject > UI > Canvas.
- Canvas component: Render Mode = **Screen Space - Overlay**
- Add component: **CanvasScaler**
  - UI Scale Mode = **Scale With Screen Size**
  - Reference Resolution = **1080 x 1920**
  - Screen Match Mode = **Match Width Or Height**
  - Match = **1** (match height)

**Step 5: Create TopBarPanel**

Right-click Canvas > Create Empty. Name: `TopBarPanel`.
- RectTransform: Anchor = **Top Stretch** (top-left to top-right)
- Height = **160**, Left/Right = **0**, Top = **0**
- Add component: **Vertical Layout Group** (child alignment: upper left, spacing: 0)

Create children inside TopBarPanel:

5a. `TopRow` (Create Empty inside TopBarPanel)
- RectTransform: height = **80**
- Add component: **Horizontal Layout Group** (spacing: 20, padding: left 20 right 20, child alignment: middle center)
- Add component: **Layout Element** (preferred height: 80)

5b. `GoldLabel` (UI > Text - TextMeshPro inside TopRow)
- Text: `1.2M Gold`
- Font size: **36**, color: **gold** (255, 215, 0)
- Alignment: center-left
- Add: **Layout Element** (flexible width: 1)

5c. `LevelLabel` (UI > Text - TextMeshPro inside TopRow)
- Text: `Lv 34`
- Font size: **36**, color: **white**
- Alignment: center
- Add: **Layout Element** (flexible width: 1)

5d. `FragmentBarGroup` (Create Empty inside TopRow)
- Add: **Layout Element** (flexible width: 1, preferred height: 40)
- Create child `FragmentBarBG` (UI > Image): color dark gray (0.2, 0.2, 0.2), stretch fill
- Create child `FragmentBarFill` (UI > Image): color teal (0.2, 0.8, 0.7), Image Type = **Filled**, Fill Method = **Horizontal**, Fill Amount = **0.583** (7/12)
- Create child `FragmentLabel` (UI > Text - TextMeshPro): text `7/12`, font size 20, white, center, stretch fill

5e. `WaveRow` (Create Empty inside TopBarPanel)
- RectTransform: height = **60**
- Add: **Horizontal Layout Group** (spacing: 40, child alignment: middle center)
- Add: **Layout Element** (preferred height: 60)

5f. `WaveLabel` (TMP inside WaveRow): text `Wave 3/5`, font size **28**, white, center
5g. `QuestLevelLabel` (TMP inside WaveRow): text `Quest Lv 34`, font size **28**, white, center

5h. Add **TopBarDisplay** component to TopBarPanel. Drag references:
- goldLabel → GoldLabel
- levelLabel → LevelLabel
- fragmentBarFill → FragmentBarFill
- fragmentLabel → FragmentLabel
- waveLabel → WaveLabel
- questLevelLabel → QuestLevelLabel

**Step 6: Create CombatAreaPanel**

Right-click Canvas > Create Empty. Name: `CombatAreaPanel`.
- RectTransform: Anchor = **Stretch** (all corners)
- Top = **160**, Bottom = **360** (leaves room for VerbBar + BottomNav)
- Left = **0**, Right = **0**

6a. `BG_Sky` (UI > Raw Image inside CombatAreaPanel)
- RectTransform: Anchor = **Stretch**, all offsets = **0** (fills entire panel)
- Add component: **ParallaxLayer**, scrollSpeed = **5**

6b. `BG_Mid` (UI > Raw Image inside CombatAreaPanel)
- RectTransform: Anchor = **Bottom Stretch** (bottom-left to bottom-right)
- Height = **840**, Left/Right = **0**, Bottom = **0**
- Add component: **ParallaxLayer**, scrollSpeed = **15**

6c. `BG_Ground` (UI > Raw Image inside CombatAreaPanel)
- RectTransform: Anchor = **Bottom Stretch**
- Height = **420**, Left/Right = **0**, Bottom = **0**
- Add component: **ParallaxLayer**, scrollSpeed = **30**

6d. `PartyContainer` (Create Empty inside CombatAreaPanel)
- RectTransform: Anchor = **Left Stretch** (left side)
- Width = **55%** of parent (594px), Top/Bottom = **0**, Left = **0**

6e. Party slots inside PartyContainer (all UI > Raw Image):
- `PartySlot_BackLeft`: anchoredPosition = **(100, 350)**, size = **(180, 180)**
- `PartySlot_BackRight`: anchoredPosition = **(260, 320)**, size = **(180, 180)**
- `PartySlot_FrontLeft`: anchoredPosition = **(60, 150)**, size = **(220, 220)**
- `PartySlot_FrontRight`: anchoredPosition = **(230, 120)**, size = **(220, 220)**

(Positions are approximate — adjust visually so front row overlaps back row edges.)

6f. `EnemyContainer` (Create Empty inside CombatAreaPanel)
- RectTransform: Anchor = **Right Stretch**
- Width = **35%** (378px), Top/Bottom = **0**, Right = **0**

6g. Enemy silhouettes inside EnemyContainer (all UI > Image):
- `EnemySilhouette_0`: anchoredPosition = **(-260, 300)**, size = **(120, 180)**, color = dark (0.15, 0.15, 0.2, 0.8)
- `EnemySilhouette_1`: anchoredPosition = **(-140, 250)**, size = **(120, 180)**, same color
- `EnemySilhouette_2`: anchoredPosition = **(-200, 150)**, size = **(120, 180)**, same color

6h. `FG_Grass` (UI > Raw Image inside CombatAreaPanel)
- RectTransform: Anchor = **Bottom Stretch**
- Height = **280**, Left/Right = **0**, Bottom = **0**
- Add component: **ParallaxLayer**, scrollSpeed = **50**

**Step 7: Create VerbBarPanel**

Right-click Canvas > Create Empty. Name: `VerbBarPanel`.
- RectTransform: Anchor = **Bottom Stretch**
- Height = **240**, Bottom = **120** (above BottomNav), Left = **0**, Right = **0**

7a. `VerbGrid` (Create Empty inside VerbBarPanel)
- RectTransform: Stretch fill, padding 10 on all sides
- Add component: **Grid Layout Group**
  - Cell Size: **300 x 100**
  - Spacing: **15 x 10**
  - Constraint: **Fixed Column Count = 3**
  - Child Alignment: **Upper Center**

7b. Add **VerbBarDisplay** component to VerbBarPanel.
- cardContainer → VerbGrid

**Step 8: Create BottomNavPanel**

Right-click Canvas > Create Empty. Name: `BottomNavPanel`.
- RectTransform: Anchor = **Bottom Stretch**
- Height = **120**, Bottom = **0**, Left = **0**, Right = **0**
- Add component: **Horizontal Layout Group** (spacing: 5, padding: 10, child force expand width: true)
- Add component: **Image** (color: dark background, 0.1, 0.1, 0.15)

8a. Add **BottomNavDisplay** component to BottomNavPanel.

**Step 9: Add ExploreSceneController**

Add **ExploreSceneController** component to the Canvas GameObject.
Drag references in the Inspector:
- topBar → TopBarPanel
- verbBar → VerbBarPanel
- bottomNav → BottomNavPanel
- partySlots → drag all 4 PartySlot RawImages (expand array to 4)
- bgSky → BG_Sky
- bgMid → BG_Mid
- bgGround → BG_Ground
- fgGrass → FG_Grass
- enemySilhouettes → drag all 3 EnemySilhouette Images (expand array to 3)

**Step 10: Save the scene**

Ctrl+S to save `Assets/Scenes/ExploreScene.unity`.

**Step 11: Commit the scene**

```bash
git add Assets/Scenes/ExploreScene.unity Assets/Scenes/ExploreScene.unity.meta
git commit -m "Add ExploreScene with UI layout, parallax, and party paper dolls"
```

---

### Task 10: Visual Verification

Enter Play Mode in the ExploreScene. Verify the following:

- [ ] 4 party paper dolls render on the left side, staggered (front pair larger/lower, back pair smaller/higher)
- [ ] 3 dark silhouette rectangles appear on the right side
- [ ] Background layers (sky, mid, ground) scroll left at different speeds — sky slowest, ground fastest
- [ ] Foreground grass layer scrolls in front of the characters (fastest)
- [ ] Parallax striped textures demonstrate visible scrolling movement
- [ ] TopBar shows: "1.2M Gold", "Lv 34", fragment bar at ~58% fill with "7/12", "Wave 3/5", "Quest Lv 34"
- [ ] VerbBar shows 5 colored cards in 2 rows (3 + 2), with verb names and cooldown text
- [ ] BottomNav shows 5 tab buttons, "Explore" tab highlighted in gold/yellow
- [ ] No console errors (warnings about missing sprites are acceptable)
- [ ] Screen scales correctly in different aspect ratios (use Game view resolution dropdown)

If all items pass, Sprint 3 is complete.

**Step 1: Update sprint review**

Update `docs/sprint-review.md` with Sprint 3 details.

**Step 2: Commit and push**

```bash
git add docs/sprint-review.md
git commit -m "Add Sprint 3 explore screen UI to sprint review"
git push origin unity-idle-clicker
```
