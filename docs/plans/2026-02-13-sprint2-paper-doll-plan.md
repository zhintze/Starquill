# Sprint 2: Paper Doll Display System — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Port the Godot layered paper doll character rendering system to Unity, using RenderTexture compositing with bilinear filtering for smooth scaling of pencil-drawn art across all mobile resolutions.

**Architecture:** A 4-stage DisplayBuilder pipeline produces sorted DisplayPiece lists from character data. Species body part tokens (static, modular full, modular group) resolve to sprite paths via ImageToken. A compositing camera renders all layers into a single RenderTexture at native resolution. The composited texture displays via UI.RawImage with bilinear filtering. All logic except CharacterDisplay is plain C# — fully testable in EditMode.

**Tech Stack:** Unity 6, C#, SpriteRenderer for compositing, RawImage for display, Resources.Load for assets, NUnit for testing.

**Design doc:** `docs/plans/2026-02-13-paper-doll-display-design.md`

---

### Task 1: Migrate Asset Files to Unity Resources

Copy JSON data and sprite images into Unity's Resources folder so `Resources.Load()` can find them at runtime.

**Files:**
- Create: `Assets/Resources/Data/` (directory)
- Create: `Assets/Resources/Images/` (directory)

**Step 1: Copy JSON data files**

```bash
mkdir -p Assets/Resources/Data
cp assets/data/species.json Assets/Resources/Data/
cp assets/data/equipment.json Assets/Resources/Data/
cp assets/data/color_palettes.json Assets/Resources/Data/
cp assets/data/speciesModularParts.json Assets/Resources/Data/
```

Also check if `weapons.json` exists and copy it:
```bash
ls assets/data/weapons.json && cp assets/data/weapons.json Assets/Resources/Data/ || echo "No weapons.json found — check godot-archive/scripts/ for weapon data location"
```

If weapons data is embedded elsewhere (e.g., in equipment.json or a GDScript dict), extract it into a standalone `weapons.json` for Unity.

**Step 2: Copy sprite images**

```bash
mkdir -p Assets/Resources/Images
cp -r assets/images/species Assets/Resources/Images/
cp -r assets/images/equipment Assets/Resources/Images/
cp -r assets/images/weapons Assets/Resources/Images/ 2>/dev/null || echo "No weapons image dir — check assets/images/ for weapon sprites"
```

**Step 3: Verify file counts**

```bash
find Assets/Resources/Images/species -name "*.png" | wc -l
find Assets/Resources/Images/equipment -name "*.png" | wc -l
find Assets/Resources/Data -name "*.json" | wc -l
```

Species should have thousands of PNGs. Equipment should have hundreds. Data should have 4-5 JSON files.

**Step 4: Configure sprite import settings**

Open Unity Editor. Select all PNGs under `Assets/Resources/Images/`. In the Inspector, set:
- Texture Type: Sprite (2D and UI)
- Filter Mode: Bilinear
- Compression: None
- Max Size: 256
- Pixels Per Unit: 100

Apply. This may take time with thousands of sprites.

**Note:** Unity auto-generates `.meta` files for all imported assets. These must be committed.

**Step 5: Commit**

```bash
git add Assets/Resources/
git commit -m "Migrate sprite and JSON assets to Unity Resources folder"
```

---

### Task 2: Create DisplayPiece Class

Plain C# data class with no MonoBehaviour dependency. This is the atomic unit of the rendering pipeline.

**Files:**
- Create: `Assets/Scripts/Display/DisplayPiece.cs`

**Step 1: Write DisplayPiece**

```csharp
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
```

**Step 2: Commit**

```bash
git add Assets/Scripts/Display/DisplayPiece.cs
git commit -m "Add DisplayPiece data class for paper doll layers"
```

---

### Task 3: Create ImageToken Parser with Tests

Parses species body part token strings into their components and constructs sprite resource paths.

**Files:**
- Create: `Assets/Scripts/Display/ImageToken.cs`
- Create: `Assets/Tests/EditMode/Display/ImageTokenTests.cs`

**Step 1: Write the failing tests**

```csharp
using NUnit.Framework;
using Starquill.Display;

namespace Starquill.Tests.Display
{
    public class ImageTokenTests
    {
        [Test]
        public void Parse_StaticToken_ExtractsComponents()
        {
            var token = ImageToken.Parse("0001-082");
            Assert.AreEqual(ImageTokenKind.Static, token.Kind);
            Assert.AreEqual("0001", token.ImageNum);
            Assert.AreEqual(82, token.Layer);
        }

        [Test]
        public void Parse_ModularFullToken_ExtractsComponents()
        {
            var token = ImageToken.Parse("f01-0043-084");
            Assert.AreEqual(ImageTokenKind.ModularFull, token.Kind);
            Assert.AreEqual("f01", token.GroupType);
            Assert.AreEqual("0043", token.ImageNum);
            Assert.AreEqual(84, token.Layer);
        }

        [Test]
        public void Parse_ModularGroupToken_ExtractsGroupType()
        {
            var token = ImageToken.Parse("h02");
            Assert.AreEqual(ImageTokenKind.ModularGroup, token.Kind);
            Assert.AreEqual("h02", token.GroupType);
            Assert.AreEqual(-1, token.Layer);
        }

        [Test]
        public void Parse_EmptyString_ReturnsEmpty()
        {
            var token = ImageToken.Parse("");
            Assert.AreEqual(ImageTokenKind.Empty, token.Kind);
        }

        [Test]
        public void Parse_Null_ReturnsEmpty()
        {
            var token = ImageToken.Parse(null);
            Assert.AreEqual(ImageTokenKind.Empty, token.Kind);
        }

        [Test]
        public void ToSpritePath_Static_ConstructsCorrectPath()
        {
            var token = ImageToken.Parse("0001-016");
            Assert.AreEqual("Images/species/0001-016", token.ToSpeciesSpritePath());
        }

        [Test]
        public void ToSpritePath_ModularFull_ConstructsCorrectPath()
        {
            var token = ImageToken.Parse("f01-0043-084");
            Assert.AreEqual("Images/species/f01-0043-084", token.ToSpeciesSpritePath());
        }

        [Test]
        public void ToEquipmentSpritePath_ConstructsCorrectPath()
        {
            string path = ImageToken.BuildEquipmentSpritePath("hd01", 32, 130);
            Assert.AreEqual("Images/equipment/hd01-0032-130", path);
        }

        [Test]
        public void ToWeaponSpritePath_ConstructsCorrectPath()
        {
            string path = ImageToken.BuildWeaponSpritePath("w01", 164, 1);
            Assert.AreEqual("Images/weapons/w01-164-0001", path);
        }

        [Test]
        public void BuildModularSpritePath_ConstructsCorrectPath()
        {
            string path = ImageToken.BuildModularSpeciesPath("h02", "0043", 92);
            Assert.AreEqual("Images/species/h02-0043-092", path);
        }
    }
}
```

**Step 2: Run tests to verify they fail**

Open Unity Editor, run EditMode tests. All 10 tests should fail with `ImageToken` not found.

**Step 3: Write ImageToken implementation**

```csharp
using System.Text.RegularExpressions;

namespace Starquill.Display
{
    public enum ImageTokenKind
    {
        Empty,
        Static,
        ModularFull,
        ModularGroup
    }

    public class ImageToken
    {
        private static readonly Regex StaticRegex = new(@"^\d{4}-\d{3}$");
        private static readonly Regex ModularFullRegex = new(@"^[A-Za-z]\d{2}-\d{4}-\d{3}$");
        private static readonly Regex ModularGroupRegex = new(@"^[A-Za-z]\d{2}$");

        public ImageTokenKind Kind { get; private set; }
        public string GroupType { get; private set; }
        public string ImageNum { get; private set; }
        public int Layer { get; private set; } = -1;
        public string RawToken { get; private set; }

        private ImageToken() { }

        public static ImageToken Parse(string token)
        {
            var result = new ImageToken();

            if (string.IsNullOrEmpty(token))
            {
                result.Kind = ImageTokenKind.Empty;
                result.RawToken = token ?? "";
                return result;
            }

            result.RawToken = token;

            if (StaticRegex.IsMatch(token))
            {
                result.Kind = ImageTokenKind.Static;
                var parts = token.Split('-');
                result.ImageNum = parts[0];
                result.Layer = int.Parse(parts[1]);
            }
            else if (ModularFullRegex.IsMatch(token))
            {
                result.Kind = ImageTokenKind.ModularFull;
                var parts = token.Split('-');
                result.GroupType = parts[0];
                result.ImageNum = parts[1];
                result.Layer = int.Parse(parts[2]);
            }
            else if (ModularGroupRegex.IsMatch(token))
            {
                result.Kind = ImageTokenKind.ModularGroup;
                result.GroupType = token;
            }
            else
            {
                result.Kind = ImageTokenKind.Empty;
            }

            return result;
        }

        public string ToSpeciesSpritePath()
        {
            return Kind switch
            {
                ImageTokenKind.Static => $"Images/species/{ImageNum}-{Layer:D3}",
                ImageTokenKind.ModularFull => $"Images/species/{GroupType}-{ImageNum}-{Layer:D3}",
                _ => null
            };
        }

        public static string BuildModularSpeciesPath(string groupType, string imageNum, int layer)
        {
            return $"Images/species/{groupType}-{imageNum}-{layer:D3}";
        }

        public static string BuildEquipmentSpritePath(string itemType, int itemNum, int layer)
        {
            return $"Images/equipment/{itemType}-{itemNum:D4}-{layer}";
        }

        public static string BuildWeaponSpritePath(string itemType, int layer, int variant)
        {
            return $"Images/weapons/{itemType}-{layer}-{variant:D4}";
        }
    }
}
```

**Step 4: Run tests to verify they pass**

Open Unity Editor, run EditMode tests. All 10 should pass.

**Step 5: Commit**

```bash
git add Assets/Scripts/Display/ImageToken.cs Assets/Tests/EditMode/Display/ImageTokenTests.cs
git commit -m "Add ImageToken parser for species/equipment sprite path construction"
```

---

### Task 4: Create ColorManager with Tests

Loads color palettes from JSON and provides color lookup/parsing APIs.

**Files:**
- Create: `Assets/Scripts/Display/ColorManager.cs`
- Create: `Assets/Tests/EditMode/Display/ColorManagerTests.cs`

**Step 1: Write the failing tests**

```csharp
using NUnit.Framework;
using UnityEngine;
using Starquill.Display;

namespace Starquill.Tests.Display
{
    public class ColorManagerTests
    {
        [Test]
        public void ParseHex_SixCharNoHash_ReturnsCorrectColor()
        {
            Color c = ColorManager.ParseHex("FF0000");
            Assert.AreEqual(1f, c.r, 0.01f);
            Assert.AreEqual(0f, c.g, 0.01f);
            Assert.AreEqual(0f, c.b, 0.01f);
        }

        [Test]
        public void ParseHex_WithHash_ReturnsCorrectColor()
        {
            Color c = ColorManager.ParseHex("#00FF00");
            Assert.AreEqual(0f, c.r, 0.01f);
            Assert.AreEqual(1f, c.g, 0.01f);
            Assert.AreEqual(0f, c.b, 0.01f);
        }

        [Test]
        public void ParseHex_White_ReturnsWhite()
        {
            Color c = ColorManager.ParseHex("FFFFFF");
            Assert.AreEqual(Color.white, c);
        }

        [Test]
        public void ParseHex_Empty_ReturnsWhite()
        {
            Color c = ColorManager.ParseHex("");
            Assert.AreEqual(Color.white, c);
        }

        [Test]
        public void LoadPalettes_FromJson_ParsesCorrectly()
        {
            string json = "{\"test_palette\":[\"FF0000\",\"00FF00\",\"0000FF\"]}";
            var manager = new ColorManager();
            manager.LoadFromJson(json);

            Color[] palette = manager.GetPalette("test_palette");
            Assert.AreEqual(3, palette.Length);
            Assert.AreEqual(1f, palette[0].r, 0.01f);  // red
            Assert.AreEqual(1f, palette[1].g, 0.01f);  // green
            Assert.AreEqual(1f, palette[2].b, 0.01f);  // blue
        }

        [Test]
        public void GetPalette_Missing_ReturnsFallback()
        {
            string json = "{\"main\":[\"FFFFFF\"]}";
            var manager = new ColorManager();
            manager.LoadFromJson(json);

            Color[] palette = manager.GetPalette("nonexistent");
            Assert.IsNotNull(palette);
            Assert.Greater(palette.Length, 0);
        }

        [Test]
        public void GetRandomColor_ReturnsColorFromPalette()
        {
            string json = "{\"single\":[\"FF0000\"]}";
            var manager = new ColorManager();
            manager.LoadFromJson(json);

            Color c = manager.GetRandomColor("single");
            Assert.AreEqual(1f, c.r, 0.01f);
        }

        [Test]
        public void ResolveSpeciesColorField_HexArray_ReturnsParsedColors()
        {
            var manager = new ColorManager();
            manager.LoadFromJson("{\"main\":[\"FFFFFF\"]}");

            string[] hexArray = { "FF0000", "00FF00" };
            Color[] colors = manager.ResolveColorField(hexArray);
            Assert.AreEqual(2, colors.Length);
            Assert.AreEqual(1f, colors[0].r, 0.01f);
        }

        [Test]
        public void ResolveSpeciesColorField_PaletteKeyword_ReturnsFromPalette()
        {
            var manager = new ColorManager();
            manager.LoadFromJson("{\"human\":[\"3A1914\",\"45443C\"]}");

            string[] keyword = { "human" };
            Color[] colors = manager.ResolveColorField(keyword);
            Assert.AreEqual(2, colors.Length);
        }
    }
}
```

**Step 2: Run tests to verify they fail**

**Step 3: Write ColorManager implementation**

```csharp
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
            // Unity's JsonUtility can't deserialize Dictionary directly.
            // Parse manually: the JSON is { "name": ["hex",...], ... }
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

            // If the first entry looks like a hex color (6+ chars, all hex digits), treat as hex array
            if (field[0].Length >= 6 && IsHexString(field[0]))
            {
                var colors = new Color[field.Length];
                for (int i = 0; i < field.Length; i++)
                    colors[i] = ParseHex(field[i]);
                return colors;
            }

            // Otherwise treat as palette keyword
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
            // Lightweight JSON parse for { "key": ["val",...], ... } structure
            var result = new Dictionary<string, string[]>();
            json = json.Trim();
            if (!json.StartsWith("{")) return result;

            json = json.Substring(1, json.Length - 2); // strip outer braces
            int i = 0;
            while (i < json.Length)
            {
                // Find key
                int keyStart = json.IndexOf('"', i);
                if (keyStart < 0) break;
                int keyEnd = json.IndexOf('"', keyStart + 1);
                string key = json.Substring(keyStart + 1, keyEnd - keyStart - 1);

                // Find array start
                int arrStart = json.IndexOf('[', keyEnd);
                int arrEnd = json.IndexOf(']', arrStart);
                string arrContent = json.Substring(arrStart + 1, arrEnd - arrStart - 1);

                // Parse array of quoted strings
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
```

**Step 4: Run tests to verify they pass**

All 9 tests should pass.

**Step 5: Commit**

```bash
git add Assets/Scripts/Display/ColorManager.cs Assets/Tests/EditMode/Display/ColorManagerTests.cs
git commit -m "Add ColorManager with palette loading, hex parsing, and color field resolution"
```

---

### Task 5: Create DisplayDataRegistry

Loads and caches all display-related JSON data. This is the data backbone that DisplayBuilder queries.

**Files:**
- Create: `Assets/Scripts/Display/DisplayDataRegistry.cs`

**Step 1: Write DisplayDataRegistry**

This class needs custom JSON parsing since Unity's `JsonUtility` cannot handle the heterogeneous species.json and equipment.json structures (arrays with mixed field types, arrays-as-values). We use `MiniJSON` or a simple hand parser. For simplicity, use Unity's built-in `JsonUtility` with wrapper classes where possible, and manual parsing for the rest.

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Starquill.Display
{
    // Parsed species display data (from species.json)
    public class SpeciesDisplayData
    {
        public string Name;
        public string BackArm, Body, Ears, Eyes, FacialDetail, FacialHair;
        public string FrontArm, Head, Legs, Mouth, Nose;
        public string[] Hair;                    // array of group codes for weighted pick, or single token
        public string[] OtherBodyParts;
        public string[] ItemRestrictions;
        public string[] SkinColor;               // hex array or palette keyword
        public string[] HairColor;
        public string[] EyesColor;
        public string[] FacialDetailColor;
        public SkinVarianceSet[] SkinVarianceSets;
        public float XScale = 1f, YScale = 1f;
    }

    public class SkinVarianceSet
    {
        public string[] HexColors;
        public int[] Indices;                    // layer numbers that get this variance color
    }

    // Parsed equipment display data (from equipment.json)
    public class EquipmentDisplayData
    {
        public string ItemType;                  // e.g. "hd01", "tr03", "w01"
        public string Description;
        public int Amount;                       // number of visual variants
        public int[] LayerCodes;                 // which layers this item renders on
        public int[] LayerColorVariance;         // layers that get independent color
        public int[] HiddenLayers;               // species layers to hide when equipped
        public bool Modular;
    }

    // Modular part count (from speciesModularParts.json)
    public class ModularPartInfo
    {
        public string Type;
        public int Amount;
    }

    public class DisplayDataRegistry
    {
        public Dictionary<string, SpeciesDisplayData> Species { get; private set; } = new();
        public Dictionary<string, EquipmentDisplayData> Equipment { get; private set; } = new();
        public Dictionary<string, int> ModularPartCounts { get; private set; } = new();
        public Dictionary<string, int[]> SpeciesLayerMappings { get; private set; } = new();
        public ColorManager Colors { get; private set; } = new();

        private static DisplayDataRegistry _instance;
        public static DisplayDataRegistry Instance => _instance ??= new DisplayDataRegistry();

        public void LoadAll()
        {
            LoadSpecies();
            LoadEquipment();
            LoadModularParts();
            LoadLayerMappings();
            Colors.LoadFromResources();
        }

        private void LoadSpecies()
        {
            var asset = Resources.Load<TextAsset>("Data/species");
            if (asset == null) { Debug.LogWarning("DisplayDataRegistry: species.json not found"); return; }
            ParseSpeciesJson(asset.text);
        }

        private void LoadEquipment()
        {
            var asset = Resources.Load<TextAsset>("Data/equipment");
            if (asset == null) { Debug.LogWarning("DisplayDataRegistry: equipment.json not found"); return; }
            ParseEquipmentJson(asset.text);
        }

        private void LoadModularParts()
        {
            var asset = Resources.Load<TextAsset>("Data/speciesModularParts");
            if (asset == null) { Debug.LogWarning("DisplayDataRegistry: speciesModularParts.json not found"); return; }
            ParseModularPartsJson(asset.text);
        }

        private void LoadLayerMappings()
        {
            // Hardcoded from Godot's ConfigManager species_layers
            // These define which layers each modular group code renders to
            SpeciesLayerMappings = new Dictionary<string, int[]>
            {
                { "f01", new[] { 84 } },       // eyes
                { "f02", new[] { 86 } },       // nose
                { "f03", new[] { 85 } },       // mouth
                { "f04", new[] { 100 } },      // ears
                { "f05", new[] { 122 } },      // facialHair
                { "f06", new[] { 87 } },       // facialDetail
                { "h01", new[] { 92 } },                // hair (single)
                { "h02", new[] { 92, 128 } },           // hair (front + back)
                { "h03", new[] { 92, 154 } },           // hair (front + far back)
                { "h04", new[] { 92, 128, 154 } },      // hair (front + mid + far back)
                { "d02", new[] { 86 } },       // dwarf nose
                { "d04", new[] { 100 } },      // dwarf ears
                { "d05", new[] { 122 } },      // dwarf facialHair
                { "d06", new[] { 87 } },       // dwarf facialDetail
                { "g02", new[] { 86 } },       // gnome nose
                { "s01", new[] { 82 } },       // skeleton head
                { "s02", new[] { 84 } },       // skeleton eyes
                { "s03", new[] { 86 } },       // skeleton nose
                { "s04", new[] { 87 } },       // skeleton facialDetail
            };
        }

        // --- JSON Parsing ---
        // species.json is an array of objects with mixed types
        // We use a lightweight approach: parse to Dictionary via MiniJSON-style

        private void ParseSpeciesJson(string json)
        {
            Species.Clear();
            var list = SimpleJson.ParseArray(json);
            foreach (var obj in list)
            {
                var data = new SpeciesDisplayData();
                data.Name = obj.GetString("name", "");
                data.BackArm = obj.GetString("backArm", "");
                data.Body = obj.GetString("body", "");
                data.Ears = obj.GetString("ears", "");
                data.Eyes = obj.GetString("eyes", "");
                data.FacialDetail = obj.GetString("facialDetail", "");
                data.FacialHair = obj.GetString("facialHair", "");
                data.FrontArm = obj.GetString("frontArm", "");
                data.Head = obj.GetString("head", "");
                data.Legs = obj.GetString("legs", "");
                data.Mouth = obj.GetString("mouth", "");
                data.Nose = obj.GetString("nose", "");
                data.Hair = obj.GetStringArray("hair");
                data.OtherBodyParts = obj.GetStringArray("otherBodyParts");
                data.ItemRestrictions = obj.GetStringArray("itemRestrictions");
                data.SkinColor = obj.GetStringArray("skin_color");
                data.HairColor = obj.GetStringArray("hair_color");
                data.EyesColor = obj.GetStringArray("eyes_color");
                data.FacialDetailColor = obj.GetStringArray("facialDetail_color");
                data.XScale = obj.GetFloat("x_scale", 1f);
                data.YScale = obj.GetFloat("y_scale", 1f);

                // Parse skinVariance_sets
                var sets = obj.GetArray("skinVariance_sets");
                if (sets != null)
                {
                    data.SkinVarianceSets = new SkinVarianceSet[sets.Count];
                    for (int i = 0; i < sets.Count; i++)
                    {
                        var setObj = sets[i] as Dictionary<string, object>;
                        var vs = new SkinVarianceSet();
                        vs.HexColors = SimpleJson.ToStringArray(setObj, "hex_colors");
                        vs.Indices = SimpleJson.ToIntArray(setObj, "indices");
                        data.SkinVarianceSets[i] = vs;
                    }
                }
                else
                {
                    data.SkinVarianceSets = new SkinVarianceSet[0];
                }

                if (!string.IsNullOrEmpty(data.Name))
                    Species[data.Name] = data;
            }
        }

        private void ParseEquipmentJson(string json)
        {
            Equipment.Clear();
            var list = SimpleJson.ParseArray(json);
            foreach (var obj in list)
            {
                var data = new EquipmentDisplayData();
                data.ItemType = obj.GetString("item_type", "");
                data.Description = obj.GetString("description", "");
                data.Amount = obj.GetInt("amount", 1);
                data.LayerCodes = obj.GetIntArray("layer_codes");
                data.LayerColorVariance = obj.GetIntArray("layer_color_variance");
                data.HiddenLayers = obj.GetIntArray("hidden_layers");
                data.Modular = obj.GetBool("modular", false);

                if (!string.IsNullOrEmpty(data.ItemType))
                    Equipment[data.ItemType] = data;
            }
        }

        private void ParseModularPartsJson(string json)
        {
            ModularPartCounts.Clear();
            var list = SimpleJson.ParseArray(json);
            foreach (var obj in list)
            {
                string type = obj.GetString("type", "");
                int amount = obj.GetInt("amount", 1);
                if (!string.IsNullOrEmpty(type))
                    ModularPartCounts[type] = amount;
            }
        }

        /// <summary>
        /// Pick a random persistent image number for a modular group type.
        /// Returns a zero-padded 4-digit string (e.g., "0043").
        /// </summary>
        public string PickModularImageNum(string groupType, System.Random rng = null)
        {
            if (!ModularPartCounts.TryGetValue(groupType, out int amount))
                amount = 1;
            int num = rng != null ? rng.Next(1, amount + 1) : Random.Range(1, amount + 1);
            return num.ToString("D4");
        }
    }
}
```

**Step 2: Create SimpleJson helper**

We need a lightweight JSON parser for arrays of dictionaries. Create a helper:

```csharp
using System.Collections.Generic;

namespace Starquill.Display
{
    /// <summary>
    /// Minimal JSON parser for arrays of flat objects.
    /// Handles: strings, numbers, booleans, arrays of strings/ints, nested objects.
    /// Uses MiniJSON-style approach (parse to Dictionary/List of object).
    /// </summary>
    public static class SimpleJson
    {
        public static List<Dictionary<string, object>> ParseArray(string json)
        {
            var result = new List<Dictionary<string, object>>();
            var parsed = MiniJSON.Json.Deserialize(json);
            if (parsed is List<object> list)
            {
                foreach (var item in list)
                {
                    if (item is Dictionary<string, object> dict)
                        result.Add(dict);
                }
            }
            return result;
        }

        public static string GetString(this Dictionary<string, object> dict, string key, string defaultValue = "")
        {
            if (dict.TryGetValue(key, out var val) && val != null)
                return val.ToString();
            return defaultValue;
        }

        public static int GetInt(this Dictionary<string, object> dict, string key, int defaultValue = 0)
        {
            if (dict.TryGetValue(key, out var val) && val != null)
            {
                if (val is long l) return (int)l;
                if (val is double d) return (int)d;
                if (int.TryParse(val.ToString(), out int result)) return result;
            }
            return defaultValue;
        }

        public static float GetFloat(this Dictionary<string, object> dict, string key, float defaultValue = 0f)
        {
            if (dict.TryGetValue(key, out var val) && val != null)
            {
                if (val is double d) return (float)d;
                if (val is long l) return l;
                if (float.TryParse(val.ToString(), out float result)) return result;
            }
            return defaultValue;
        }

        public static bool GetBool(this Dictionary<string, object> dict, string key, bool defaultValue = false)
        {
            if (dict.TryGetValue(key, out var val) && val is bool b)
                return b;
            return defaultValue;
        }

        public static string[] GetStringArray(this Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var val))
            {
                if (val is List<object> list)
                {
                    var arr = new string[list.Count];
                    for (int i = 0; i < list.Count; i++)
                        arr[i] = list[i]?.ToString() ?? "";
                    return arr;
                }
                // Single string value (e.g., hair field is sometimes a string)
                if (val is string s && !string.IsNullOrEmpty(s))
                    return new[] { s };
            }
            return new string[0];
        }

        public static int[] GetIntArray(this Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var val) && val is List<object> list)
            {
                var arr = new int[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] is long l) arr[i] = (int)l;
                    else if (list[i] is double d) arr[i] = (int)d;
                }
                return arr;
            }
            return new int[0];
        }

        public static List<object> GetArray(this Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var val) && val is List<object> list)
                return list;
            return null;
        }

        public static string[] ToStringArray(Dictionary<string, object> dict, string key)
        {
            return dict.GetStringArray(key);
        }

        public static int[] ToIntArray(Dictionary<string, object> dict, string key)
        {
            return dict.GetIntArray(key);
        }
    }
}
```

**Step 3: Add MiniJSON**

MiniJSON is a single-file public-domain JSON parser widely used in Unity. Download or create `Assets/Scripts/Display/MiniJSON.cs`:

Search for "MiniJSON unity c#" — it is a single ~300 line file by Calvin Rien (MIT license). Place it at `Assets/Scripts/Display/MiniJSON.cs`. It provides `MiniJSON.Json.Deserialize(string)` returning `Dictionary<string, object>` or `List<object>`.

Alternatively, copy from: https://gist.github.com/darktable/1411710 — but do NOT fetch at runtime. Copy the source file into the project.

**Step 4: Commit**

```bash
git add Assets/Scripts/Display/DisplayDataRegistry.cs Assets/Scripts/Display/SimpleJson.cs Assets/Scripts/Display/MiniJSON.cs
git commit -m "Add DisplayDataRegistry with JSON parsing for species, equipment, and modular parts"
```

---

### Task 6: Create ImageResolver

Sprite loading with caching. Maps sprite paths to loaded Sprite objects.

**Files:**
- Create: `Assets/Scripts/Display/ImageResolver.cs`

**Step 1: Write ImageResolver**

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Starquill.Display
{
    public class ImageResolver
    {
        private readonly Dictionary<string, Sprite> cache = new();
        private int missCount;

        public Sprite Resolve(string spritePath)
        {
            if (string.IsNullOrEmpty(spritePath))
                return null;

            if (cache.TryGetValue(spritePath, out var cached))
                return cached;

            var sprite = Resources.Load<Sprite>(spritePath);
            if (sprite == null)
            {
                missCount++;
                if (missCount <= 10)
                    Debug.LogWarning($"ImageResolver: Sprite not found at '{spritePath}'");
                else if (missCount == 11)
                    Debug.LogWarning("ImageResolver: Suppressing further missing sprite warnings");
            }

            cache[spritePath] = sprite; // cache null too to avoid repeated load attempts
            return sprite;
        }

        public void ClearCache()
        {
            cache.Clear();
            missCount = 0;
        }

        public int CachedCount => cache.Count;
    }
}
```

**Step 2: Commit**

```bash
git add Assets/Scripts/Display/ImageResolver.cs
git commit -m "Add ImageResolver with sprite caching for display pipeline"
```

---

### Task 7: Create DisplayBuilder with Tests

The core 4-stage pipeline. Takes character data and produces sorted DisplayPiece list.

**Files:**
- Create: `Assets/Scripts/Display/DisplayBuilder.cs`
- Create: `Assets/Tests/EditMode/Display/DisplayBuilderTests.cs`

**Step 1: Write the failing tests**

```csharp
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Starquill.Display;

namespace Starquill.Tests.Display
{
    public class DisplayBuilderTests
    {
        private DisplayDataRegistry registry;

        [SetUp]
        public void SetUp()
        {
            registry = new DisplayDataRegistry();
            // Manually populate test data instead of loading from Resources
            registry.Species["test_human"] = new SpeciesDisplayData
            {
                Name = "test_human",
                BackArm = "0001-016",
                Body = "0001-038",
                Head = "0001-082",
                Legs = "0001-037",
                FrontArm = "0001-102",
                Eyes = "f01",
                Nose = "f02",
                Mouth = "f03",
                Ears = "",
                Hair = new[] { "h01" },
                FacialHair = "",
                FacialDetail = "",
                OtherBodyParts = new string[0],
                ItemRestrictions = new string[0],
                SkinColor = new[] { "FFFFFF" },
                HairColor = new[] { "FF0000" },
                EyesColor = new[] { "0000FF" },
                FacialDetailColor = new[] { "FFFFFF" },
                SkinVarianceSets = new SkinVarianceSet[0],
                XScale = 1f,
                YScale = 1f
            };

            registry.ModularPartCounts["f01"] = 10;
            registry.ModularPartCounts["f02"] = 10;
            registry.ModularPartCounts["f03"] = 10;
            registry.ModularPartCounts["h01"] = 10;

            registry.SpeciesLayerMappings["f01"] = new[] { 84 };
            registry.SpeciesLayerMappings["f02"] = new[] { 86 };
            registry.SpeciesLayerMappings["f03"] = new[] { 85 };
            registry.SpeciesLayerMappings["h01"] = new[] { 92 };
        }

        [Test]
        public void BuildSpecies_StaticTokens_CreatePiecesWithCorrectLayers()
        {
            var instance = CreateTestInstance("test_human");
            var builder = new DisplayBuilder(registry);

            var pieces = builder.BuildSpeciesPieces(instance, registry.Species["test_human"]);

            // Should have pieces for: backArm(16), legs(37), body(38), head(82), eyes(84), mouth(85), nose(86), hair(92), frontArm(102)
            Assert.Greater(pieces.Count, 0);
            // Verify sorted ascending
            for (int i = 1; i < pieces.Count; i++)
                Assert.LessOrEqual(pieces[i - 1].Layer, pieces[i].Layer);
        }

        [Test]
        public void BuildSpecies_ModularGroup_ExpandsToLayerMapping()
        {
            var instance = CreateTestInstance("test_human");
            var builder = new DisplayBuilder(registry);

            var pieces = builder.BuildSpeciesPieces(instance, registry.Species["test_human"]);

            // f01 (eyes) should produce piece at layer 84
            Assert.IsTrue(pieces.Any(p => p.Layer == 84), "Should have eyes piece at layer 84");
            // h01 (hair) should produce piece at layer 92
            Assert.IsTrue(pieces.Any(p => p.Layer == 92), "Should have hair piece at layer 92");
        }

        [Test]
        public void BuildSpecies_HairColor_AppliedToHairLayers()
        {
            var instance = CreateTestInstance("test_human");
            instance.HairColor = Color.red;
            var builder = new DisplayBuilder(registry);

            var pieces = builder.BuildSpeciesPieces(instance, registry.Species["test_human"]);

            var hairPiece = pieces.FirstOrDefault(p => p.Layer == 92);
            Assert.IsNotNull(hairPiece);
            Assert.AreEqual(Color.red, hairPiece.TintColor);
        }

        [Test]
        public void BuildSpecies_EyesColor_AppliedToEyeLayers()
        {
            var instance = CreateTestInstance("test_human");
            instance.EyesColor = Color.blue;
            var builder = new DisplayBuilder(registry);

            var pieces = builder.BuildSpeciesPieces(instance, registry.Species["test_human"]);

            var eyesPiece = pieces.FirstOrDefault(p => p.Layer == 84);
            Assert.IsNotNull(eyesPiece);
            Assert.AreEqual(Color.blue, eyesPiece.TintColor);
        }

        [Test]
        public void HiddenLayers_RemovesSpeciesPieces()
        {
            var speciesPieces = new List<DisplayPiece>
            {
                new(16, "path/a"), new(82, "path/b"), new(92, "path/c"), new(128, "path/d")
            };
            var hiddenLayers = new HashSet<int> { 92, 128 }; // hide hair layers

            var filtered = DisplayBuilder.FilterHiddenLayers(speciesPieces, hiddenLayers);

            Assert.AreEqual(2, filtered.Count);
            Assert.IsFalse(filtered.Any(p => p.Layer == 92));
            Assert.IsFalse(filtered.Any(p => p.Layer == 128));
        }

        [Test]
        public void HatDeduplication_LatestWins()
        {
            var equipItems = new List<(string itemType, int itemNum)>
            {
                ("hd01", 5), ("hd03", 10), ("hd02", 7)
            };

            var deduped = DisplayBuilder.DeduplicateHats(equipItems);

            Assert.AreEqual(1, deduped.Count(e => e.itemType.StartsWith("hd") && int.Parse(e.itemType.Substring(2)) <= 8));
            Assert.AreEqual("hd02", deduped.Last(e => e.itemType.StartsWith("hd")).itemType); // last wins
        }

        [Test]
        public void MergeAndSort_CombinesBothSets_SortedByLayer()
        {
            var species = new List<DisplayPiece>
            {
                new(38, "body"), new(16, "arm"), new(82, "head")
            };
            var equipment = new List<DisplayPiece>
            {
                new(130, "hat"), new(24, "gloves")
            };

            var merged = DisplayBuilder.MergeAndSort(species, equipment);

            Assert.AreEqual(5, merged.Count);
            Assert.AreEqual(16, merged[0].Layer);  // arm
            Assert.AreEqual(24, merged[1].Layer);  // gloves
            Assert.AreEqual(38, merged[2].Layer);  // body
            Assert.AreEqual(82, merged[3].Layer);  // head
            Assert.AreEqual(130, merged[4].Layer); // hat
        }

        private SpeciesInstanceData CreateTestInstance(string speciesName)
        {
            var rng = new System.Random(42);
            return new SpeciesInstanceData
            {
                SpeciesName = speciesName,
                SkinColor = Color.white,
                HairColor = Color.red,
                EyesColor = Color.blue,
                FacialDetailColor = Color.white,
                SkinVarianceColors = new Dictionary<int, Color>(),
                ModularImageNums = new Dictionary<string, string>
                {
                    { "f01", "0001" }, { "f02", "0001" }, { "f03", "0001" }, { "h01", "0001" }
                },
                XScale = 1f,
                YScale = 1f
            };
        }
    }
}
```

**Step 2: Run tests to verify they fail**

**Step 3: Write SpeciesInstanceData and DisplayBuilder**

First, create the runtime species instance data class for the display system:

```csharp
// Add to Assets/Scripts/Display/SpeciesInstanceData.cs
using System.Collections.Generic;
using UnityEngine;

namespace Starquill.Display
{
    /// <summary>
    /// Runtime display state for a species instance. Created when a character is instantiated.
    /// Holds persistent color choices and modular image selections.
    /// </summary>
    public class SpeciesInstanceData
    {
        public string SpeciesName;
        public Color SkinColor = Color.white;
        public Color HairColor = Color.white;
        public Color EyesColor = Color.white;
        public Color FacialDetailColor = Color.white;
        public Dictionary<int, Color> SkinVarianceColors = new();   // layer -> variance color
        public Dictionary<string, string> ModularImageNums = new(); // groupType -> "0043"
        public float XScale = 1f, YScale = 1f;

        /// <summary>
        /// Get variance color for a layer, or white if no variance defined.
        /// </summary>
        public Color GetVarianceColor(int layer)
        {
            return SkinVarianceColors.TryGetValue(layer, out var color) ? color : Color.white;
        }

        /// <summary>
        /// Create a SpeciesInstanceData from a SpeciesDisplayData definition, randomizing colors and modular choices.
        /// </summary>
        public static SpeciesInstanceData CreateFrom(SpeciesDisplayData species, DisplayDataRegistry registry, System.Random rng = null)
        {
            var instance = new SpeciesInstanceData();
            instance.SpeciesName = species.Name;
            instance.XScale = species.XScale;
            instance.YScale = species.YScale;

            // Resolve colors from palettes or hex arrays
            var colors = registry.Colors;
            var skinPalette = colors.ResolveColorField(species.SkinColor);
            instance.SkinColor = skinPalette.Length > 0
                ? skinPalette[rng?.Next(skinPalette.Length) ?? Random.Range(0, skinPalette.Length)]
                : Color.white;

            var hairPalette = species.HairColor != null && species.HairColor.Length > 0
                ? colors.ResolveColorField(species.HairColor)
                : colors.GetPalette("main");
            instance.HairColor = hairPalette[rng?.Next(hairPalette.Length) ?? Random.Range(0, hairPalette.Length)];

            var eyesPalette = species.EyesColor != null && species.EyesColor.Length > 0
                ? colors.ResolveColorField(species.EyesColor)
                : colors.GetPalette("main");
            instance.EyesColor = eyesPalette[rng?.Next(eyesPalette.Length) ?? Random.Range(0, eyesPalette.Length)];

            var fdPalette = species.FacialDetailColor != null && species.FacialDetailColor.Length > 0
                ? colors.ResolveColorField(species.FacialDetailColor)
                : colors.GetPalette("main");
            instance.FacialDetailColor = fdPalette[rng?.Next(fdPalette.Length) ?? Random.Range(0, fdPalette.Length)];

            // Skin variance
            if (species.SkinVarianceSets != null)
            {
                foreach (var set in species.SkinVarianceSets)
                {
                    if (set.HexColors == null || set.HexColors.Length == 0) continue;
                    var hex = set.HexColors[rng?.Next(set.HexColors.Length) ?? Random.Range(0, set.HexColors.Length)];
                    var color = ColorManager.ParseHex(hex);
                    foreach (int layer in set.Indices)
                        instance.SkinVarianceColors[layer] = color;
                }
            }

            // Pick modular image numbers for all group-only tokens in this species
            PickModularNums(instance, species, registry, rng);

            return instance;
        }

        private static void PickModularNums(SpeciesInstanceData instance, SpeciesDisplayData species, DisplayDataRegistry registry, System.Random rng)
        {
            // Collect all modular group tokens from species fields
            var fields = new[] { species.Eyes, species.Nose, species.Mouth, species.Ears,
                                 species.FacialHair, species.FacialDetail };
            foreach (var token in fields)
            {
                if (string.IsNullOrEmpty(token)) continue;
                var parsed = ImageToken.Parse(token);
                if (parsed.Kind == ImageTokenKind.ModularGroup && !instance.ModularImageNums.ContainsKey(parsed.GroupType))
                    instance.ModularImageNums[parsed.GroupType] = registry.PickModularImageNum(parsed.GroupType, rng);
            }

            // Hair: pick from array (weighted by modular part amounts)
            if (species.Hair != null && species.Hair.Length > 0)
            {
                string hairChoice;
                if (species.Hair.Length == 1)
                {
                    hairChoice = species.Hair[0];
                }
                else
                {
                    // Weight by amount (more variants = more likely to be picked)
                    int totalWeight = 0;
                    foreach (var h in species.Hair)
                    {
                        registry.ModularPartCounts.TryGetValue(h, out int amt);
                        totalWeight += System.Math.Max(amt, 1);
                    }
                    int roll = rng?.Next(totalWeight) ?? Random.Range(0, totalWeight);
                    hairChoice = species.Hair[0];
                    int cumulative = 0;
                    foreach (var h in species.Hair)
                    {
                        registry.ModularPartCounts.TryGetValue(h, out int amt);
                        cumulative += System.Math.Max(amt, 1);
                        if (roll < cumulative) { hairChoice = h; break; }
                    }
                }

                var parsed = ImageToken.Parse(hairChoice);
                if (parsed.Kind == ImageTokenKind.ModularGroup && !instance.ModularImageNums.ContainsKey(parsed.GroupType))
                    instance.ModularImageNums[parsed.GroupType] = registry.PickModularImageNum(parsed.GroupType, rng);

                instance.ChosenHairGroup = hairChoice;
            }
        }

        public string ChosenHairGroup;  // which hair group code was selected (e.g., "h02")
    }
}
```

Now the DisplayBuilder:

```csharp
// Assets/Scripts/Display/DisplayBuilder.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Starquill.Display
{
    public class DisplayBuilder
    {
        // Field name -> color category for tint resolution
        private static readonly HashSet<string> HairFields = new() { "hair", "facialHair" };
        private static readonly HashSet<string> EyesFields = new() { "eyes" };
        private static readonly HashSet<string> FacialDetailFields = new() { "facialDetail" };

        private readonly DisplayDataRegistry registry;

        public DisplayBuilder(DisplayDataRegistry registry)
        {
            this.registry = registry;
        }

        /// <summary>
        /// Full build pipeline: species pieces + equipment pieces -> filtered, sorted list.
        /// </summary>
        public List<DisplayPiece> Build(
            SpeciesInstanceData speciesInstance,
            SpeciesDisplayData speciesData,
            List<EquipmentDisplayInfo> equippedItems = null)
        {
            var speciesPieces = BuildSpeciesPieces(speciesInstance, speciesData);
            var equipmentPieces = new List<DisplayPiece>();
            var hiddenLayers = new HashSet<int>();

            if (equippedItems != null && equippedItems.Count > 0)
            {
                var deduped = DeduplicateEquipment(equippedItems, speciesData.ItemRestrictions);
                foreach (var item in deduped)
                {
                    var eqData = registry.Equipment.GetValueOrDefault(item.ItemType);
                    if (eqData == null) continue;

                    // Collect hidden layers
                    foreach (int layer in eqData.HiddenLayers)
                        hiddenLayers.Add(layer);

                    // Build equipment pieces
                    BuildEquipmentPieces(item, eqData, equipmentPieces);
                }
            }

            // Filter hidden layers from species pieces
            speciesPieces = FilterHiddenLayers(speciesPieces, hiddenLayers);

            return MergeAndSort(speciesPieces, equipmentPieces);
        }

        /// <summary>
        /// Stage 1: Build species body part pieces.
        /// </summary>
        public List<DisplayPiece> BuildSpeciesPieces(SpeciesInstanceData instance, SpeciesDisplayData species)
        {
            var pieces = new List<DisplayPiece>();

            AddFieldPieces(pieces, "backArm", species.BackArm, instance, instance.SkinColor);
            AddFieldPieces(pieces, "legs", species.Legs, instance, instance.SkinColor);
            AddFieldPieces(pieces, "body", species.Body, instance, instance.SkinColor);
            AddFieldPieces(pieces, "head", species.Head, instance, instance.SkinColor);
            AddFieldPieces(pieces, "ears", species.Ears, instance, instance.SkinColor);
            AddFieldPieces(pieces, "eyes", species.Eyes, instance, instance.EyesColor);
            AddFieldPieces(pieces, "nose", species.Nose, instance, instance.SkinColor);
            AddFieldPieces(pieces, "mouth", species.Mouth, instance, instance.SkinColor);
            AddFieldPieces(pieces, "facialHair", species.FacialHair, instance, instance.HairColor);
            AddFieldPieces(pieces, "facialDetail", species.FacialDetail, instance, instance.FacialDetailColor);
            AddFieldPieces(pieces, "frontArm", species.FrontArm, instance, instance.SkinColor);

            // Hair (uses chosen group from instance)
            if (!string.IsNullOrEmpty(instance.ChosenHairGroup))
                AddFieldPieces(pieces, "hair", instance.ChosenHairGroup, instance, instance.HairColor);

            // Other body parts
            if (species.OtherBodyParts != null)
            {
                foreach (var part in species.OtherBodyParts)
                    AddFieldPieces(pieces, "otherBodyParts", part, instance, instance.SkinColor);
            }

            pieces.Sort((a, b) => a.Layer.CompareTo(b.Layer));
            return pieces;
        }

        private void AddFieldPieces(List<DisplayPiece> pieces, string fieldName, string token,
            SpeciesInstanceData instance, Color baseColor)
        {
            if (string.IsNullOrEmpty(token)) return;

            var parsed = ImageToken.Parse(token);
            switch (parsed.Kind)
            {
                case ImageTokenKind.Static:
                {
                    var tint = CalculateSpeciesTint(fieldName, parsed.Layer, baseColor, instance);
                    pieces.Add(new DisplayPiece(parsed.Layer, parsed.ToSpeciesSpritePath(), tint));
                    break;
                }
                case ImageTokenKind.ModularFull:
                {
                    var tint = CalculateSpeciesTint(fieldName, parsed.Layer, baseColor, instance);
                    pieces.Add(new DisplayPiece(parsed.Layer, parsed.ToSpeciesSpritePath(), tint));
                    break;
                }
                case ImageTokenKind.ModularGroup:
                {
                    if (!registry.SpeciesLayerMappings.TryGetValue(parsed.GroupType, out var layers))
                        break;
                    if (!instance.ModularImageNums.TryGetValue(parsed.GroupType, out var imageNum))
                        break;

                    foreach (int layer in layers)
                    {
                        var tint = CalculateSpeciesTint(fieldName, layer, baseColor, instance);
                        string path = ImageToken.BuildModularSpeciesPath(parsed.GroupType, imageNum, layer);
                        pieces.Add(new DisplayPiece(layer, path, tint));
                    }
                    break;
                }
            }
        }

        private Color CalculateSpeciesTint(string fieldName, int layer, Color baseColor, SpeciesInstanceData instance)
        {
            // Hair and eyes always use their category color directly
            if (HairFields.Contains(fieldName) || EyesFields.Contains(fieldName) || FacialDetailFields.Contains(fieldName))
                return baseColor;

            // For skin-based fields: check variance first, fall back to base skin color
            var variance = instance.GetVarianceColor(layer);
            return variance != Color.white ? variance : baseColor;
        }

        /// <summary>
        /// Stage 2: Build equipment pieces for one item.
        /// </summary>
        private void BuildEquipmentPieces(EquipmentDisplayInfo item, EquipmentDisplayData eqData, List<DisplayPiece> pieces)
        {
            bool isWeapon = item.ItemType.StartsWith("w");

            for (int i = 0; i < eqData.LayerCodes.Length; i++)
            {
                int layer = eqData.LayerCodes[i];
                string path;

                if (isWeapon)
                {
                    int variant = item.LayerVariants != null && i < item.LayerVariants.Length
                        ? item.LayerVariants[i] : item.ItemNum;
                    path = ImageToken.BuildWeaponSpritePath(item.ItemType, layer, variant);
                }
                else
                {
                    path = ImageToken.BuildEquipmentSpritePath(item.ItemType, item.ItemNum, layer);
                }

                // Determine tint color
                Color tint = item.BaseColor;
                if (eqData.LayerColorVariance != null && eqData.LayerColorVariance.Contains(layer))
                {
                    if (item.VarianceColors != null && item.VarianceColors.TryGetValue(layer, out var vc))
                        tint = vc;
                }

                var piece = new DisplayPiece(layer, path, tint);

                // Off-hand weapon transforms
                if (item.IsOffhand && isWeapon)
                {
                    bool isShield = item.ItemType == "w17"; // shields
                    piece.IsOffhandWeapon = true;
                    if (isShield)
                        piece.Offset = new Vector2(42, 0);
                    else
                    {
                        piece.Rotation = -40f;
                        piece.Offset = new Vector2(-21, 15);
                    }
                }

                pieces.Add(piece);
            }
        }

        /// <summary>
        /// Stage 3a: Filter pieces by hidden layers.
        /// </summary>
        public static List<DisplayPiece> FilterHiddenLayers(List<DisplayPiece> pieces, HashSet<int> hiddenLayers)
        {
            if (hiddenLayers == null || hiddenLayers.Count == 0) return pieces;
            return pieces.Where(p => !hiddenLayers.Contains(p.Layer)).ToList();
        }

        /// <summary>
        /// Stage 3b: Deduplicate equipment — latest wins for same item_type, hat set mutual exclusion.
        /// </summary>
        public static List<(string itemType, int itemNum)> DeduplicateHats(List<(string itemType, int itemNum)> items)
        {
            // Find last hat in hd01-hd08 range
            int lastHatIdx = -1;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                var type = items[i].itemType;
                if (type.Length == 4 && type.StartsWith("hd"))
                {
                    if (int.TryParse(type.Substring(2), out int num) && num >= 1 && num <= 8)
                    {
                        if (lastHatIdx < 0) lastHatIdx = i;
                    }
                }
            }

            if (lastHatIdx < 0) return items;

            // Remove all hd01-hd08 except the last one
            return items.Where((item, idx) =>
            {
                var type = item.itemType;
                if (type.Length == 4 && type.StartsWith("hd") && int.TryParse(type.Substring(2), out int num) && num >= 1 && num <= 8)
                    return idx == lastHatIdx;
                return true;
            }).ToList();
        }

        private List<EquipmentDisplayInfo> DeduplicateEquipment(List<EquipmentDisplayInfo> items, string[] itemRestrictions)
        {
            var restrictions = new HashSet<string>(itemRestrictions ?? new string[0]);
            var filtered = items.Where(i => !restrictions.Contains(i.ItemType)).ToList();

            // Latest-wins for same item_type
            var seen = new Dictionary<string, int>();
            for (int i = 0; i < filtered.Count; i++)
                seen[filtered[i].ItemType] = i;
            filtered = filtered.Where((item, idx) => seen[item.ItemType] == idx).ToList();

            // Hat set deduplication (hd01-hd08)
            int lastHatIdx = -1;
            for (int i = filtered.Count - 1; i >= 0; i--)
            {
                var type = filtered[i].ItemType;
                if (type.Length == 4 && type.StartsWith("hd") && int.TryParse(type.Substring(2), out int num) && num >= 1 && num <= 8)
                {
                    if (lastHatIdx < 0) lastHatIdx = i;
                }
            }

            if (lastHatIdx >= 0)
            {
                filtered = filtered.Where((item, idx) =>
                {
                    var type = item.ItemType;
                    if (type.Length == 4 && type.StartsWith("hd") && int.TryParse(type.Substring(2), out int num) && num >= 1 && num <= 8)
                        return idx == lastHatIdx;
                    return true;
                }).ToList();
            }

            return filtered;
        }

        /// <summary>
        /// Stage 4: Merge and sort all pieces by layer ascending.
        /// </summary>
        public static List<DisplayPiece> MergeAndSort(List<DisplayPiece> species, List<DisplayPiece> equipment)
        {
            var merged = new List<DisplayPiece>(species.Count + equipment.Count);
            merged.AddRange(species);
            merged.AddRange(equipment);
            merged.Sort((a, b) => a.Layer.CompareTo(b.Layer));
            return merged;
        }
    }

    /// <summary>
    /// Equipment info needed by the display system. Bridges between gameplay EquipmentInstance and display pipeline.
    /// </summary>
    public class EquipmentDisplayInfo
    {
        public string ItemType;          // e.g., "hd01", "w01"
        public int ItemNum;              // visual variant number
        public Color BaseColor = Color.white;
        public Dictionary<int, Color> VarianceColors;  // layer -> variance color
        public int[] LayerVariants;      // per-layer variant nums (modular weapons only)
        public bool IsOffhand;
    }
}
```

**Step 4: Run tests to verify they pass**

All 7 tests should pass.

**Step 5: Commit**

```bash
git add Assets/Scripts/Display/DisplayBuilder.cs Assets/Scripts/Display/SpeciesInstanceData.cs Assets/Tests/EditMode/Display/DisplayBuilderTests.cs
git commit -m "Add DisplayBuilder with 4-stage pipeline, species instance data, and tests"
```

---

### Task 8: Create Starquill.Display Assembly Definition

Wire up the assembly definition for the Display module.

**Files:**
- Create: `Assets/Scripts/Display/Starquill.Display.asmdef`
- Modify: `Assets/Scripts/Managers/Starquill.Managers.asmdef` — add Display reference
- Modify: `Assets/Tests/EditMode/EditModeTests.asmdef` — add Display reference

**Step 1: Create Display assembly definition**

```json
{
    "name": "Starquill.Display",
    "rootNamespace": "Starquill.Display",
    "references": ["Starquill.Core", "Starquill.Data"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "autoReferenced": true
}
```

**Step 2: Update Managers asmdef to reference Display**

Add `"Starquill.Display"` to the references array in `Assets/Scripts/Managers/Starquill.Managers.asmdef`.

**Step 3: Update EditModeTests asmdef to reference Display**

Add `"Starquill.Display"` to the references array in `Assets/Tests/EditMode/EditModeTests.asmdef`.

**Step 4: Commit**

```bash
git add Assets/Scripts/Display/Starquill.Display.asmdef Assets/Scripts/Managers/Starquill.Managers.asmdef Assets/Tests/EditMode/EditModeTests.asmdef
git commit -m "Add Starquill.Display assembly definition and update references"
```

---

### Task 9: Create CharacterDisplay MonoBehaviour

The RenderTexture compositing renderer. This is the only Unity-specific rendering component.

**Files:**
- Create: `Assets/Scripts/Display/CharacterDisplay.cs`

**Step 1: Write CharacterDisplay**

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Starquill.Display
{
    /// <summary>
    /// Renders a character's DisplayPieces into a RenderTexture via an off-screen compositing camera,
    /// then displays the result in a RawImage with bilinear filtering.
    /// </summary>
    public class CharacterDisplay : MonoBehaviour
    {
        [Header("Compositing Settings")]
        [SerializeField] private int textureSize = 400;     // 2x native (200x200) for quality
        [SerializeField] private int compositingLayer = 31;  // Unity layer for off-screen rendering

        [Header("Output")]
        [SerializeField] private RawImage outputImage;       // UI element that shows the result

        private RenderTexture renderTexture;
        private Camera compositingCamera;
        private readonly List<SpriteRenderer> spritePool = new();
        private ImageResolver imageResolver;
        private Transform compositingRoot;

        public RenderTexture Texture => renderTexture;

        public void Initialize(ImageResolver resolver)
        {
            imageResolver = resolver;
            SetupCompositingCamera();
            SetupRenderTexture();
        }

        /// <summary>
        /// Rebuild the character visual from a list of DisplayPieces.
        /// </summary>
        public void SetPieces(List<DisplayPiece> pieces)
        {
            if (compositingCamera == null || imageResolver == null) return;

            // Ensure enough pooled SpriteRenderers
            while (spritePool.Count < pieces.Count)
                CreatePooledRenderer();

            // Configure each piece
            for (int i = 0; i < pieces.Count; i++)
            {
                var piece = pieces[i];
                var sr = spritePool[i];
                sr.gameObject.SetActive(true);

                sr.sprite = imageResolver.Resolve(piece.SpritePath);
                sr.sortingOrder = piece.Layer;
                sr.color = piece.TintColor;
                sr.flipX = piece.FlipH;

                var t = sr.transform;
                t.localPosition = new Vector3(piece.Offset.x / 100f, piece.Offset.y / 100f, 0);
                t.localScale = new Vector3(piece.Scale.x, piece.Scale.y, 1f);
                t.localRotation = Quaternion.Euler(0, 0, piece.Rotation);
            }

            // Deactivate excess pool members
            for (int i = pieces.Count; i < spritePool.Count; i++)
                spritePool[i].gameObject.SetActive(false);

            // Render to texture
            Composite();
        }

        private void Composite()
        {
            compositingCamera.targetTexture = renderTexture;
            compositingCamera.Render();

            if (outputImage != null)
                outputImage.texture = renderTexture;
        }

        private void SetupCompositingCamera()
        {
            // Create a child object for the compositing camera
            var camObj = new GameObject("CompositingCamera");
            camObj.transform.SetParent(transform);
            camObj.transform.localPosition = new Vector3(0, 0, -10);

            compositingCamera = camObj.AddComponent<Camera>();
            compositingCamera.orthographic = true;
            compositingCamera.orthographicSize = 1f;         // 1 unit = 100 pixels at PPU 100
            compositingCamera.cullingMask = 1 << compositingLayer;
            compositingCamera.clearFlags = CameraClearFlags.SolidColor;
            compositingCamera.backgroundColor = new Color(0, 0, 0, 0); // transparent
            compositingCamera.enabled = false;                // only render manually

            // Create root for sprite renderers
            compositingRoot = new GameObject("CompositingRoot").transform;
            compositingRoot.SetParent(transform);
            compositingRoot.localPosition = Vector3.zero;
        }

        private void SetupRenderTexture()
        {
            renderTexture = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGB32);
            renderTexture.filterMode = FilterMode.Bilinear;
            renderTexture.Create();
        }

        private void CreatePooledRenderer()
        {
            var obj = new GameObject($"Layer_{spritePool.Count}");
            obj.transform.SetParent(compositingRoot);
            obj.transform.localPosition = Vector3.zero;
            obj.layer = compositingLayer;

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Default";
            spritePool.Add(sr);
        }

        private void OnDestroy()
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }
        }

        /// <summary>
        /// Convenience: rebuild from character data in one call.
        /// </summary>
        public void RebuildFromData(
            SpeciesInstanceData speciesInstance,
            SpeciesDisplayData speciesData,
            List<EquipmentDisplayInfo> equipment,
            DisplayBuilder builder)
        {
            var pieces = builder.Build(speciesInstance, speciesData, equipment);
            SetPieces(pieces);
        }
    }
}
```

**Step 2: Commit**

```bash
git add Assets/Scripts/Display/CharacterDisplay.cs
git commit -m "Add CharacterDisplay with RenderTexture compositing and bilinear output"
```

---

### Task 10: Integration Test Scene

Create a minimal test scene that loads one character with species data and renders it via CharacterDisplay.

**Files:**
- Create: `Assets/Scenes/DisplayTest.unity` (via Unity Editor)
- Create: `Assets/Scripts/Display/DisplayTestRunner.cs`

**Step 1: Write DisplayTestRunner**

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Starquill.Display
{
    /// <summary>
    /// Test harness: loads data, creates a random species instance, builds display pieces,
    /// and renders via CharacterDisplay. Attach to a GameObject in the DisplayTest scene.
    /// </summary>
    public class DisplayTestRunner : MonoBehaviour
    {
        [SerializeField] private CharacterDisplay characterDisplay;
        [SerializeField] private RawImage outputImage;

        private void Start()
        {
            // Load all display data
            var registry = DisplayDataRegistry.Instance;
            registry.LoadAll();

            var imageResolver = new ImageResolver();
            characterDisplay.Initialize(imageResolver);

            // Pick first available species
            string speciesName = null;
            foreach (var kvp in registry.Species)
            {
                speciesName = kvp.Key;
                break;
            }

            if (speciesName == null)
            {
                Debug.LogError("DisplayTestRunner: No species found in registry");
                return;
            }

            var speciesData = registry.Species[speciesName];
            var instance = SpeciesInstanceData.CreateFrom(speciesData, registry, new System.Random(42));

            Debug.Log($"DisplayTestRunner: Rendering species '{speciesName}' with {instance.ModularImageNums.Count} modular parts");

            var builder = new DisplayBuilder(registry);
            characterDisplay.RebuildFromData(instance, speciesData, null, builder);

            Debug.Log("DisplayTestRunner: Render complete");
        }
    }
}
```

**Step 2: Create scene in Unity Editor**

1. Open Unity Editor
2. File > New Scene > Basic 2D
3. Add a Canvas (GameObject > UI > Canvas). Set Canvas Scaler to "Scale With Screen Size", reference 1080x1920.
4. Add a RawImage child to Canvas. Size: 400x400, centered.
5. Add an empty GameObject "CharacterRenderer" at root. Add the `CharacterDisplay` component.
6. Wire CharacterDisplay's `outputImage` field to the RawImage.
7. Add an empty GameObject "TestRunner". Add the `DisplayTestRunner` component.
8. Wire TestRunner's `characterDisplay` field and `outputImage` field.
9. Save scene as `Assets/Scenes/DisplayTest.unity`.

**Step 3: Run the scene**

Enter Play Mode. Check console for:
- "DisplayTestRunner: Rendering species 'human' with N modular parts"
- "DisplayTestRunner: Render complete"
- The RawImage should show the composited character (or be transparent if sprites are missing — check for ImageResolver warnings)

**Step 4: Commit**

```bash
git add Assets/Scripts/Display/DisplayTestRunner.cs Assets/Scenes/
git commit -m "Add DisplayTest scene and test runner for visual verification"
```

---

### Task 11: Final Verification and Push

**Step 1: Run all EditMode tests**

Open Unity Editor > Window > General > Test Runner > EditMode > Run All.

All tests should pass:
- ImageTokenTests (10 tests)
- ColorManagerTests (9 tests)
- DisplayBuilderTests (7 tests)
- Plus all existing Sprint 1 tests (~49 tests)

**Step 2: Run DisplayTest scene**

Enter Play Mode on DisplayTest scene. Verify a character renders in the RawImage.

**Step 3: Verify no compilation errors**

```bash
~/Unity/Hub/Editor/6000.3.8f1/Editor/Unity -batchmode -nographics -projectPath /home/keroppi/Development/Starquill -quit -logFile /tmp/starquill-sprint2-compile.txt 2>&1
grep "error CS" /tmp/starquill-sprint2-compile.txt | head -10
```

Should be 0 errors.

**Step 4: Push**

```bash
git push
```

**Step 5: Update sprint review**

Add Sprint 2 summary to `docs/sprint-review.md`.

```bash
git add docs/sprint-review.md
git commit -m "Add Sprint 2 to sprint review document"
git push
```
