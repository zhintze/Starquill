# Sprint 1: Core Loop Proof — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Player can explore, fight waves, draw and tap Verbs, earn gold, equip loot on paper-doll characters.

**Architecture:** Unity 6 LTS, 2D mobile, ScriptableObject-driven data, deterministic tick-based combat, event-driven UI. Pure C# logic separated from MonoBehaviours for testability.

**Tech Stack:** Unity 6 LTS, C#, Unity Test Framework (Edit Mode), TextMeshPro, Unity UI (Canvas)

**Reference docs:**
- `docs/plans/2026-02-12-idle-rpg-clicker-design.md` — Full architecture spec
- `verb-stat-system-design-doc.md` — Verb/stat/combat reference
- `unity-idle-clicker-project-guide.md` — Unity packages and patterns

---

## Phase 0: Environment & Project Setup

### Task 1: Install Unity on Arch Linux

**Files:** None (system setup)

**Step 1: Install Unity Hub via AUR**

```bash
# Install yay if not present (AUR helper)
# If yay is already installed, skip this
sudo pacman -S --needed git base-devel
git clone https://aur.archlinux.org/yay.git /tmp/yay && cd /tmp/yay && makepkg -si

# Install Unity Hub
yay -S unityhub
```

**Step 2: Launch Unity Hub and install Unity 6 LTS**

```bash
unityhub &
```

In Unity Hub:
1. Sign in or create a Unity account (Personal license is free)
2. Go to **Installs** → **Install Editor**
3. Select **Unity 6000.x LTS** (latest LTS release)
4. Check these modules:
   - **Linux Build Support (IL2CPP)**
   - **Android Build Support** (includes SDK, NDK, JDK)
   - **iOS Build Support** (if on Mac, skip on Linux)
   - **WebGL Build Support** (optional, good for testing)
5. Click Install and wait for completion

**Step 3: Verify installation**

```bash
# Unity should now be available. Find the install path:
ls ~/.local/share/unityhub/editors/
# Should show something like: 6000.1.3f1/
```

**Step 4: Commit — no code changes, just verification**

---

### Task 2: Archive Godot Files

**Files:**
- Move: All Godot-specific files → `godot-archive/`
- Keep: `assets/`, `docs/`, `documents/`, `diagrams/`, `*.md` design docs

**Step 1: Create archive directory and move Godot files**

```bash
mkdir -p godot-archive

# Move Godot-specific directories
mv project.godot godot-archive/
mv export_presets.cfg godot-archive/
mv scripts/ godot-archive/scripts/
mv scenes/ godot-archive/scenes/
mv autoload/ godot-archive/autoload/
mv addons/ godot-archive/addons/
mv godot-mcp/ godot-archive/godot-mcp/
mv build/ godot-archive/build/
mv resources/ godot-archive/resources/
mv tests/ godot-archive/tests/
mv themes/ godot-archive/themes/
mv controller/ godot-archive/controller/
mv model/ godot-archive/model/
mv deploy-android.sh godot-archive/
mv config/ godot-archive/config/  # if exists

# Remove Godot cache (not needed in archive)
rm -rf .godot/

# Remove APK files (large binaries)
rm -f Starquill.apk Starquill.apk.idsig

# Keep these at root:
# - assets/ (sprites and data JSONs reusable)
# - docs/, documents/, diagrams/
# - *.md design docs
# - .git, .gitignore, .editorconfig
# - CLAUDE.md, README.md
```

**Step 2: Update .gitignore for Unity**

Add Unity-specific ignores and remove Godot-specific ones. Keep the file clean.

```gitignore
# Unity generated
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]ser[Ss]ettings/
[Mm]emoryCaptures/
[Rr]ecordings/
Asset[Ss]tore[Tt]ools/

# Unity meta files (keep tracked, but ignore OS-specific)
*.pidb
*.unityproj
*.sln
*.suo
*.user
*.userprefs
*.pidb.meta
*.pdb.meta
*.mdb.meta

# Builds
*.apk
*.aab
*.unitypackage
*.app

# OS
.DS_Store
Thumbs.db

# IDE
.idea/
.vs/
.vscode/
*.swp
*.swo

# Dependencies
node_modules/

# Godot (archived)
.godot/
```

**Step 3: Commit**

```bash
git add -A
git commit -m "Archive Godot files and prepare for Unity project"
```

---

### Task 3: Create Unity Project

**Files:** Unity project structure (created by Unity Hub)

**Step 1: Create project from Unity Hub**

1. Open Unity Hub
2. Click **New Project**
3. Settings:
   - Template: **2D (Built-in Render Pipeline)**
   - Project Name: `Starquill` (or use existing directory)
   - Location: `/home/keroppi/Development/Starquill/` — Unity Hub will create project files here
4. Click **Create Project**

> **Important:** If Unity Hub complains the directory isn't empty, create the project in a temp location, then move the Unity folders (`Assets/`, `Packages/`, `ProjectSettings/`) into the Starquill root.

**Step 2: Configure project settings in Unity Editor**

1. **Edit → Project Settings → Player → Other Settings:**
   - Scripting Backend: **IL2CPP**
   - Managed Stripping Level: **High**
   - Target Architecture: **ARM64** (for Android)
   - Company Name: Your name/studio
   - Product Name: Starquill

2. **Edit → Project Settings → Player → Resolution and Presentation:**
   - Default Orientation: **Portrait**

3. **Edit → Project Settings → Editor:**
   - Asset Serialization: **Force Text** (important for version control)

**Step 3: Create project folder structure**

In Unity Editor's Project window, create these folders inside `Assets/`:

```
Assets/
├── Art/
│   ├── Characters/       # Paper doll sprites (copied from assets/images/)
│   ├── Enemies/          # Enemy sprites
│   ├── Equipment/        # Equipment layer sprites
│   ├── UI/               # UI art assets
│   └── Effects/          # VFX sprites
├── Data/
│   ├── Species/          # SpeciesDefinition ScriptableObjects
│   ├── Verbs/            # VerbDefinition ScriptableObjects
│   │   ├── STR/
│   │   ├── DEX/
│   │   ├── CON/
│   │   ├── INT/
│   │   ├── WIS/
│   │   └── CHA/
│   ├── Equipment/        # EquipmentDefinition ScriptableObjects
│   ├── Enemies/          # EnemyDefinition ScriptableObjects
│   ├── LootTables/       # LootTableDefinition ScriptableObjects
│   ├── Quests/           # QuestZone/Quest ScriptableObjects
│   ├── StatusEffects/    # StatusEffectDefinition ScriptableObjects
│   ├── Sets/             # EquipmentSetDefinition ScriptableObjects
│   └── Config/           # EconomyConfig, AdvantageMatrix singletons
├── Prefabs/
│   ├── Characters/
│   ├── Enemies/
│   ├── UI/
│   └── Effects/
├── Scenes/
│   ├── Boot.unity
│   └── Game.unity
├── Scripts/
│   ├── Core/             # Enums, interfaces, base types
│   ├── Data/             # ScriptableObject definitions
│   ├── Combat/           # Combat tick, damage calc, verb pool
│   ├── Characters/       # Character, party, species logic
│   ├── Equipment/        # Equipment, loot, affixes
│   ├── Exploration/      # Exploration state, quest discovery
│   ├── Quests/           # Quest zones, cadence, dialogue
│   ├── Display/          # Paper doll rendering
│   ├── Economy/          # Gold, leveling, offline earnings
│   ├── UI/               # UI controllers and views
│   ├── Managers/         # GameManager, SaveManager
│   └── Util/             # Big number formatting, helpers
├── Tests/
│   └── EditMode/         # Edit mode unit tests
│       ├── Combat/
│       ├── Economy/
│       └── Data/
└── Resources/            # Assets loaded at runtime
```

**Step 4: Set up Unity Test Framework**

1. **Window → Package Manager → Unity Registry**
2. Search for "Test Framework" — it should already be installed
3. Create assembly definition for tests:

Create file `Assets/Tests/EditMode/EditModeTests.asmdef`:
```json
{
    "name": "EditModeTests",
    "rootNamespace": "Starquill.Tests",
    "references": [
        "Starquill.Core",
        "Starquill.Combat",
        "Starquill.Economy"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "optionalUnityReferences": [
        "TestAssemblies"
    ]
}
```

Create file `Assets/Scripts/Core/Starquill.Core.asmdef`:
```json
{
    "name": "Starquill.Core",
    "rootNamespace": "Starquill.Core",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "autoReferenced": true
}
```

Create file `Assets/Scripts/Combat/Starquill.Combat.asmdef`:
```json
{
    "name": "Starquill.Combat",
    "rootNamespace": "Starquill.Combat",
    "references": ["Starquill.Core"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "autoReferenced": true
}
```

Create file `Assets/Scripts/Economy/Starquill.Economy.asmdef`:
```json
{
    "name": "Starquill.Economy",
    "rootNamespace": "Starquill.Economy",
    "references": ["Starquill.Core"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "autoReferenced": true
}
```

**Step 5: Copy sprite assets**

```bash
# Copy existing Starquill character sprites into Unity project
cp -r assets/images/* Assets/Art/Characters/
cp -r assets/data/* Assets/Data/Config/  # JSON reference data
```

**Step 6: Commit**

```bash
git add Assets/ Packages/ ProjectSettings/ .gitignore
git commit -m "Initialize Unity 6 project with folder structure and test framework"
```

---

## Phase 1: Core Data Types

### Task 4: Core Enums and Types

**Files:**
- Create: `Assets/Scripts/Core/StatType.cs`
- Create: `Assets/Scripts/Core/Rarity.cs`
- Create: `Assets/Scripts/Core/TargetMode.cs`
- Create: `Assets/Scripts/Core/VerbCategory.cs`
- Create: `Assets/Scripts/Core/StatusEffectType.cs`
- Create: `Assets/Scripts/Core/EquipmentSlot.cs`
- Create: `Assets/Scripts/Core/Advantage.cs`

**Step 1: Create all core enums**

`Assets/Scripts/Core/StatType.cs`:
```csharp
namespace Starquill.Core
{
    public enum StatType
    {
        STR,
        DEX,
        CON,
        INT,
        WIS,
        CHA
    }

    public static class StatTypeExtensions
    {
        public static VerbCategory GetCategory(this StatType stat)
        {
            return stat switch
            {
                StatType.STR or StatType.DEX or StatType.CON => VerbCategory.Physical,
                StatType.INT or StatType.WIS or StatType.CHA => VerbCategory.Mental,
                _ => VerbCategory.Physical
            };
        }
    }
}
```

`Assets/Scripts/Core/Rarity.cs`:
```csharp
namespace Starquill.Core
{
    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
}
```

`Assets/Scripts/Core/TargetMode.cs`:
```csharp
namespace Starquill.Core
{
    public enum TargetMode
    {
        Single,   // Hits 1 random enemy
        Cleave,   // Hits 2-3 random enemies
        AoE       // Hits all enemies
    }
}
```

`Assets/Scripts/Core/VerbCategory.cs`:
```csharp
namespace Starquill.Core
{
    public enum VerbCategory
    {
        Physical,
        Mental
    }
}
```

`Assets/Scripts/Core/StatusEffectType.cs`:
```csharp
namespace Starquill.Core
{
    public enum StatusEffectType
    {
        None,
        Stagger,   // STR — delays target action
        Bleed,     // DEX — DoT, stacks up to 5
        Weaken,    // CON — target deals -20% damage
        Expose,    // INT — target takes +25% damage
        Reveal,    // WIS — strips and blocks buffs
        Confuse    // CHA — 30% chance target hits ally
    }
}
```

`Assets/Scripts/Core/EquipmentSlot.cs`:
```csharp
namespace Starquill.Core
{
    public enum EquipmentSlot
    {
        Head,
        Torso,
        Arms,
        Legs,
        Feet,
        MainHand,
        OffHand,
        Misc1,
        Misc2,
        Misc3,
        Misc4
    }
}
```

`Assets/Scripts/Core/Advantage.cs`:
```csharp
namespace Starquill.Core
{
    public enum Advantage
    {
        Weak,      // 0.67x or 0.8x
        Neutral,   // 1.0x
        Strong     // 1.2x or 1.5x
    }
}
```

**Step 2: Write test for StatType extension**

`Assets/Tests/EditMode/Data/StatTypeTests.cs`:
```csharp
using NUnit.Framework;
using Starquill.Core;

namespace Starquill.Tests.Data
{
    public class StatTypeTests
    {
        [Test]
        public void STR_IsPhysical()
        {
            Assert.AreEqual(VerbCategory.Physical, StatType.STR.GetCategory());
        }

        [Test]
        public void DEX_IsPhysical()
        {
            Assert.AreEqual(VerbCategory.Physical, StatType.DEX.GetCategory());
        }

        [Test]
        public void CON_IsPhysical()
        {
            Assert.AreEqual(VerbCategory.Physical, StatType.CON.GetCategory());
        }

        [Test]
        public void INT_IsMental()
        {
            Assert.AreEqual(VerbCategory.Mental, StatType.INT.GetCategory());
        }

        [Test]
        public void WIS_IsMental()
        {
            Assert.AreEqual(VerbCategory.Mental, StatType.WIS.GetCategory());
        }

        [Test]
        public void CHA_IsMental()
        {
            Assert.AreEqual(VerbCategory.Mental, StatType.CHA.GetCategory());
        }
    }
}
```

**Step 3: Run tests in Unity**

Open Unity → **Window → General → Test Runner → EditMode → Run All**

Expected: 6 tests pass.

**Step 4: Commit**

```bash
git add Assets/Scripts/Core/ Assets/Tests/
git commit -m "Add core enums: StatType, Rarity, TargetMode, StatusEffectType, EquipmentSlot"
```

---

### Task 5: Stats and Species ScriptableObjects

**Files:**
- Create: `Assets/Scripts/Data/Stats.cs`
- Create: `Assets/Scripts/Data/SpeciesDefinition.cs`
- Create: `Assets/Scripts/Data/SpeciesAbilityDefinition.cs`
- Test: `Assets/Tests/EditMode/Data/StatsTests.cs`

**Step 1: Write Stats class**

`Assets/Scripts/Data/Stats.cs`:
```csharp
using System;
using Starquill.Core;

namespace Starquill.Data
{
    [Serializable]
    public class Stats
    {
        public int STR;
        public int DEX;
        public int CON;
        public int INT;
        public int WIS;
        public int CHA;

        public int GetStat(StatType type)
        {
            return type switch
            {
                StatType.STR => STR,
                StatType.DEX => DEX,
                StatType.CON => CON,
                StatType.INT => INT,
                StatType.WIS => WIS,
                StatType.CHA => CHA,
                _ => 0
            };
        }

        public void SetStat(StatType type, int value)
        {
            switch (type)
            {
                case StatType.STR: STR = value; break;
                case StatType.DEX: DEX = value; break;
                case StatType.CON: CON = value; break;
                case StatType.INT: INT = value; break;
                case StatType.WIS: WIS = value; break;
                case StatType.CHA: CHA = value; break;
            }
        }

        public int Total => STR + DEX + CON + INT + WIS + CHA;

        public StatType HighestStat()
        {
            int max = STR;
            StatType result = StatType.STR;

            if (DEX > max) { max = DEX; result = StatType.DEX; }
            if (CON > max) { max = CON; result = StatType.CON; }
            if (INT > max) { max = INT; result = StatType.INT; }
            if (WIS > max) { max = WIS; result = StatType.WIS; }
            if (CHA > max) { max = CHA; result = StatType.CHA; }

            return result;
        }

        public Stats Clone()
        {
            return new Stats
            {
                STR = this.STR, DEX = this.DEX, CON = this.CON,
                INT = this.INT, WIS = this.WIS, CHA = this.CHA
            };
        }

        public static Stats operator +(Stats a, Stats b)
        {
            return new Stats
            {
                STR = a.STR + b.STR, DEX = a.DEX + b.DEX, CON = a.CON + b.CON,
                INT = a.INT + b.INT, WIS = a.WIS + b.WIS, CHA = a.CHA + b.CHA
            };
        }
    }
}
```

**Step 2: Write Stats tests**

`Assets/Tests/EditMode/Data/StatsTests.cs`:
```csharp
using NUnit.Framework;
using Starquill.Core;
using Starquill.Data;

namespace Starquill.Tests.Data
{
    public class StatsTests
    {
        [Test]
        public void GetStat_ReturnsCorrectValue()
        {
            var stats = new Stats { STR = 12, DEX = 3, CON = 8, INT = 2, WIS = 3, CHA = 2 };
            Assert.AreEqual(12, stats.GetStat(StatType.STR));
            Assert.AreEqual(3, stats.GetStat(StatType.DEX));
            Assert.AreEqual(8, stats.GetStat(StatType.CON));
        }

        [Test]
        public void Total_SumsAllStats()
        {
            var stats = new Stats { STR = 12, DEX = 3, CON = 8, INT = 2, WIS = 3, CHA = 2 };
            Assert.AreEqual(30, stats.Total);
        }

        [Test]
        public void HighestStat_ReturnsCorrectType()
        {
            var stats = new Stats { STR = 12, DEX = 3, CON = 8, INT = 2, WIS = 3, CHA = 2 };
            Assert.AreEqual(StatType.STR, stats.HighestStat());
        }

        [Test]
        public void Addition_CombinesStats()
        {
            var a = new Stats { STR = 10, DEX = 5, CON = 3, INT = 2, WIS = 1, CHA = 1 };
            var b = new Stats { STR = 2, DEX = 0, CON = 5, INT = 0, WIS = 0, CHA = 3 };
            var result = a + b;
            Assert.AreEqual(12, result.STR);
            Assert.AreEqual(5, result.DEX);
            Assert.AreEqual(8, result.CON);
            Assert.AreEqual(4, result.CHA);
        }
    }
}
```

**Step 3: Write SpeciesDefinition**

`Assets/Scripts/Data/SpeciesDefinition.cs`:
```csharp
using UnityEngine;
using Starquill.Core;

namespace Starquill.Data
{
    [CreateAssetMenu(menuName = "Starquill/Species Definition")]
    public class SpeciesDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string speciesId;
        public string displayName;
        public string description;
        public Sprite portrait;

        [Header("Base Stats (must total 30)")]
        public Stats baseStats;

        [Header("Species Ability")]
        public SpeciesAbilityDefinition ability;

        [Header("Visual")]
        public string[] bodyPartIds;
        public Color skinColor = Color.white;
        public Color hairColor = Color.white;
        public float xScale = 1f;
        public float yScale = 1f;

        [Header("Restrictions")]
        public string[] itemRestrictions;
    }
}
```

`Assets/Scripts/Data/SpeciesAbilityDefinition.cs`:
```csharp
using UnityEngine;

namespace Starquill.Data
{
    [CreateAssetMenu(menuName = "Starquill/Species Ability")]
    public class SpeciesAbilityDefinition : ScriptableObject
    {
        public string abilityId;
        public string displayName;
        public string description;
        public Sprite icon;

        [Header("Rank Thresholds (kills needed)")]
        public int[] rankThresholds = { 0, 100, 500, 2000, 10000, 50000 };

        [Header("Effect Per Rank (percentage bonus)")]
        public float[] effectPerRank = { 5f, 7f, 10f, 12f, 15f, 20f };
    }
}
```

**Step 4: Run tests**

Unity Test Runner → EditMode → Run All.
Expected: All Stats tests + StatType tests pass.

**Step 5: Commit**

```bash
git add Assets/Scripts/Data/Stats.cs Assets/Scripts/Data/SpeciesDefinition.cs Assets/Scripts/Data/SpeciesAbilityDefinition.cs Assets/Tests/EditMode/Data/StatsTests.cs
git commit -m "Add Stats class with tests and SpeciesDefinition ScriptableObjects"
```

---

### Task 6: Equipment ScriptableObjects

**Files:**
- Create: `Assets/Scripts/Data/EquipmentDefinition.cs`
- Create: `Assets/Scripts/Data/AffixDefinition.cs`
- Create: `Assets/Scripts/Data/EquipmentSetDefinition.cs`

**Step 1: Write EquipmentDefinition**

`Assets/Scripts/Data/EquipmentDefinition.cs`:
```csharp
using UnityEngine;
using Starquill.Core;

namespace Starquill.Data
{
    [CreateAssetMenu(menuName = "Starquill/Equipment Definition")]
    public class EquipmentDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string itemId;
        public string displayName;
        public string description;
        public Sprite icon;
        public Rarity rarity;

        [Header("Slot")]
        public EquipmentSlot slot;

        [Header("Stats")]
        public Stats statMods;
        public float percentDamageBonus;

        [Header("Affixes")]
        public AffixDefinition[] possibleAffixes;
        public int maxAffixSlots;

        [Header("Visual Layers")]
        public int[] layerCodes;
        public int[] hiddenLayers;
        public int[] layerColorVariance;
        public int variantCount = 1;

        [Header("Set")]
        public EquipmentSetDefinition setMembership;
    }
}
```

`Assets/Scripts/Data/AffixDefinition.cs`:
```csharp
using UnityEngine;
using Starquill.Core;

namespace Starquill.Data
{
    [CreateAssetMenu(menuName = "Starquill/Affix Definition")]
    public class AffixDefinition : ScriptableObject
    {
        public string affixId;
        public string displayName;
        public StatType statModified;
        public float minValue;
        public float maxValue;
        public bool isPercentage;
        public EquipmentSlot[] validSlots;
    }
}
```

`Assets/Scripts/Data/EquipmentSetDefinition.cs`:
```csharp
using UnityEngine;

namespace Starquill.Data
{
    [CreateAssetMenu(menuName = "Starquill/Equipment Set")]
    public class EquipmentSetDefinition : ScriptableObject
    {
        public string setId;
        public string displayName;

        [Header("Set Bonuses")]
        public string twoPieceDescription;
        public Stats twoPieceStatBonus;

        public string threePieceDescription;
        public Stats threePieceStatBonus;

        public string fourPieceDescription;
        public Stats fourPieceStatBonus;
        public float fourPieceSpecialEffect;
    }
}
```

**Step 2: Commit**

```bash
git add Assets/Scripts/Data/EquipmentDefinition.cs Assets/Scripts/Data/AffixDefinition.cs Assets/Scripts/Data/EquipmentSetDefinition.cs
git commit -m "Add Equipment, Affix, and EquipmentSet ScriptableObject definitions"
```

---

### Task 7: Verb and StatusEffect ScriptableObjects

**Files:**
- Create: `Assets/Scripts/Data/VerbDefinition.cs`
- Create: `Assets/Scripts/Data/StatusEffectDefinition.cs`

**Step 1: Write VerbDefinition**

`Assets/Scripts/Data/VerbDefinition.cs`:
```csharp
using UnityEngine;
using Starquill.Core;

namespace Starquill.Data
{
    [CreateAssetMenu(menuName = "Starquill/Verb Definition")]
    public class VerbDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string verbId;
        public string displayName;
        public string description;
        public Sprite icon;

        [Header("Stat Typing")]
        public StatType statType;
        public VerbCategory Category => statType.GetCategory();

        [Header("Damage")]
        public float baseDamage;
        public int hitCount = 1;
        public TargetMode targetMode;

        [Header("Status Effect")]
        public StatusEffectType effect;
        public float effectProcChance = 0.5f;
        public int effectDuration = 2;
        public float effectPotency = 1f;

        [Header("Scaling")]
        public float statScaling = 1f;
        public int levelRequirement;
        public Rarity rarity;

        [Header("Cooldown")]
        public float cooldownTicks = 2f;
        public int verbPoolPriority;

        [Header("Healing (WIS only)")]
        public bool isHealingVerb;
        public float healAmount;
    }
}
```

`Assets/Scripts/Data/StatusEffectDefinition.cs`:
```csharp
using UnityEngine;
using Starquill.Core;

namespace Starquill.Data
{
    [CreateAssetMenu(menuName = "Starquill/Status Effect")]
    public class StatusEffectDefinition : ScriptableObject
    {
        public StatusEffectType effectType;
        public string displayName;
        public string description;
        public Sprite icon;
        public Color effectColor;

        [Header("Stacking Rules")]
        public bool canStack;
        public int maxStacks = 1;
        public bool refreshOnReapply = true;

        [Header("Interaction")]
        public StatusEffectType overrides;
        public StatusEffectType cleansedBy;
        public StatusEffectType reduces;
    }
}
```

**Step 2: Commit**

```bash
git add Assets/Scripts/Data/VerbDefinition.cs Assets/Scripts/Data/StatusEffectDefinition.cs
git commit -m "Add VerbDefinition and StatusEffectDefinition ScriptableObjects"
```

---

## Phase 2: Economy & Math

### Task 8: EconomyConfig and AdvantageMatrix

**Files:**
- Create: `Assets/Scripts/Data/EconomyConfig.cs`
- Create: `Assets/Scripts/Data/AdvantageMatrix.cs`
- Test: `Assets/Tests/EditMode/Data/AdvantageMatrixTests.cs`

**Step 1: Write EconomyConfig**

`Assets/Scripts/Data/EconomyConfig.cs`:
```csharp
using UnityEngine;

namespace Starquill.Data
{
    [CreateAssetMenu(menuName = "Starquill/Economy Config")]
    public class EconomyConfig : ScriptableObject
    {
        [Header("Enemy Scaling")]
        public float baseHP = 50f;
        public float hpGrowthRate = 0.12f;

        [Header("Gold")]
        public float baseGold = 5f;
        public float goldGrowthRate = 0.10f;

        [Header("Upgrade Costs")]
        public float baseCost = 10f;
        public float costGrowthRate = 0.15f;

        [Header("Offline")]
        public float offlineEfficiency = 0.50f;
        public float maxOfflineSeconds = 28800f;
        public float offlineFragmentRate = 0.25f;

        [Header("Verb Draw")]
        public float verbDrawCooldown = 10f;
        public int verbSlotCount = 3;

        [Header("Combat")]
        public float autoAttackDPSFraction = 0.3f;

        [Header("Pity Timers")]
        public int pityUncommon = 50;
        public int pityRare = 200;
        public int pityEpic = 1000;
        public int pityLegendary = 5000;

        [Header("Exploration")]
        public float questRetreatGoldPenalty = 0.50f;
        public float exploreKillsPerMinute = 6f;
        public float questDiscoveryRate = 0.03f;
        public float fragmentDropRate = 0.01f;

        [Header("Prestige (Stub)")]
        public float prestigeMultiplierBase = 1.0f;

        // --- Computed helpers ---

        public float EnemyHP(int questLevel)
        {
            return baseHP * Mathf.Pow(1f + hpGrowthRate, questLevel);
        }

        public float GoldPerKill(int questLevel, float chaBonus = 0f, float prestigeMult = 1f, float boostMult = 1f)
        {
            return baseGold
                * Mathf.Pow(1f + goldGrowthRate, questLevel)
                * (1f + chaBonus)
                * prestigeMult
                * boostMult;
        }

        public float UpgradeCost(int currentLevel)
        {
            return baseCost * Mathf.Pow(1f + costGrowthRate, currentLevel);
        }

        public float OfflineGold(float goldPerSecond, float elapsedSeconds, float prestigeMult = 1f)
        {
            float cappedTime = Mathf.Min(elapsedSeconds, maxOfflineSeconds);
            return goldPerSecond * cappedTime * offlineEfficiency * prestigeMult;
        }
    }
}
```

**Step 2: Write AdvantageMatrix**

`Assets/Scripts/Data/AdvantageMatrix.cs`:
```csharp
using System;
using UnityEngine;
using Starquill.Core;

namespace Starquill.Data
{
    [CreateAssetMenu(menuName = "Starquill/Advantage Matrix")]
    public class AdvantageMatrix : ScriptableObject
    {
        // Hardcoded matrix from verb-stat-system-design-doc.md
        // Row = attacker, Col = defender
        // Order: STR, DEX, CON, INT, WIS, CHA

        private static readonly float[,] DamageMatrix = {
            // Defender: STR   DEX    CON    INT   WIS   CHA
            /* STR */ { 1.0f, 1.5f,  0.67f, 1.2f, 1.2f, 1.2f },
            /* DEX */ { 0.67f, 1.0f, 1.5f,  1.2f, 1.2f, 1.2f },
            /* CON */ { 1.5f, 0.67f, 1.0f,  1.2f, 1.2f, 1.2f },
            /* INT */ { 0.8f, 0.8f,  0.8f,  1.0f, 1.5f, 0.67f },
            /* WIS */ { 0.8f, 0.8f,  0.8f,  0.67f, 1.0f, 1.5f },
            /* CHA */ { 0.8f, 0.8f,  0.8f,  1.5f, 0.67f, 1.0f },
        };

        private static readonly float[,] ProcMatrix = {
            // Defender: STR   DEX    CON    INT   WIS   CHA
            /* STR */ { 0.5f, 0.5f,  0.5f,  0.0f, 0.0f, 0.0f },
            /* DEX */ { 0.5f, 0.5f,  0.5f,  0.0f, 0.0f, 0.0f },
            /* CON */ { 0.5f, 0.5f,  0.5f,  0.0f, 0.0f, 0.0f },
            /* INT */ { 1.0f, 1.0f,  1.0f,  0.5f, 0.5f, 0.5f },
            /* WIS */ { 1.0f, 1.0f,  1.0f,  0.5f, 0.5f, 0.5f },
            /* CHA */ { 1.0f, 1.0f,  1.0f,  0.5f, 0.5f, 0.5f },
        };

        public MatchupResult GetMatchup(StatType attacker, StatType defender)
        {
            int a = (int)attacker;
            int d = (int)defender;

            float dmgMult = DamageMatrix[a, d];
            float procMod = ProcMatrix[a, d];

            Advantage advantage;
            if (dmgMult >= 1.4f) advantage = Advantage.Strong;
            else if (dmgMult <= 0.7f) advantage = Advantage.Weak;
            else advantage = Advantage.Neutral;

            return new MatchupResult
            {
                damageMultiplier = dmgMult,
                statusProcModifier = procMod,
                advantage = advantage
            };
        }
    }

    [Serializable]
    public struct MatchupResult
    {
        public float damageMultiplier;
        public float statusProcModifier;
        public Advantage advantage;
    }
}
```

**Step 3: Write AdvantageMatrix tests**

`Assets/Tests/EditMode/Data/AdvantageMatrixTests.cs`:
```csharp
using NUnit.Framework;
using Starquill.Core;
using Starquill.Data;
using UnityEngine;

namespace Starquill.Tests.Data
{
    public class AdvantageMatrixTests
    {
        private AdvantageMatrix matrix;

        [SetUp]
        public void SetUp()
        {
            matrix = ScriptableObject.CreateInstance<AdvantageMatrix>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(matrix);
        }

        // Physical triangle
        [Test] public void STR_Beats_DEX() => AssertStrong(StatType.STR, StatType.DEX, 1.5f);
        [Test] public void DEX_Beats_CON() => AssertStrong(StatType.DEX, StatType.CON, 1.5f);
        [Test] public void CON_Beats_STR() => AssertStrong(StatType.CON, StatType.STR, 1.5f);

        [Test] public void DEX_Weak_To_STR() => AssertWeak(StatType.DEX, StatType.STR, 0.67f);
        [Test] public void CON_Weak_To_DEX() => AssertWeak(StatType.CON, StatType.DEX, 0.67f);
        [Test] public void STR_Weak_To_CON() => AssertWeak(StatType.STR, StatType.CON, 0.67f);

        // Mental triangle
        [Test] public void INT_Beats_WIS() => AssertStrong(StatType.INT, StatType.WIS, 1.5f);
        [Test] public void WIS_Beats_CHA() => AssertStrong(StatType.WIS, StatType.CHA, 1.5f);
        [Test] public void CHA_Beats_INT() => AssertStrong(StatType.CHA, StatType.INT, 1.5f);

        // Cross-triangle
        [Test]
        public void Physical_To_Mental_Deals_1_2x()
        {
            var result = matrix.GetMatchup(StatType.STR, StatType.INT);
            Assert.AreEqual(1.2f, result.damageMultiplier, 0.01f);
            Assert.AreEqual(0.0f, result.statusProcModifier, 0.01f); // No status procs
        }

        [Test]
        public void Mental_To_Physical_Deals_0_8x_With_Guaranteed_Proc()
        {
            var result = matrix.GetMatchup(StatType.INT, StatType.STR);
            Assert.AreEqual(0.8f, result.damageMultiplier, 0.01f);
            Assert.AreEqual(1.0f, result.statusProcModifier, 0.01f); // Always procs
        }

        // Neutral
        [Test]
        public void Same_Type_Is_Neutral()
        {
            var result = matrix.GetMatchup(StatType.STR, StatType.STR);
            Assert.AreEqual(1.0f, result.damageMultiplier, 0.01f);
            Assert.AreEqual(Advantage.Neutral, result.advantage);
        }

        private void AssertStrong(StatType atk, StatType def, float expectedMult)
        {
            var result = matrix.GetMatchup(atk, def);
            Assert.AreEqual(expectedMult, result.damageMultiplier, 0.01f);
            Assert.AreEqual(Advantage.Strong, result.advantage);
        }

        private void AssertWeak(StatType atk, StatType def, float expectedMult)
        {
            var result = matrix.GetMatchup(atk, def);
            Assert.AreEqual(expectedMult, result.damageMultiplier, 0.01f);
            Assert.AreEqual(Advantage.Weak, result.advantage);
        }
    }
}
```

**Step 4: Run tests**

Unity Test Runner → EditMode → Run All.
Expected: All advantage matrix tests pass (14 tests).

**Step 5: Commit**

```bash
git add Assets/Scripts/Data/EconomyConfig.cs Assets/Scripts/Data/AdvantageMatrix.cs Assets/Tests/EditMode/Data/AdvantageMatrixTests.cs
git commit -m "Add EconomyConfig and AdvantageMatrix with full test coverage"
```

---

### Task 9: DamageCalculator

**Files:**
- Create: `Assets/Scripts/Combat/DamageCalculator.cs`
- Test: `Assets/Tests/EditMode/Combat/DamageCalculatorTests.cs`

**Step 1: Write DamageCalculator**

`Assets/Scripts/Combat/DamageCalculator.cs`:
```csharp
using Starquill.Core;
using Starquill.Data;

namespace Starquill.Combat
{
    public static class DamageCalculator
    {
        /// <summary>
        /// Calculate final damage for a verb hit.
        /// Formula: baseDamage × (1 + charStat × statScaling × 0.1) × advantageMult × exposeMult × equipBonus × prestigeMult × boostMult
        /// </summary>
        public static float Calculate(
            float baseDamage,
            float statScaling,
            int characterStat,
            float advantageMultiplier,
            bool targetIsExposed = false,
            float equipmentDamageBonus = 0f,
            float prestigeMultiplier = 1f,
            float boostMultiplier = 1f)
        {
            float statMult = 1f + characterStat * statScaling * 0.1f;
            float exposeMult = targetIsExposed ? 1.25f : 1f;
            float equipMult = 1f + equipmentDamageBonus;

            return baseDamage * statMult * advantageMultiplier * exposeMult * equipMult * prestigeMultiplier * boostMultiplier;
        }

        /// <summary>
        /// Get the stat proc modifier for a character's stat value.
        /// 8+ = 1.0, 5-7 = 0.75, 3-4 = 0.5, 1-2 = 0.25
        /// </summary>
        public static float GetStatProcModifier(int statValue)
        {
            if (statValue >= 8) return 1.0f;
            if (statValue >= 5) return 0.75f;
            if (statValue >= 3) return 0.5f;
            return 0.25f;
        }

        /// <summary>
        /// Get the stat scaling modifier for a character's stat value.
        /// 8+ = 1.0, 5-7 = 1.0, 3-4 = 0.75, 1-2 = 0.5
        /// </summary>
        public static float GetStatScalingModifier(int statValue)
        {
            if (statValue >= 5) return 1.0f;
            if (statValue >= 3) return 0.75f;
            return 0.5f;
        }
    }
}
```

**Step 2: Write DamageCalculator tests**

`Assets/Tests/EditMode/Combat/DamageCalculatorTests.cs`:
```csharp
using NUnit.Framework;
using Starquill.Combat;

namespace Starquill.Tests.Combat
{
    public class DamageCalculatorTests
    {
        [Test]
        public void BasicDamage_NoModifiers()
        {
            // baseDamage 55, statScaling 1.0, charStat 12 (Brute STR)
            // = 55 × (1 + 12 × 1.0 × 0.1) = 55 × 2.2 = 121
            float result = DamageCalculator.Calculate(55f, 1.0f, 12, 1.0f);
            Assert.AreEqual(121f, result, 0.1f);
        }

        [Test]
        public void LowStat_ReducesDamage()
        {
            // Same verb on Mage (STR = 2)
            // = 55 × (1 + 2 × 1.0 × 0.1) = 55 × 1.2 = 66
            float result = DamageCalculator.Calculate(55f, 1.0f, 2, 1.0f);
            Assert.AreEqual(66f, result, 0.1f);
        }

        [Test]
        public void Advantage_Multiplies_1_5x()
        {
            float result = DamageCalculator.Calculate(55f, 1.0f, 12, 1.5f);
            Assert.AreEqual(181.5f, result, 0.1f);
        }

        [Test]
        public void Disadvantage_Multiplies_0_67x()
        {
            float result = DamageCalculator.Calculate(55f, 1.0f, 12, 0.67f);
            Assert.AreEqual(81.07f, result, 0.1f);
        }

        [Test]
        public void Expose_Adds_25_Percent()
        {
            float normal = DamageCalculator.Calculate(100f, 1.0f, 10, 1.0f);
            float exposed = DamageCalculator.Calculate(100f, 1.0f, 10, 1.0f, targetIsExposed: true);
            Assert.AreEqual(normal * 1.25f, exposed, 0.1f);
        }

        [Test]
        public void FullCombo_Advantage_Plus_Expose()
        {
            // base 55, stat 12, advantage 1.5, exposed
            // = 55 × 2.2 × 1.5 × 1.25 = 226.875
            float result = DamageCalculator.Calculate(55f, 1.0f, 12, 1.5f, targetIsExposed: true);
            Assert.AreEqual(226.875f, result, 0.1f);
        }

        // Stat proc modifiers
        [Test] public void StatProcMod_High() => Assert.AreEqual(1.0f, DamageCalculator.GetStatProcModifier(8));
        [Test] public void StatProcMod_Moderate() => Assert.AreEqual(0.75f, DamageCalculator.GetStatProcModifier(6));
        [Test] public void StatProcMod_Weak() => Assert.AreEqual(0.5f, DamageCalculator.GetStatProcModifier(4));
        [Test] public void StatProcMod_Dump() => Assert.AreEqual(0.25f, DamageCalculator.GetStatProcModifier(1));
    }
}
```

**Step 3: Run tests**

Unity Test Runner → EditMode → Run All.
Expected: 10 DamageCalculator tests pass.

**Step 4: Commit**

```bash
git add Assets/Scripts/Combat/DamageCalculator.cs Assets/Tests/EditMode/Combat/DamageCalculatorTests.cs
git commit -m "Add DamageCalculator with full damage formula and stat modifier tests"
```

---

### Task 10: EconomyConfig Tests

**Files:**
- Test: `Assets/Tests/EditMode/Economy/EconomyConfigTests.cs`

**Step 1: Write economy formula tests**

`Assets/Tests/EditMode/Economy/EconomyConfigTests.cs`:
```csharp
using NUnit.Framework;
using Starquill.Data;
using UnityEngine;

namespace Starquill.Tests.Economy
{
    public class EconomyConfigTests
    {
        private EconomyConfig config;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<EconomyConfig>();
            // Defaults match the design doc
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
        }

        [Test]
        public void EnemyHP_Level1_Is50()
        {
            Assert.AreEqual(50f, config.EnemyHP(0), 1f);
        }

        [Test]
        public void EnemyHP_Level10_Scales()
        {
            float hp = config.EnemyHP(10);
            Assert.AreEqual(155f, hp, 5f); // 50 × 1.12^10 ≈ 155
        }

        [Test]
        public void EnemyHP_Level100_BigNumbers()
        {
            float hp = config.EnemyHP(100);
            Assert.Greater(hp, 1_000_000f); // Should be in millions
        }

        [Test]
        public void GoldPerKill_Level1_Is5()
        {
            Assert.AreEqual(5f, config.GoldPerKill(0), 0.1f);
        }

        [Test]
        public void GoldPerKill_WithChaBonus()
        {
            float base_ = config.GoldPerKill(10);
            float boosted = config.GoldPerKill(10, chaBonus: 0.2f);
            Assert.AreEqual(base_ * 1.2f, boosted, 0.1f);
        }

        [Test]
        public void UpgradeCost_GrowsFasterThanGold()
        {
            // costGrowthRate (0.15) > goldGrowthRate (0.10)
            float gold50 = config.GoldPerKill(50);
            float cost50 = config.UpgradeCost(50);
            float gold10 = config.GoldPerKill(10);
            float cost10 = config.UpgradeCost(10);

            float goldRatio = gold50 / gold10;
            float costRatio = cost50 / cost10;
            Assert.Greater(costRatio, goldRatio);
        }

        [Test]
        public void OfflineGold_CapsAt8Hours()
        {
            float uncapped = config.OfflineGold(100f, 100000f); // way over 8 hours
            float capped = config.OfflineGold(100f, 28800f);     // exactly 8 hours
            Assert.AreEqual(capped, uncapped, 0.1f);
        }

        [Test]
        public void OfflineGold_50PercentEfficiency()
        {
            float offline = config.OfflineGold(100f, 3600f); // 1 hour
            float expected = 100f * 3600f * 0.5f; // 50% efficiency
            Assert.AreEqual(expected, offline, 0.1f);
        }
    }
}
```

**Step 2: Run tests**

Unity Test Runner → EditMode → Run All.
Expected: 8 economy tests pass.

**Step 3: Commit**

```bash
git add Assets/Tests/EditMode/Economy/EconomyConfigTests.cs
git commit -m "Add EconomyConfig formula tests validating HP/gold/cost scaling curves"
```

---

## Phase 3: Combat System

### Task 11: VerbPool — Draw, Rotation, Cooldown

**Files:**
- Create: `Assets/Scripts/Combat/VerbPool.cs`
- Create: `Assets/Scripts/Combat/DrawnVerb.cs`
- Test: `Assets/Tests/EditMode/Combat/VerbPoolTests.cs`

**Step 1: Write DrawnVerb**

`Assets/Scripts/Combat/DrawnVerb.cs`:
```csharp
using Starquill.Data;

namespace Starquill.Combat
{
    public class DrawnVerb
    {
        public VerbDefinition Verb { get; }
        public int OwnerIndex { get; } // Which party member owns this verb
        public float DrawTime { get; set; }
        public float CooldownRemaining { get; set; }

        public bool IsOnCooldown => CooldownRemaining > 0;

        public DrawnVerb(VerbDefinition verb, int ownerIndex, float drawTime)
        {
            Verb = verb;
            OwnerIndex = ownerIndex;
            DrawTime = drawTime;
            CooldownRemaining = 0;
        }

        public void StartCooldown()
        {
            CooldownRemaining = Verb.cooldownTicks;
        }

        public void TickCooldown()
        {
            if (CooldownRemaining > 0)
                CooldownRemaining -= 1f;
        }
    }
}
```

**Step 2: Write VerbPool**

`Assets/Scripts/Combat/VerbPool.cs`:
```csharp
using System.Collections.Generic;
using System.Linq;
using Starquill.Data;

namespace Starquill.Combat
{
    /// <summary>
    /// Manages the shared verb pool from all party members.
    /// Handles random drawing, rotation, and cooldowns.
    /// </summary>
    public class VerbPool
    {
        public struct PoolEntry
        {
            public VerbDefinition verb;
            public int ownerIndex;
        }

        private readonly List<PoolEntry> allVerbs = new();
        private readonly List<DrawnVerb> drawnSlots = new();
        private readonly List<DrawnVerb> onCooldown = new();
        private readonly int maxSlots;
        private readonly float rotationTime;
        private readonly System.Random rng;

        public IReadOnlyList<DrawnVerb> DrawnSlots => drawnSlots;
        public int AvailableCount => allVerbs.Count - onCooldown.Count;

        public VerbPool(int maxSlots, float rotationTime, int? seed = null)
        {
            this.maxSlots = maxSlots;
            this.rotationTime = rotationTime;
            this.rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }

        public void AddVerbs(int ownerIndex, IEnumerable<VerbDefinition> verbs)
        {
            foreach (var verb in verbs)
            {
                allVerbs.Add(new PoolEntry { verb = verb, ownerIndex = ownerIndex });
            }
        }

        public void Clear()
        {
            allVerbs.Clear();
            drawnSlots.Clear();
            onCooldown.Clear();
        }

        /// <summary>
        /// Fill empty slots with random draws from available pool.
        /// </summary>
        public void FillSlots(float currentTime)
        {
            while (drawnSlots.Count < maxSlots)
            {
                var drawn = DrawRandom(currentTime);
                if (drawn == null) break; // No available verbs
                drawnSlots.Add(drawn);
            }
        }

        /// <summary>
        /// Rotate out verbs that have been sitting untapped for too long.
        /// Returns rotated-out verbs (they go back to pool, NOT on cooldown).
        /// </summary>
        public List<DrawnVerb> RotateStaleVerbs(float currentTime)
        {
            var rotated = new List<DrawnVerb>();
            for (int i = drawnSlots.Count - 1; i >= 0; i--)
            {
                if (currentTime - drawnSlots[i].DrawTime >= rotationTime)
                {
                    rotated.Add(drawnSlots[i]);
                    drawnSlots.RemoveAt(i);
                }
            }
            return rotated;
        }

        /// <summary>
        /// Player taps a drawn verb. Remove from slots, put on cooldown.
        /// </summary>
        public DrawnVerb ActivateVerb(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= drawnSlots.Count) return null;

            var verb = drawnSlots[slotIndex];
            drawnSlots.RemoveAt(slotIndex);
            verb.StartCooldown();
            onCooldown.Add(verb);
            return verb;
        }

        /// <summary>
        /// Tick all cooldowns. Remove verbs that come off cooldown.
        /// </summary>
        public void TickCooldowns()
        {
            for (int i = onCooldown.Count - 1; i >= 0; i--)
            {
                onCooldown[i].TickCooldown();
                if (!onCooldown[i].IsOnCooldown)
                {
                    onCooldown.RemoveAt(i);
                }
            }
        }

        private DrawnVerb DrawRandom(float currentTime)
        {
            // Get verb IDs currently drawn or on cooldown
            var unavailable = new HashSet<string>();
            foreach (var d in drawnSlots) unavailable.Add(d.Verb.verbId);
            foreach (var c in onCooldown) unavailable.Add(c.Verb.verbId);

            var available = allVerbs
                .Where(e => !unavailable.Contains(e.verb.verbId))
                .ToList();

            if (available.Count == 0) return null;

            var pick = available[rng.Next(available.Count)];
            return new DrawnVerb(pick.verb, pick.ownerIndex, currentTime);
        }
    }
}
```

**Step 3: Write VerbPool tests**

`Assets/Tests/EditMode/Combat/VerbPoolTests.cs`:
```csharp
using NUnit.Framework;
using Starquill.Combat;
using Starquill.Core;
using Starquill.Data;
using UnityEngine;

namespace Starquill.Tests.Combat
{
    public class VerbPoolTests
    {
        private VerbPool pool;

        private VerbDefinition CreateVerb(string id, StatType stat, float cooldown = 2f)
        {
            var verb = ScriptableObject.CreateInstance<VerbDefinition>();
            verb.verbId = id;
            verb.displayName = id;
            verb.statType = stat;
            verb.baseDamage = 50;
            verb.cooldownTicks = cooldown;
            return verb;
        }

        [SetUp]
        public void SetUp()
        {
            pool = new VerbPool(3, 10f, seed: 42);
        }

        [Test]
        public void FillSlots_FillsUpToMax()
        {
            pool.AddVerbs(0, new[] { CreateVerb("v1", StatType.STR), CreateVerb("v2", StatType.DEX) });
            pool.AddVerbs(1, new[] { CreateVerb("v3", StatType.INT), CreateVerb("v4", StatType.WIS) });

            pool.FillSlots(0f);
            Assert.AreEqual(3, pool.DrawnSlots.Count);
        }

        [Test]
        public void FillSlots_DoesNotExceedAvailable()
        {
            pool.AddVerbs(0, new[] { CreateVerb("v1", StatType.STR) });

            pool.FillSlots(0f);
            Assert.AreEqual(1, pool.DrawnSlots.Count);
        }

        [Test]
        public void ActivateVerb_PutsOnCooldown()
        {
            pool.AddVerbs(0, new[] {
                CreateVerb("v1", StatType.STR),
                CreateVerb("v2", StatType.DEX),
                CreateVerb("v3", StatType.INT),
                CreateVerb("v4", StatType.WIS)
            });

            pool.FillSlots(0f);
            var activated = pool.ActivateVerb(0);

            Assert.IsNotNull(activated);
            Assert.IsTrue(activated.IsOnCooldown);
            Assert.AreEqual(2, pool.DrawnSlots.Count); // One removed
        }

        [Test]
        public void RotateStaleVerbs_RemovesAfterTimeout()
        {
            pool.AddVerbs(0, new[] {
                CreateVerb("v1", StatType.STR),
                CreateVerb("v2", StatType.DEX),
                CreateVerb("v3", StatType.INT),
                CreateVerb("v4", StatType.WIS)
            });

            pool.FillSlots(0f); // Drawn at time 0
            var rotated = pool.RotateStaleVerbs(10f); // 10 seconds later

            Assert.AreEqual(3, rotated.Count); // All 3 rotated out
            Assert.AreEqual(0, pool.DrawnSlots.Count);
        }

        [Test]
        public void RotatedVerbs_AreNotOnCooldown()
        {
            pool.AddVerbs(0, new[] {
                CreateVerb("v1", StatType.STR),
                CreateVerb("v2", StatType.DEX),
                CreateVerb("v3", StatType.INT),
                CreateVerb("v4", StatType.WIS)
            });

            pool.FillSlots(0f);
            var rotated = pool.RotateStaleVerbs(10f);

            foreach (var v in rotated)
                Assert.IsFalse(v.IsOnCooldown);
        }

        [Test]
        public void TickCooldowns_EventuallyFreesVerbs()
        {
            var verb = CreateVerb("v1", StatType.STR, cooldown: 2f);
            pool.AddVerbs(0, new[] { verb, CreateVerb("v2", StatType.DEX) });

            pool.FillSlots(0f);
            pool.ActivateVerb(0); // v1 on cooldown (2 ticks)

            pool.TickCooldowns(); // tick 1
            pool.TickCooldowns(); // tick 2 — should be off cooldown

            // Verb should be drawable again
            pool.FillSlots(0f);
            Assert.AreEqual(2, pool.DrawnSlots.Count);
        }
    }
}
```

**Step 4: Run tests — verify all pass**

**Step 5: Commit**

```bash
git add Assets/Scripts/Combat/VerbPool.cs Assets/Scripts/Combat/DrawnVerb.cs Assets/Tests/EditMode/Combat/VerbPoolTests.cs
git commit -m "Add VerbPool with draw/rotate/cooldown mechanics and tests"
```

---

### Task 12: CombatTickProcessor

**Files:**
- Create: `Assets/Scripts/Combat/CombatTickProcessor.cs`
- Create: `Assets/Scripts/Combat/EnemyState.cs`
- Create: `Assets/Scripts/Combat/CombatTickResult.cs`

**Step 1: Write EnemyState**

`Assets/Scripts/Combat/EnemyState.cs`:
```csharp
using System.Collections.Generic;
using Starquill.Core;

namespace Starquill.Combat
{
    public class EnemyState
    {
        public string Id { get; }
        public StatType StatType { get; }
        public float MaxHP { get; }
        public float CurrentHP { get; set; }
        public bool IsAlive => CurrentHP > 0;
        public float DamagePerTick { get; set; }

        private readonly Dictionary<StatusEffectType, int> activeEffects = new();

        public EnemyState(string id, StatType statType, float maxHP, float damagePerTick)
        {
            Id = id;
            StatType = statType;
            MaxHP = maxHP;
            CurrentHP = maxHP;
            DamagePerTick = damagePerTick;
        }

        public void TakeDamage(float amount)
        {
            if (HasStatus(StatusEffectType.Expose))
                amount *= 1.25f;
            CurrentHP -= amount;
            if (CurrentHP < 0) CurrentHP = 0;
        }

        public void ApplyStatus(StatusEffectType type, int duration)
        {
            if (type == StatusEffectType.None) return;

            // Confuse replaces Weaken
            if (type == StatusEffectType.Confuse)
                activeEffects.Remove(StatusEffectType.Weaken);

            // Reveal cleanses Confuse
            if (type == StatusEffectType.Reveal)
                activeEffects.Remove(StatusEffectType.Confuse);

            // Bleed stacks are handled separately (simplified: refresh duration)
            activeEffects[type] = duration;
        }

        public bool HasStatus(StatusEffectType type)
        {
            return activeEffects.ContainsKey(type) && activeEffects[type] > 0;
        }

        public void TickStatuses()
        {
            var keys = new List<StatusEffectType>(activeEffects.Keys);
            foreach (var key in keys)
            {
                activeEffects[key]--;
                if (activeEffects[key] <= 0)
                    activeEffects.Remove(key);
            }
        }

        public float GetDamageOutput()
        {
            float dmg = DamagePerTick;
            if (HasStatus(StatusEffectType.Weaken))
                dmg *= 0.8f; // -20%
            if (HasStatus(StatusEffectType.Stagger))
                dmg = 0; // Skip action
            return dmg;
        }
    }
}
```

`Assets/Scripts/Combat/CombatTickResult.cs`:
```csharp
namespace Starquill.Combat
{
    public class CombatTickResult
    {
        public float TotalDamageDealt;
        public float TotalDamageReceived;
        public int EnemiesKilled;
        public int StatusProcs;
        public int AdvantageHits;
        public int DisadvantageHits;
        public float GoldEarned;
        public bool WaveCleared;
    }
}
```

**Step 2: Write CombatTickProcessor**

`Assets/Scripts/Combat/CombatTickProcessor.cs`:
```csharp
using System.Collections.Generic;
using System.Linq;
using Starquill.Core;
using Starquill.Data;

namespace Starquill.Combat
{
    public class CombatTickProcessor
    {
        private readonly AdvantageMatrix advantageMatrix;
        private readonly EconomyConfig economyConfig;
        private readonly System.Random rng;

        public CombatTickProcessor(AdvantageMatrix matrix, EconomyConfig config, int? seed = null)
        {
            advantageMatrix = matrix;
            economyConfig = config;
            rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }

        /// <summary>
        /// Process one combat tick. Called once per second.
        /// </summary>
        public CombatTickResult ProcessTick(
            List<EnemyState> enemies,
            Stats[] partyStats,
            DrawnVerb activatedVerb,
            int questLevel,
            float prestigeMultiplier = 1f,
            float boostMultiplier = 1f)
        {
            var result = new CombatTickResult();
            var aliveEnemies = enemies.Where(e => e.IsAlive).ToList();
            if (aliveEnemies.Count == 0)
            {
                result.WaveCleared = true;
                return result;
            }

            // 1. Auto-attack: each party member deals passive DPS
            foreach (var stats in partyStats)
            {
                var highestStat = stats.HighestStat();
                float autoAtk = stats.GetStat(highestStat) * economyConfig.autoAttackDPSFraction;
                var target = aliveEnemies[rng.Next(aliveEnemies.Count)];
                target.TakeDamage(autoAtk);
                result.TotalDamageDealt += autoAtk;
            }

            // 2. If player activated a verb, resolve it
            if (activatedVerb != null)
            {
                ResolveVerb(activatedVerb, aliveEnemies, partyStats, result, prestigeMultiplier, boostMultiplier);
            }

            // 3. Tick status effects on all enemies
            foreach (var enemy in enemies)
            {
                enemy.TickStatuses();
            }

            // 4. Process enemy attacks
            foreach (var enemy in aliveEnemies)
            {
                float dmg = enemy.GetDamageOutput();
                result.TotalDamageReceived += dmg;
            }

            // 5. Check for kills
            foreach (var enemy in enemies)
            {
                if (!enemy.IsAlive)
                {
                    result.EnemiesKilled++;
                    result.GoldEarned += economyConfig.GoldPerKill(questLevel, prestigeMult: prestigeMultiplier, boostMult: boostMultiplier);
                }
            }

            // 6. Check wave cleared
            result.WaveCleared = enemies.All(e => !e.IsAlive);

            return result;
        }

        private void ResolveVerb(
            DrawnVerb drawn,
            List<EnemyState> aliveEnemies,
            Stats[] partyStats,
            CombatTickResult result,
            float prestigeMult,
            float boostMult)
        {
            var verb = drawn.Verb;
            var charStats = partyStats[drawn.OwnerIndex];
            int charStat = charStats.GetStat(verb.statType);

            // Determine targets
            List<EnemyState> targets;
            switch (verb.targetMode)
            {
                case TargetMode.AoE:
                    targets = new List<EnemyState>(aliveEnemies);
                    break;
                case TargetMode.Cleave:
                    targets = aliveEnemies.OrderBy(_ => rng.Next()).Take(System.Math.Min(3, aliveEnemies.Count)).ToList();
                    break;
                default: // Single
                    targets = new List<EnemyState> { aliveEnemies[rng.Next(aliveEnemies.Count)] };
                    break;
            }

            foreach (var target in targets)
            {
                var matchup = advantageMatrix.GetMatchup(verb.statType, target.StatType);

                if (matchup.advantage == Advantage.Strong) result.AdvantageHits++;
                else if (matchup.advantage == Advantage.Weak) result.DisadvantageHits++;

                float scalingMod = DamageCalculator.GetStatScalingModifier(charStat);
                float damage = DamageCalculator.Calculate(
                    verb.baseDamage * scalingMod,
                    verb.statScaling,
                    charStat,
                    matchup.damageMultiplier,
                    target.HasStatus(StatusEffectType.Expose),
                    prestigeMultiplier: prestigeMult,
                    boostMultiplier: boostMult
                );

                // Apply multi-hit
                for (int i = 0; i < verb.hitCount; i++)
                {
                    float hitDmg = damage / verb.hitCount;
                    target.TakeDamage(hitDmg);
                    result.TotalDamageDealt += hitDmg;
                }

                // Attempt status proc
                float procChance = verb.effectProcChance
                    * matchup.statusProcModifier
                    * DamageCalculator.GetStatProcModifier(charStat);

                if (procChance > 0 && rng.NextDouble() <= procChance)
                {
                    target.ApplyStatus(verb.effect, verb.effectDuration);
                    result.StatusProcs++;
                }
            }
        }
    }
}
```

**Step 3: Commit**

```bash
git add Assets/Scripts/Combat/
git commit -m "Add CombatTickProcessor, EnemyState, and CombatTickResult"
```

---

### Task 13: Pity Timer System

**Files:**
- Create: `Assets/Scripts/Equipment/PityTracker.cs`
- Test: `Assets/Tests/EditMode/Economy/PityTrackerTests.cs`

**Step 1: Write PityTracker**

`Assets/Scripts/Equipment/PityTracker.cs`:
```csharp
using System;
using Starquill.Core;
using Starquill.Data;

namespace Starquill.Equipment
{
    [Serializable]
    public class PityTracker
    {
        public int killsSinceUncommon;
        public int killsSinceRare;
        public int killsSinceEpic;
        public int killsSinceLegendary;

        /// <summary>
        /// Register a kill. Returns the guaranteed minimum rarity if pity triggers, or null.
        /// </summary>
        public Rarity? RegisterKill(EconomyConfig config)
        {
            killsSinceUncommon++;
            killsSinceRare++;
            killsSinceEpic++;
            killsSinceLegendary++;

            Rarity? guaranteed = null;

            if (killsSinceLegendary >= config.pityLegendary)
            {
                guaranteed = Rarity.Legendary;
                killsSinceLegendary = 0;
            }
            else if (killsSinceEpic >= config.pityEpic)
            {
                guaranteed = Rarity.Epic;
                killsSinceEpic = 0;
            }
            else if (killsSinceRare >= config.pityRare)
            {
                guaranteed = Rarity.Rare;
                killsSinceRare = 0;
            }
            else if (killsSinceUncommon >= config.pityUncommon)
            {
                guaranteed = Rarity.Uncommon;
                killsSinceUncommon = 0;
            }

            return guaranteed;
        }

        /// <summary>
        /// Call when a drop of the given rarity occurs (resets lower pity counters).
        /// </summary>
        public void RegisterDrop(Rarity rarity)
        {
            if (rarity >= Rarity.Uncommon) killsSinceUncommon = 0;
            if (rarity >= Rarity.Rare) killsSinceRare = 0;
            if (rarity >= Rarity.Epic) killsSinceEpic = 0;
            if (rarity >= Rarity.Legendary) killsSinceLegendary = 0;
        }
    }
}
```

**Step 2: Write PityTracker tests**

`Assets/Tests/EditMode/Economy/PityTrackerTests.cs`:
```csharp
using NUnit.Framework;
using Starquill.Core;
using Starquill.Data;
using Starquill.Equipment;
using UnityEngine;

namespace Starquill.Tests.Economy
{
    public class PityTrackerTests
    {
        private PityTracker tracker;
        private EconomyConfig config;

        [SetUp]
        public void SetUp()
        {
            tracker = new PityTracker();
            config = ScriptableObject.CreateInstance<EconomyConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
        }

        [Test]
        public void NoGuarantee_BeforePityThreshold()
        {
            for (int i = 0; i < 49; i++)
            {
                var result = tracker.RegisterKill(config);
                Assert.IsNull(result);
            }
        }

        [Test]
        public void GuaranteesUncommon_At50Kills()
        {
            Rarity? result = null;
            for (int i = 0; i < 50; i++)
                result = tracker.RegisterKill(config);
            Assert.AreEqual(Rarity.Uncommon, result);
        }

        [Test]
        public void GuaranteesRare_At200Kills()
        {
            Rarity? result = null;
            for (int i = 0; i < 200; i++)
            {
                result = tracker.RegisterKill(config);
                // Reset uncommon pity so it doesn't interfere
                if (result == Rarity.Uncommon) result = null;
            }
            Assert.AreEqual(Rarity.Rare, result);
        }

        [Test]
        public void RegisterDrop_ResetsPityCounters()
        {
            for (int i = 0; i < 40; i++)
                tracker.RegisterKill(config);

            tracker.RegisterDrop(Rarity.Uncommon);
            Assert.AreEqual(0, tracker.killsSinceUncommon);

            // Should need another 50 kills for next pity
            for (int i = 0; i < 49; i++)
            {
                var result = tracker.RegisterKill(config);
                Assert.IsNull(result);
            }
        }
    }
}
```

**Step 3: Run tests, verify pass**

**Step 4: Commit**

```bash
git add Assets/Scripts/Equipment/PityTracker.cs Assets/Tests/EditMode/Economy/PityTrackerTests.cs
git commit -m "Add PityTracker with guaranteed drop thresholds and tests"
```

---

## Phase 4: Characters & Party

### Task 14: Character Runtime Class

**Files:**
- Create: `Assets/Scripts/Characters/CharacterInstance.cs`
- Create: `Assets/Scripts/Characters/Party.cs`

**Step 1: Write CharacterInstance**

`Assets/Scripts/Characters/CharacterInstance.cs`:
```csharp
using System;
using System.Collections.Generic;
using Starquill.Core;
using Starquill.Data;

namespace Starquill.Characters
{
    [Serializable]
    public class CharacterInstance
    {
        public string id;
        public string displayName;
        public SpeciesDefinition species;
        public Stats baseStats;
        public int level = 1;
        public int xp;
        public int speciesKills; // For species ability ranking

        // Equipment
        public EquipmentDefinition[] equipment = new EquipmentDefinition[11]; // 11 slots

        // Verb loadout
        public List<VerbDefinition> equippedVerbs = new();

        // Level-up stat allocations
        public Stats allocatedStats = new();

        public Stats GetTotalStats()
        {
            var total = baseStats.Clone();
            total = total + allocatedStats;

            foreach (var equip in equipment)
            {
                if (equip != null)
                    total = total + equip.statMods;
            }

            return total;
        }

        public int GetVerbSlotCount()
        {
            if (level >= 51) return 5;
            if (level >= 26) return 4;
            if (level >= 11) return 3;
            return 2;
        }

        public int GetSpeciesAbilityRank()
        {
            if (species?.ability == null) return 0;
            var thresholds = species.ability.rankThresholds;
            for (int i = thresholds.Length - 1; i >= 0; i--)
            {
                if (speciesKills >= thresholds[i]) return i;
            }
            return 0;
        }

        public void EquipItem(EquipmentDefinition item)
        {
            equipment[(int)item.slot] = item;
        }

        public void UnequipSlot(EquipmentSlot slot)
        {
            equipment[(int)slot] = null;
        }
    }
}
```

**Step 2: Write Party**

`Assets/Scripts/Characters/Party.cs`:
```csharp
using System.Collections.Generic;
using System.Linq;
using Starquill.Core;
using Starquill.Data;

namespace Starquill.Characters
{
    public class Party
    {
        public const int MaxSize = 4;
        public List<CharacterInstance> Members { get; } = new();

        public Stats[] GetAllStats()
        {
            return Members.Select(m => m.GetTotalStats()).ToArray();
        }

        public List<VerbDefinition> GetAllVerbs()
        {
            return Members.SelectMany(m => m.equippedVerbs).ToList();
        }

        public float GetChaGoldBonus()
        {
            return Members.Sum(m => m.GetTotalStats().CHA) * 0.02f;
        }

        public float GetIntXPBonus()
        {
            return Members.Sum(m => m.GetTotalStats().INT) * 0.02f;
        }

        public float GetWisDiscoveryBonus()
        {
            return Members.Sum(m => m.GetTotalStats().WIS) * 0.02f;
        }

        public bool AddMember(CharacterInstance character)
        {
            if (Members.Count >= MaxSize) return false;
            Members.Add(character);
            return true;
        }

        public void RemoveMember(int index)
        {
            if (index >= 0 && index < Members.Count)
                Members.RemoveAt(index);
        }
    }
}
```

**Step 3: Commit**

```bash
git add Assets/Scripts/Characters/
git commit -m "Add CharacterInstance and Party classes with stat aggregation"
```

---

### Task 15: Exploration and Quest State Machines

**Files:**
- Create: `Assets/Scripts/Exploration/ExplorationState.cs`
- Create: `Assets/Scripts/Exploration/ExplorationManager.cs`
- Create: `Assets/Scripts/Quests/QuestState.cs`
- Create: `Assets/Scripts/Quests/QuestManager.cs`
- Create: `Assets/Scripts/Data/QuestZoneDefinition.cs`

**Step 1: Write QuestZoneDefinition**

`Assets/Scripts/Data/QuestZoneDefinition.cs`:
```csharp
using UnityEngine;
using Starquill.Core;

namespace Starquill.Data
{
    [CreateAssetMenu(menuName = "Starquill/Quest Zone")]
    public class QuestZoneDefinition : ScriptableObject
    {
        public string zoneId;
        public string displayName;
        public string description;
        public int zoneLevel;
        public StatType[] dominantEnemyTypes;
        public int questCount = 10;
        public Sprite backgroundArt;

        [Header("Dialogue")]
        public string[] discoveryDialogue;
        public string[] completionDialogue;

        [Header("Milestone Reward")]
        public string milestoneUnlockDescription;
    }
}
```

**Step 2: Write ExplorationState and ExplorationManager**

`Assets/Scripts/Exploration/ExplorationState.cs`:
```csharp
namespace Starquill.Exploration
{
    public enum ExplorationState
    {
        Exploring,   // Ambient wave combat
        InQuest,     // Structured quest zone
        QuestRetreat // Quest failed, exploring with retry available
    }
}
```

`Assets/Scripts/Exploration/ExplorationManager.cs`:
```csharp
using System;
using Starquill.Data;

namespace Starquill.Exploration
{
    /// <summary>
    /// Manages the ambient exploration loop.
    /// Handles wave progression, quest discovery, and fragment drops.
    /// </summary>
    public class ExplorationManager
    {
        public ExplorationState State { get; private set; } = ExplorationState.Exploring;
        public int CurrentWave { get; private set; }
        public int QuestLevel { get; set; } = 1;
        public float FragmentProgress { get; private set; }

        private readonly EconomyConfig config;
        private readonly Random rng;

        // Events
        public event Action<QuestZoneDefinition> OnQuestDiscovered;
        public event Action<float> OnFragmentDropped;
        public event Action OnWaveCleared;

        public ExplorationManager(EconomyConfig config, int? seed = null)
        {
            this.config = config;
            rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public void ProcessWaveCleared()
        {
            CurrentWave++;
            OnWaveCleared?.Invoke();

            // Check for quest discovery
            if (State == ExplorationState.Exploring && rng.NextDouble() < config.questDiscoveryRate)
            {
                // Quest discovered — caller handles which zone
                // For now, fire event and let manager resolve
            }

            // Check for fragment drops
            if (rng.NextDouble() < config.fragmentDropRate)
            {
                float fragment = 1f;
                FragmentProgress += fragment;
                OnFragmentDropped?.Invoke(fragment);
            }
        }

        public void EnterQuest()
        {
            State = ExplorationState.InQuest;
        }

        public void QuestCompleted()
        {
            State = ExplorationState.Exploring;
            CurrentWave = 0;
        }

        public void QuestRetreated()
        {
            State = ExplorationState.QuestRetreat;
        }

        public void RetryQuest()
        {
            State = ExplorationState.InQuest;
        }

        public void DismissRetry()
        {
            State = ExplorationState.Exploring;
        }
    }
}
```

**Step 3: Commit**

```bash
git add Assets/Scripts/Exploration/ Assets/Scripts/Quests/ Assets/Scripts/Data/QuestZoneDefinition.cs
git commit -m "Add ExplorationManager state machine and QuestZoneDefinition"
```

---

## Phase 5: Persistence

### Task 16: Save/Load System

**Files:**
- Create: `Assets/Scripts/Managers/SaveData.cs`
- Create: `Assets/Scripts/Managers/SaveManager.cs`

**Step 1: Write SaveData**

`Assets/Scripts/Managers/SaveData.cs`:
```csharp
using System;
using System.Collections.Generic;
using Starquill.Equipment;

namespace Starquill.Managers
{
    [Serializable]
    public class SaveData
    {
        // Economy
        public double gold;
        public int highestQuestLevel;

        // Party (character IDs + state)
        public List<CharacterSaveData> characters = new();
        public List<int> partyMemberIndices = new();

        // Pity
        public PityTracker pityTracker = new();

        // Offline
        public long lastPlayedTimestamp;

        // Exploration
        public int currentQuestLevel = 1;
        public float fragmentProgress;

        // Codex (collected item IDs)
        public HashSet<string> codexEntries = new();
    }

    [Serializable]
    public class CharacterSaveData
    {
        public string id;
        public string displayName;
        public string speciesId;
        public int level;
        public int xp;
        public int speciesKills;
        public int[] allocatedStats = new int[6]; // STR,DEX,CON,INT,WIS,CHA
        public List<string> equippedVerbIds = new();
        public string[] equippedItemIds = new string[11];
    }
}
```

**Step 2: Write SaveManager**

`Assets/Scripts/Managers/SaveManager.cs`:
```csharp
using UnityEngine;

namespace Starquill.Managers
{
    public class SaveManager : MonoBehaviour
    {
        private const string SaveKey = "StarquillSave";

        public SaveData CurrentSave { get; private set; } = new();

        public void Save()
        {
            CurrentSave.lastPlayedTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string json = JsonUtility.ToJson(CurrentSave, true);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }

        public SaveData Load()
        {
            if (PlayerPrefs.HasKey(SaveKey))
            {
                string json = PlayerPrefs.GetString(SaveKey);
                CurrentSave = JsonUtility.FromJson<SaveData>(json);
            }
            else
            {
                CurrentSave = new SaveData();
            }
            return CurrentSave;
        }

        public float GetOfflineSeconds()
        {
            if (CurrentSave.lastPlayedTimestamp == 0) return 0;
            long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return now - CurrentSave.lastPlayedTimestamp;
        }

        public void DeleteSave()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            CurrentSave = new SaveData();
        }
    }
}
```

**Step 3: Commit**

```bash
git add Assets/Scripts/Managers/
git commit -m "Add SaveManager with JSON serialization and offline time tracking"
```

---

### Task 17: GameManager — Tying It Together

**Files:**
- Create: `Assets/Scripts/Managers/GameManager.cs`

**Step 1: Write GameManager**

`Assets/Scripts/Managers/GameManager.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;
using Starquill.Characters;
using Starquill.Combat;
using Starquill.Data;
using Starquill.Equipment;
using Starquill.Exploration;

namespace Starquill.Managers
{
    /// <summary>
    /// Central game controller. Manages the tick loop, party, exploration, and economy.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Config")]
        public EconomyConfig economyConfig;
        public AdvantageMatrix advantageMatrix;

        [Header("State")]
        public double gold;
        public int questLevel = 1;

        // Runtime systems
        private Party party;
        private VerbPool verbPool;
        private CombatTickProcessor combatProcessor;
        private ExplorationManager exploration;
        private PityTracker pityTracker;
        private SaveManager saveManager;

        // Combat state
        private List<EnemyState> currentEnemies = new();
        private float tickTimer;
        private float currentTime;

        public Party Party => party;
        public VerbPool VerbPool => verbPool;
        public ExplorationManager Exploration => exploration;

        private void Awake()
        {
            saveManager = GetComponent<SaveManager>();
            if (saveManager == null)
                saveManager = gameObject.AddComponent<SaveManager>();

            party = new Party();
            verbPool = new VerbPool(
                economyConfig.verbSlotCount,
                economyConfig.verbDrawCooldown
            );
            combatProcessor = new CombatTickProcessor(advantageMatrix, economyConfig);
            exploration = new ExplorationManager(economyConfig);
            pityTracker = new PityTracker();
        }

        private void Start()
        {
            var save = saveManager.Load();
            gold = save.gold;
            questLevel = save.currentQuestLevel;

            // Calculate offline earnings
            float offlineSeconds = saveManager.GetOfflineSeconds();
            if (offlineSeconds > 0)
            {
                float goldPerSecond = economyConfig.GoldPerKill(questLevel) * economyConfig.exploreKillsPerMinute / 60f;
                float offlineGold = economyConfig.OfflineGold(goldPerSecond, offlineSeconds);
                // Store for claim screen — don't add directly
                // UI will show claim button
            }

            SpawnWave();
            RebuildVerbPool();
        }

        private void Update()
        {
            tickTimer += Time.deltaTime;
            currentTime += Time.deltaTime;

            if (tickTimer >= 1f) // 1 tick per second
            {
                tickTimer -= 1f;
                ProcessTick();
            }
        }

        private void ProcessTick()
        {
            // Verb rotation
            verbPool.RotateStaleVerbs(currentTime);
            verbPool.FillSlots(currentTime);
            verbPool.TickCooldowns();

            // Combat tick (no verb activated — auto-attack only)
            var result = combatProcessor.ProcessTick(
                currentEnemies,
                party.GetAllStats(),
                null, // No verb this tick unless player tapped
                questLevel,
                economyConfig.prestigeMultiplierBase
            );

            gold += result.GoldEarned;

            if (result.WaveCleared)
            {
                exploration.ProcessWaveCleared();
                SpawnWave();
            }

            // Auto-save every 30 seconds
            if ((int)currentTime % 30 == 0)
            {
                saveManager.CurrentSave.gold = gold;
                saveManager.CurrentSave.currentQuestLevel = questLevel;
                saveManager.Save();
            }
        }

        /// <summary>
        /// Called by UI when player taps a verb card.
        /// </summary>
        public void OnVerbTapped(int slotIndex)
        {
            var activated = verbPool.ActivateVerb(slotIndex);
            if (activated == null) return;

            var result = combatProcessor.ProcessTick(
                currentEnemies,
                party.GetAllStats(),
                activated,
                questLevel,
                economyConfig.prestigeMultiplierBase
            );

            gold += result.GoldEarned;

            if (result.WaveCleared)
            {
                exploration.ProcessWaveCleared();
                SpawnWave();
            }
        }

        private void SpawnWave()
        {
            currentEnemies.Clear();
            int enemyCount = 2 + questLevel / 10; // Scale enemy count with level
            if (enemyCount > 6) enemyCount = 6;

            var statTypes = new[] {
                Core.StatType.STR, Core.StatType.DEX, Core.StatType.CON,
                Core.StatType.INT, Core.StatType.WIS, Core.StatType.CHA
            };

            for (int i = 0; i < enemyCount; i++)
            {
                var type = statTypes[Random.Range(0, statTypes.Length)];
                float hp = economyConfig.EnemyHP(questLevel);
                currentEnemies.Add(new EnemyState($"enemy_{i}", type, hp, hp * 0.05f));
            }
        }

        private void RebuildVerbPool()
        {
            verbPool.Clear();
            for (int i = 0; i < party.Members.Count; i++)
            {
                verbPool.AddVerbs(i, party.Members[i].equippedVerbs);
            }
            verbPool.FillSlots(currentTime);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                saveManager.CurrentSave.gold = gold;
                saveManager.CurrentSave.currentQuestLevel = questLevel;
                saveManager.Save();
            }
        }

        private void OnApplicationQuit()
        {
            saveManager.CurrentSave.gold = gold;
            saveManager.CurrentSave.currentQuestLevel = questLevel;
            saveManager.Save();
        }
    }
}
```

**Step 2: Commit**

```bash
git add Assets/Scripts/Managers/GameManager.cs
git commit -m "Add GameManager with tick loop, verb activation, and auto-save"
```

---

## Phase 6: Assembly Definitions

### Task 18: Wire Up Assembly References

**Files:**
- Create/Update: Assembly definition files so all namespaces can reference each other

**Step 1: Create remaining assembly definitions**

`Assets/Scripts/Characters/Starquill.Characters.asmdef`:
```json
{
    "name": "Starquill.Characters",
    "rootNamespace": "Starquill.Characters",
    "references": ["Starquill.Core", "Starquill.Data"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "autoReferenced": true
}
```

`Assets/Scripts/Equipment/Starquill.Equipment.asmdef`:
```json
{
    "name": "Starquill.Equipment",
    "rootNamespace": "Starquill.Equipment",
    "references": ["Starquill.Core", "Starquill.Data"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "autoReferenced": true
}
```

`Assets/Scripts/Data/Starquill.Data.asmdef`:
```json
{
    "name": "Starquill.Data",
    "rootNamespace": "Starquill.Data",
    "references": ["Starquill.Core"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "autoReferenced": true
}
```

`Assets/Scripts/Exploration/Starquill.Exploration.asmdef`:
```json
{
    "name": "Starquill.Exploration",
    "rootNamespace": "Starquill.Exploration",
    "references": ["Starquill.Core", "Starquill.Data"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "autoReferenced": true
}
```

`Assets/Scripts/Managers/Starquill.Managers.asmdef`:
```json
{
    "name": "Starquill.Managers",
    "rootNamespace": "Starquill.Managers",
    "references": [
        "Starquill.Core",
        "Starquill.Data",
        "Starquill.Combat",
        "Starquill.Characters",
        "Starquill.Equipment",
        "Starquill.Exploration"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "autoReferenced": true
}
```

Update `Assets/Tests/EditMode/EditModeTests.asmdef` to reference all assemblies:
```json
{
    "name": "EditModeTests",
    "rootNamespace": "Starquill.Tests",
    "references": [
        "Starquill.Core",
        "Starquill.Data",
        "Starquill.Combat",
        "Starquill.Economy",
        "Starquill.Characters",
        "Starquill.Equipment",
        "Starquill.Exploration",
        "Starquill.Managers"
    ],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "optionalUnityReferences": ["TestAssemblies"]
}
```

**Step 2: Verify compilation in Unity — no errors**

**Step 3: Run all tests — all should pass**

**Step 4: Commit**

```bash
git add Assets/Scripts/ Assets/Tests/
git commit -m "Add assembly definitions for all modules and wire test references"
```

---

## Phase 7: Verification & Push

### Task 19: Full Test Run and Push

**Step 1: Open Unity → Window → General → Test Runner → EditMode → Run All**

Expected results:
- StatType tests: 6 pass
- Stats tests: 4 pass
- AdvantageMatrix tests: 14 pass
- DamageCalculator tests: 10 pass
- EconomyConfig tests: 8 pass
- VerbPool tests: 5 pass
- PityTracker tests: 3 pass
- **Total: ~50 tests, all green**

**Step 2: Verify the game compiles and runs**

Unity → File → Build Settings → Build (or just hit Play in Editor). The game should launch without errors. No UI yet — just the tick loop running.

**Step 3: Push branch**

```bash
git push -u origin unity-idle-clicker
```

---

## Summary: Sprint 1 Commit History

| # | Commit | Phase |
|---|--------|-------|
| 1 | Add idle RPG clicker architecture and design documents | Setup |
| 2 | Archive Godot files and prepare for Unity project | Setup |
| 3 | Initialize Unity 6 project with folder structure and test framework | Setup |
| 4 | Add core enums: StatType, Rarity, TargetMode, StatusEffectType, EquipmentSlot | Data |
| 5 | Add Stats class with tests and SpeciesDefinition ScriptableObjects | Data |
| 6 | Add Equipment, Affix, and EquipmentSet ScriptableObject definitions | Data |
| 7 | Add VerbDefinition and StatusEffectDefinition ScriptableObjects | Data |
| 8 | Add EconomyConfig and AdvantageMatrix with full test coverage | Math |
| 9 | Add DamageCalculator with full damage formula and stat modifier tests | Math |
| 10 | Add EconomyConfig formula tests validating HP/gold/cost scaling curves | Math |
| 11 | Add VerbPool with draw/rotate/cooldown mechanics and tests | Combat |
| 12 | Add CombatTickProcessor, EnemyState, and CombatTickResult | Combat |
| 13 | Add PityTracker with guaranteed drop thresholds and tests | Combat |
| 14 | Add CharacterInstance and Party classes with stat aggregation | Characters |
| 15 | Add ExplorationManager state machine and QuestZoneDefinition | Exploration |
| 16 | Add SaveManager with JSON serialization and offline time tracking | Persistence |
| 17 | Add GameManager with tick loop, verb activation, and auto-save | Core |
| 18 | Add assembly definitions for all modules and wire test references | Wiring |
| 19 | Full test verification and push | Verification |

**What Sprint 1 delivers:** The full backend loop — exploration tick system, verb draw/rotate/cooldown, dual-triangle combat math, enemy waves, gold economy, loot pity timers, party management, save/load, and offline earnings. No UI yet (that's Sprint 2's first priority), but all the math and systems are testable and verified.

**Sprint 2 will add:** UI screens (Explore, Party, Loot, Shop), paper-doll rendering, equipment upgrade paths, full rarity/affix system, ads integration, and cloud save.
