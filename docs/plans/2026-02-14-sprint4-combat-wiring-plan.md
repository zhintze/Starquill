# Sprint 4: Minimum Viable Combat — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Wire existing backend (GameManager, VerbPool, CombatTickProcessor) to ExploreScene UI so players can tap verb cards, see damage numbers, watch enemy HP bars, and accumulate gold in real-time.

**Architecture:** GameManager becomes a singleton with events. UI subscribes to events in ExploreSceneController. VerbPool gains pass-count staleness replacing timer rotation. New UI components (DamageNumberSpawner, EnemyDisplayController, VerbCardAnimator, GoldCounterAnimator) handle visual feedback. All animation is coroutine-based (no DOTween).

**Tech Stack:** Unity 6 (6000.3.8f1), C# 12, TextMeshPro, NUnit, 10 assembly definitions

**Important context:**
- Tests cannot run headless on Arch Linux — must use Unity Editor GUI
- Verify compile errors via `check_compile_errors` MCP tool after each code change
- Assembly refs: Starquill.UI needs Managers, Combat, Exploration, Characters added
- Design doc: `docs/plans/2026-02-13-sprint4-combat-wiring-design.md`

---

### Task 1: DrawnVerb PassCount + VerbPool Pass-Count Rotation

**Files:**
- Modify: `Assets/Scripts/Combat/DrawnVerb.cs`
- Modify: `Assets/Scripts/Combat/VerbPool.cs`
- Test: `Assets/Tests/EditMode/Combat/VerbPoolTests.cs`

**Step 1: Add PassCount to DrawnVerb**

In `Assets/Scripts/Combat/DrawnVerb.cs`, add a `PassCount` property and `IncrementPassCount()`:

```csharp
// After the IsOnCooldown property (line 11), add:
public int PassCount { get; private set; }

// After TickCooldown() (line 22), add:
public void IncrementPassCount() { PassCount++; }
public void ResetPassCount() { PassCount = 0; }
```

**Step 2: Write failing tests for pass-count rotation**

Add to `Assets/Tests/EditMode/Combat/VerbPoolTests.cs`:

```csharp
[Test]
public void IncrementPassCounts_BumpsNonActivatedSlots()
{
    pool.AddVerbs(0, new[] {
        CreateVerb("v1", StatType.STR),
        CreateVerb("v2", StatType.DEX),
        CreateVerb("v3", StatType.INT),
        CreateVerb("v4", StatType.WIS)
    });
    pool.FillSlots(0f);
    // Activate slot 1 — slots 0 and 2 should get PassCount incremented
    pool.IncrementPassCounts(1);
    Assert.AreEqual(1, pool.DrawnSlots[0].PassCount);
    Assert.AreEqual(1, pool.DrawnSlots[1].PassCount); // was slot 2, now shifted to 1
}

[Test]
public void ReplaceStaleVerbs_RemovesVerbsWithPassCountAtThreshold()
{
    pool.AddVerbs(0, new[] {
        CreateVerb("v1", StatType.STR),
        CreateVerb("v2", StatType.DEX),
        CreateVerb("v3", StatType.INT),
        CreateVerb("v4", StatType.WIS)
    });
    pool.FillSlots(0f);
    // Pass over slot 0 twice (activate 1 twice, refilling between)
    pool.IncrementPassCounts(1);
    pool.FillSlots(1f);
    pool.IncrementPassCounts(1);
    var replaced = pool.ReplaceStaleVerbs(2f);
    Assert.IsTrue(replaced.Count > 0);
    // All remaining drawn verbs should have PassCount < 2
    foreach (var v in pool.DrawnSlots)
        Assert.Less(v.PassCount, 2);
}

[Test]
public void IncrementPassCounts_DoesNotIncrementActivatedSlot()
{
    pool.AddVerbs(0, new[] {
        CreateVerb("v1", StatType.STR),
        CreateVerb("v2", StatType.DEX),
        CreateVerb("v3", StatType.INT),
        CreateVerb("v4", StatType.WIS)
    });
    pool.FillSlots(0f);
    string slot0Id = pool.DrawnSlots[0].Verb.verbId;
    pool.IncrementPassCounts(0);
    // Slot 0 was activated so it gets removed — remaining slots are the old 1 and 2
    // But wait, IncrementPassCounts doesn't remove — it just marks. Let me adjust.
    // Actually: IncrementPassCounts increments the non-activated ones BEFORE removal
    Assert.AreEqual(0, pool.DrawnSlots[0].PassCount); // slot 0 itself wasn't incremented
    Assert.AreEqual(1, pool.DrawnSlots[1].PassCount);
    Assert.AreEqual(1, pool.DrawnSlots[2].PassCount);
}
```

**Step 3: Implement IncrementPassCounts and ReplaceStaleVerbs on VerbPool**

In `Assets/Scripts/Combat/VerbPool.cs`, add two new methods and events:

```csharp
// After the AvailableCount property, add events:
public event System.Action<int, DrawnVerb> OnVerbDrawn;
public event System.Action<int, DrawnVerb> OnVerbRemoved;

// New method — call after ActivateVerb to bump pass counts on remaining verbs:
public void IncrementPassCounts(int activatedSlotIndex)
{
    for (int i = 0; i < drawnSlots.Count; i++)
    {
        if (i != activatedSlotIndex)
            drawnSlots[i].IncrementPassCount();
    }
}

// New method — removes verbs with PassCount >= threshold, returns removed list:
public List<DrawnVerb> ReplaceStaleVerbs(float currentTime, int passThreshold = 2)
{
    var replaced = new List<DrawnVerb>();
    for (int i = drawnSlots.Count - 1; i >= 0; i--)
    {
        if (drawnSlots[i].PassCount >= passThreshold)
        {
            var removed = drawnSlots[i];
            replaced.Add(removed);
            OnVerbRemoved?.Invoke(i, removed);
            drawnSlots.RemoveAt(i);
        }
    }
    return replaced;
}
```

Also update `FillSlots` to fire `OnVerbDrawn`:

```csharp
public void FillSlots(float currentTime)
{
    while (drawnSlots.Count < maxSlots)
    {
        var drawn = DrawRandom(currentTime);
        if (drawn == null) break;
        int index = drawnSlots.Count;
        drawnSlots.Add(drawn);
        OnVerbDrawn?.Invoke(index, drawn);
    }
}
```

And update `ActivateVerb` to fire `OnVerbRemoved`:

```csharp
public DrawnVerb ActivateVerb(int slotIndex)
{
    if (slotIndex < 0 || slotIndex >= drawnSlots.Count) return null;
    var verb = drawnSlots[slotIndex];
    OnVerbRemoved?.Invoke(slotIndex, verb);
    drawnSlots.RemoveAt(slotIndex);
    verb.StartCooldown();
    onCooldown.Add(verb);
    return verb;
}
```

**Step 4: Verify no compile errors**

Use MCP `check_compile_errors` to confirm clean compilation.

**Step 5: Commit**

```
git add Assets/Scripts/Combat/DrawnVerb.cs Assets/Scripts/Combat/VerbPool.cs Assets/Tests/EditMode/Combat/VerbPoolTests.cs
git commit -m "Add pass-count staleness to VerbPool, replacing timer-based rotation"
```

---

### Task 2: StatTypeColors + NumberFormatter Utilities

**Files:**
- Create: `Assets/Scripts/UI/StatTypeColors.cs`
- Create: `Assets/Scripts/UI/NumberFormatter.cs`
- Test: `Assets/Tests/EditMode/UI/StatTypeColorsTests.cs`
- Test: `Assets/Tests/EditMode/UI/NumberFormatterTests.cs`

**Step 1: Write failing tests for StatTypeColors**

Create `Assets/Tests/EditMode/UI/StatTypeColorsTests.cs`:

```csharp
using NUnit.Framework;
using Starquill.Core;
using Starquill.UI;
using UnityEngine;

namespace Starquill.Tests.UI
{
    [TestFixture]
    public class StatTypeColorsTests
    {
        [Test]
        public void GetColor_STR_ReturnsRed()
        {
            var color = StatTypeColors.GetColor(StatType.STR);
            Assert.AreEqual(0.9f, color.r, 0.01f);
            Assert.AreEqual(0.2f, color.g, 0.01f);
        }

        [Test]
        public void GetColor_AllTypesReturnDistinctColors()
        {
            var colors = new Color[6];
            colors[0] = StatTypeColors.GetColor(StatType.STR);
            colors[1] = StatTypeColors.GetColor(StatType.DEX);
            colors[2] = StatTypeColors.GetColor(StatType.CON);
            colors[3] = StatTypeColors.GetColor(StatType.INT);
            colors[4] = StatTypeColors.GetColor(StatType.WIS);
            colors[5] = StatTypeColors.GetColor(StatType.CHA);
            for (int i = 0; i < 5; i++)
                for (int j = i + 1; j < 6; j++)
                    Assert.AreNotEqual(colors[i], colors[j], $"{(StatType)i} and {(StatType)j} should differ");
        }

        [Test]
        public void GetAdvantageColor_Strong_ReturnsGreen()
        {
            var color = StatTypeColors.GetAdvantageColor(Advantage.Strong);
            Assert.Greater(color.g, color.r);
        }

        [Test]
        public void GetAdvantageColor_Weak_ReturnsRed()
        {
            var color = StatTypeColors.GetAdvantageColor(Advantage.Weak);
            Assert.Greater(color.r, color.g);
        }

        [Test]
        public void GetAdvantageColor_Neutral_ReturnsWhite()
        {
            var color = StatTypeColors.GetAdvantageColor(Advantage.Neutral);
            Assert.AreEqual(1f, color.r, 0.01f);
            Assert.AreEqual(1f, color.g, 0.01f);
        }
    }
}
```

**Step 2: Write failing tests for NumberFormatter**

Create `Assets/Tests/EditMode/UI/NumberFormatterTests.cs`:

```csharp
using NUnit.Framework;
using Starquill.UI;

namespace Starquill.Tests.UI
{
    [TestFixture]
    public class NumberFormatterTests
    {
        [Test]
        public void Format_SmallNumber_NoSuffix()
        {
            Assert.AreEqual("999", NumberFormatter.FormatCompact(999));
        }

        [Test]
        public void Format_Thousands_UseK()
        {
            Assert.AreEqual("1.2K", NumberFormatter.FormatCompact(1234));
        }

        [Test]
        public void Format_Millions_UseM()
        {
            Assert.AreEqual("5.6M", NumberFormatter.FormatCompact(5600000));
        }

        [Test]
        public void Format_Billions_UseB()
        {
            Assert.AreEqual("1.0B", NumberFormatter.FormatCompact(1000000000));
        }

        [Test]
        public void Format_ExactThousand()
        {
            Assert.AreEqual("1.0K", NumberFormatter.FormatCompact(1000));
        }

        [Test]
        public void Format_Zero()
        {
            Assert.AreEqual("0", NumberFormatter.FormatCompact(0));
        }

        [Test]
        public void FormatGold_AddsPrefix()
        {
            Assert.AreEqual("+5g", NumberFormatter.FormatGoldDrop(5));
        }

        [Test]
        public void FormatGold_LargeAmount()
        {
            Assert.AreEqual("+1.2Kg", NumberFormatter.FormatGoldDrop(1200));
        }
    }
}
```

**Step 3: Implement StatTypeColors**

Create `Assets/Scripts/UI/StatTypeColors.cs`:

```csharp
using Starquill.Core;
using UnityEngine;

namespace Starquill.UI
{
    public static class StatTypeColors
    {
        public static Color GetColor(StatType stat)
        {
            return stat switch
            {
                StatType.STR => new Color(0.9f, 0.2f, 0.2f),
                StatType.DEX => new Color(0.2f, 0.8f, 0.3f),
                StatType.CON => new Color(0.7f, 0.5f, 0.2f),
                StatType.INT => new Color(0.3f, 0.4f, 0.9f),
                StatType.WIS => new Color(0.6f, 0.3f, 0.8f),
                StatType.CHA => new Color(0.9f, 0.8f, 0.2f),
                _ => Color.white
            };
        }

        public static Color GetAdvantageColor(Advantage advantage)
        {
            return advantage switch
            {
                Advantage.Strong => new Color(0.2f, 0.9f, 0.3f),
                Advantage.Weak => new Color(0.9f, 0.3f, 0.2f),
                _ => Color.white
            };
        }
    }
}
```

**Step 4: Implement NumberFormatter**

Create `Assets/Scripts/UI/NumberFormatter.cs`:

```csharp
namespace Starquill.UI
{
    public static class NumberFormatter
    {
        public static string FormatCompact(double value)
        {
            if (value < 1000) return ((int)value).ToString();
            if (value < 1000000) return (value / 1000).ToString("0.0") + "K";
            if (value < 1000000000) return (value / 1000000).ToString("0.0") + "M";
            return (value / 1000000000).ToString("0.0") + "B";
        }

        public static string FormatGoldDrop(double value)
        {
            if (value < 1000) return "+" + (int)value + "g";
            return "+" + FormatCompact(value) + "g";
        }
    }
}
```

**Step 5: Verify no compile errors**

Use MCP `check_compile_errors`.

**Step 6: Commit**

```
git add Assets/Scripts/UI/StatTypeColors.cs Assets/Scripts/UI/NumberFormatter.cs Assets/Tests/EditMode/UI/StatTypeColorsTests.cs Assets/Tests/EditMode/UI/NumberFormatterTests.cs
git commit -m "Add StatTypeColors and NumberFormatter utilities with tests"
```

---

### Task 3: EconomyConfig verbLockDuration + GameManager Singleton

**Files:**
- Modify: `Assets/Scripts/Data/EconomyConfig.cs:29` (Combat header)
- Modify: `Assets/Scripts/Managers/GameManager.cs`

**Step 1: Add verbLockDuration to EconomyConfig**

In `Assets/Scripts/Data/EconomyConfig.cs`, under the `[Header("Combat")]` section (line 29-30), add:

```csharp
[Header("Combat")]
public float autoAttackDPSFraction = 0.3f;
public float verbLockDuration = 3.0f;
```

**Step 2: Convert GameManager to Singleton with events**

Rewrite `Assets/Scripts/Managers/GameManager.cs`:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using Starquill.Characters;
using Starquill.Combat;
using Starquill.Core;
using Starquill.Data;
using Starquill.Equipment;
using Starquill.Exploration;

namespace Starquill.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Config")]
        public EconomyConfig economyConfig;
        public AdvantageMatrix advantageMatrix;

        [Header("State")]
        public double gold;
        public int questLevel = 1;

        private Party party;
        private VerbPool verbPool;
        private CombatTickProcessor combatProcessor;
        private ExplorationManager exploration;
        private PityTracker pityTracker;
        private SaveManager saveManager;
        private List<EnemyState> currentEnemies = new();
        private float tickTimer;
        private float currentTime;
        private float verbLockTimer;

        public Party Party => party;
        public VerbPool VerbPool => verbPool;
        public ExplorationManager Exploration => exploration;
        public IReadOnlyList<EnemyState> CurrentEnemies => currentEnemies;
        public bool VerbsLocked => verbLockTimer > 0f;

        // Events
        public event Action<CombatTickResult> OnCombatTick;
        public event Action<double> OnGoldChanged;
        public event Action<IReadOnlyList<EnemyState>> OnWaveStarted;
        public event Action OnWaveCleared;
        public event Action<int, CombatTickResult> OnVerbActivated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            saveManager = GetComponent<SaveManager>();
            if (saveManager == null)
                saveManager = gameObject.AddComponent<SaveManager>();

            party = new Party();
            verbPool = new VerbPool(economyConfig.verbSlotCount, economyConfig.verbDrawCooldown);
            combatProcessor = new CombatTickProcessor(advantageMatrix, economyConfig);
            exploration = new ExplorationManager(economyConfig);
            pityTracker = new PityTracker();
        }

        private void Start()
        {
            var save = saveManager.Load();
            gold = save.gold;
            questLevel = save.currentQuestLevel;

            float offlineSeconds = saveManager.GetOfflineSeconds();
            if (offlineSeconds > 0)
            {
                float goldPerSecond = economyConfig.GoldPerKill(questLevel) * economyConfig.exploreKillsPerMinute / 60f;
                float offlineGold = economyConfig.OfflineGold(goldPerSecond, offlineSeconds);
                // TODO: Show claim screen instead of auto-adding
            }

            SpawnWave();
            RebuildVerbPool();
        }

        private void Update()
        {
            tickTimer += Time.deltaTime;
            currentTime += Time.deltaTime;

            if (verbLockTimer > 0f)
                verbLockTimer -= Time.deltaTime;

            if (tickTimer >= 1f)
            {
                tickTimer -= 1f;
                ProcessTick();
            }
        }

        private void ProcessTick()
        {
            verbPool.FillSlots(currentTime);
            verbPool.TickCooldowns();

            var result = combatProcessor.ProcessTick(
                currentEnemies, party.GetAllStats(), null,
                questLevel, economyConfig.prestigeMultiplierBase);

            if (result.GoldEarned > 0)
            {
                gold += result.GoldEarned;
                OnGoldChanged?.Invoke(gold);
            }

            OnCombatTick?.Invoke(result);

            if (result.WaveCleared)
            {
                OnWaveCleared?.Invoke();
                exploration.ProcessWaveCleared();
                SpawnWave();
            }

            if ((int)currentTime % 30 == 0)
            {
                saveManager.CurrentSave.gold = gold;
                saveManager.CurrentSave.currentQuestLevel = questLevel;
                saveManager.Save();
            }
        }

        public void OnVerbTapped(int slotIndex)
        {
            if (VerbsLocked) return;

            var activated = verbPool.ActivateVerb(slotIndex);
            if (activated == null) return;

            verbPool.IncrementPassCounts(slotIndex);
            verbPool.ReplaceStaleVerbs(currentTime);
            verbPool.FillSlots(currentTime);

            verbLockTimer = economyConfig.verbLockDuration;

            var result = combatProcessor.ProcessTick(
                currentEnemies, party.GetAllStats(), activated,
                questLevel, economyConfig.prestigeMultiplierBase);

            if (result.GoldEarned > 0)
            {
                gold += result.GoldEarned;
                OnGoldChanged?.Invoke(gold);
            }

            OnVerbActivated?.Invoke(slotIndex, result);

            if (result.WaveCleared)
            {
                OnWaveCleared?.Invoke();
                exploration.ProcessWaveCleared();
                SpawnWave();
            }
        }

        private void SpawnWave()
        {
            currentEnemies.Clear();
            int enemyCount = 2 + questLevel / 10;
            if (enemyCount > 6) enemyCount = 6;

            var statTypes = new[] {
                StatType.STR, StatType.DEX, StatType.CON,
                StatType.INT, StatType.WIS, StatType.CHA
            };

            for (int i = 0; i < enemyCount; i++)
            {
                var type = statTypes[UnityEngine.Random.Range(0, statTypes.Length)];
                float hp = economyConfig.EnemyHP(questLevel);
                currentEnemies.Add(new EnemyState($"enemy_{i}", type, hp, hp * 0.05f));
            }

            OnWaveStarted?.Invoke(currentEnemies);
        }

        private void RebuildVerbPool()
        {
            verbPool.Clear();
            for (int i = 0; i < party.Members.Count; i++)
                verbPool.AddVerbs(i, party.Members[i].equippedVerbs);
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

**Step 3: Verify no compile errors**

Use MCP `check_compile_errors`.

**Step 4: Commit**

```
git add Assets/Scripts/Data/EconomyConfig.cs Assets/Scripts/Managers/GameManager.cs
git commit -m "Convert GameManager to singleton with events and verb lock timer"
```

---

### Task 4: Update UI Assembly + GoldCounterAnimator

**Files:**
- Modify: `Assets/Scripts/UI/Starquill.UI.asmdef`
- Create: `Assets/Scripts/UI/GoldCounterAnimator.cs`

**Step 1: Update UI assembly references**

In `Assets/Scripts/UI/Starquill.UI.asmdef`, add Combat, Managers, Characters, and Exploration references:

```json
{
    "name": "Starquill.UI",
    "rootNamespace": "Starquill.UI",
    "references": [
        "Starquill.Core",
        "Starquill.Data",
        "Starquill.Display",
        "Starquill.Combat",
        "Starquill.Managers",
        "Starquill.Characters",
        "Starquill.Exploration",
        "Unity.TextMeshPro"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "autoReferenced": true
}
```

**Step 2: Create GoldCounterAnimator**

Create `Assets/Scripts/UI/GoldCounterAnimator.cs`:

```csharp
using TMPro;
using UnityEngine;

namespace Starquill.UI
{
    public class GoldCounterAnimator : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private float lerpSpeed = 4f;

        private double displayedValue;
        private double targetValue;

        public void SetTarget(double value)
        {
            targetValue = value;
        }

        public void SetImmediate(double value)
        {
            targetValue = value;
            displayedValue = value;
            UpdateLabel();
        }

        private void Update()
        {
            if (System.Math.Abs(displayedValue - targetValue) < 0.5)
            {
                displayedValue = targetValue;
            }
            else
            {
                displayedValue = displayedValue + (targetValue - displayedValue) * lerpSpeed * Time.deltaTime;
            }
            UpdateLabel();
        }

        private void UpdateLabel()
        {
            if (label != null)
                label.text = NumberFormatter.FormatCompact(displayedValue) + " Gold";
        }
    }
}
```

**Step 3: Verify no compile errors**

Use MCP `check_compile_errors`.

**Step 4: Commit**

```
git add Assets/Scripts/UI/Starquill.UI.asmdef Assets/Scripts/UI/GoldCounterAnimator.cs
git commit -m "Update UI assembly refs, add GoldCounterAnimator"
```

---

### Task 5: DamageNumberSpawner

**Files:**
- Create: `Assets/Scripts/UI/DamageNumberSpawner.cs`

**Step 1: Create DamageNumberSpawner**

Create `Assets/Scripts/UI/DamageNumberSpawner.cs`:

```csharp
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Starquill.UI
{
    public class DamageNumberSpawner : MonoBehaviour
    {
        [SerializeField] private int poolSize = 20;
        [SerializeField] private float floatDistance = 100f;
        [SerializeField] private float duration = 1f;

        private readonly List<TMP_Text> pool = new();
        private int nextIndex;

        private void Awake()
        {
            for (int i = 0; i < poolSize; i++)
            {
                var obj = new GameObject($"DmgNum_{i}", typeof(RectTransform));
                obj.transform.SetParent(transform, false);
                var tmp = obj.AddComponent<TextMeshProUGUI>();
                tmp.fontSize = 28;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;
                obj.SetActive(false);
                pool.Add(tmp);
            }
        }

        public void SpawnDamage(Vector2 position, float damage, Color color, bool isVerbHit = false)
        {
            var tmp = pool[nextIndex];
            nextIndex = (nextIndex + 1) % poolSize;

            tmp.text = ((int)damage).ToString();
            tmp.color = color;
            tmp.fontSize = isVerbHit ? 32 : 24;

            var rt = tmp.GetComponent<RectTransform>();
            rt.anchoredPosition = position;

            tmp.gameObject.SetActive(true);
            StartCoroutine(AnimateFloat(tmp, position));
        }

        public void SpawnGold(Vector2 position, double amount)
        {
            var tmp = pool[nextIndex];
            nextIndex = (nextIndex + 1) % poolSize;

            tmp.text = NumberFormatter.FormatGoldDrop(amount);
            tmp.color = new Color(1f, 0.84f, 0f);
            tmp.fontSize = 28;

            var rt = tmp.GetComponent<RectTransform>();
            rt.anchoredPosition = position;

            tmp.gameObject.SetActive(true);
            StartCoroutine(AnimateFloat(tmp, position));
        }

        private IEnumerator AnimateFloat(TMP_Text tmp, Vector2 startPos)
        {
            float elapsed = 0f;
            var rt = tmp.GetComponent<RectTransform>();
            Color startColor = tmp.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                rt.anchoredPosition = startPos + Vector2.up * (floatDistance * t);
                tmp.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
                yield return null;
            }

            tmp.gameObject.SetActive(false);
        }
    }
}
```

**Step 2: Verify no compile errors**

Use MCP `check_compile_errors`.

**Step 3: Commit**

```
git add Assets/Scripts/UI/DamageNumberSpawner.cs
git commit -m "Add DamageNumberSpawner with object pool and float animation"
```

---

### Task 6: EnemyDisplayController

**Files:**
- Create: `Assets/Scripts/UI/EnemyDisplayController.cs`

**Step 1: Create EnemyDisplayController**

This component manages the enemy silhouettes, HP bars, and stat type tinting. It is placed on the EnemyContainer in the scene.

Create `Assets/Scripts/UI/EnemyDisplayController.cs`:

```csharp
using System.Collections;
using System.Collections.Generic;
using Starquill.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Starquill.UI
{
    public class EnemyDisplayController : MonoBehaviour
    {
        [SerializeField] private Image[] silhouettes;

        private readonly List<Image> hpBarFills = new();
        private readonly List<Image> hpBarBgs = new();
        private readonly List<float> displayedHP = new();
        private IReadOnlyList<EnemyState> enemies;

        public void SetupEnemies(IReadOnlyList<EnemyState> newEnemies)
        {
            ClearHPBars();
            enemies = newEnemies;

            int count = Mathf.Min(newEnemies.Count, silhouettes.Length);
            for (int i = 0; i < silhouettes.Length; i++)
            {
                if (i < count)
                {
                    silhouettes[i].gameObject.SetActive(true);
                    var enemy = newEnemies[i];

                    // Tint silhouette with stat type color
                    var statColor = StatTypeColors.GetColor(enemy.StatType);
                    silhouettes[i].color = new Color(
                        statColor.r * 0.4f + 0.15f,
                        statColor.g * 0.4f + 0.15f,
                        statColor.b * 0.4f + 0.15f,
                        0.8f);

                    // Create HP bar
                    CreateHPBar(silhouettes[i].transform, statColor, i);
                    displayedHP.Add(enemy.MaxHP);
                }
                else
                {
                    silhouettes[i].gameObject.SetActive(false);
                }
            }
        }

        private void CreateHPBar(Transform parent, Color fillColor, int index)
        {
            // Background
            var bgObj = new GameObject($"HPBarBg_{index}", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(parent, false);
            var bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0.1f, 0f);
            bgRT.anchorMax = new Vector2(0.9f, 0f);
            bgRT.pivot = new Vector2(0.5f, 1f);
            bgRT.anchoredPosition = new Vector2(0, -5);
            bgRT.sizeDelta = new Vector2(0, 12);
            bgObj.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            hpBarBgs.Add(bgObj.GetComponent<Image>());

            // Fill
            var fillObj = new GameObject($"HPBarFill_{index}", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(bgObj.transform, false);
            var fillRT = fillObj.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;
            fillRT.pivot = new Vector2(0f, 0.5f);
            var fillImg = fillObj.GetComponent<Image>();
            fillImg.color = fillColor;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 1f;
            hpBarFills.Add(fillImg);
        }

        private void Update()
        {
            if (enemies == null) return;
            int count = Mathf.Min(enemies.Count, hpBarFills.Count);
            for (int i = 0; i < count; i++)
            {
                var enemy = enemies[i];
                float targetFill = enemy.MaxHP > 0 ? enemy.CurrentHP / enemy.MaxHP : 0f;
                float currentFill = hpBarFills[i].fillAmount;
                hpBarFills[i].fillAmount = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * 8f);

                // Fade on death
                if (!enemy.IsAlive && silhouettes[i].color.a > 0.01f)
                {
                    var c = silhouettes[i].color;
                    c.a = Mathf.Lerp(c.a, 0f, Time.deltaTime * 4f);
                    silhouettes[i].color = c;

                    var bgC = hpBarBgs[i].color;
                    bgC.a = c.a;
                    hpBarBgs[i].color = bgC;

                    var fillC = hpBarFills[i].color;
                    fillC.a = c.a;
                    hpBarFills[i].color = fillC;
                }
            }
        }

        public Vector2 GetEnemyPosition(int index)
        {
            if (index < 0 || index >= silhouettes.Length) return Vector2.zero;
            return silhouettes[index].rectTransform.anchoredPosition;
        }

        private void ClearHPBars()
        {
            foreach (var bg in hpBarBgs)
                if (bg != null) Destroy(bg.gameObject);
            hpBarBgs.Clear();
            hpBarFills.Clear();
            displayedHP.Clear();
            enemies = null;
        }
    }
}
```

**Step 2: Verify no compile errors**

Use MCP `check_compile_errors`.

**Step 3: Commit**

```
git add Assets/Scripts/UI/EnemyDisplayController.cs
git commit -m "Add EnemyDisplayController with HP bars and stat type tinting"
```

---

### Task 7: VerbCardAnimator + VerbBarDisplay Interactive

**Files:**
- Create: `Assets/Scripts/UI/VerbCardAnimator.cs`
- Modify: `Assets/Scripts/UI/VerbBarDisplay.cs`

**Step 1: Create VerbCardAnimator**

Create `Assets/Scripts/UI/VerbCardAnimator.cs`:

```csharp
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Starquill.UI
{
    public class VerbCardAnimator : MonoBehaviour
    {
        [SerializeField] private float slideDuration = 0.3f;
        [SerializeField] private float slideOffset = 400f;

        public void AnimateCardIn(RectTransform card, int targetIndex, float cardWidth, float spacing, Action onComplete = null)
        {
            float targetX = targetIndex * (cardWidth + spacing);
            float startX = targetX + slideOffset;
            card.anchoredPosition = new Vector2(startX, card.anchoredPosition.y);
            StartCoroutine(SlideToPosition(card, targetX, onComplete));
        }

        public void AnimateSlideLeft(RectTransform card, int targetIndex, float cardWidth, float spacing, Action onComplete = null)
        {
            float targetX = targetIndex * (cardWidth + spacing);
            StartCoroutine(SlideToPosition(card, targetX, onComplete));
        }

        public void AnimateCardOut(RectTransform card, Action onComplete = null)
        {
            StartCoroutine(FadeOut(card, onComplete));
        }

        private IEnumerator SlideToPosition(RectTransform card, float targetX, Action onComplete)
        {
            float elapsed = 0f;
            float startX = card.anchoredPosition.x;
            float startY = card.anchoredPosition.y;

            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / slideDuration);
                card.anchoredPosition = new Vector2(
                    Mathf.Lerp(startX, targetX, t), startY);
                yield return null;
            }

            card.anchoredPosition = new Vector2(targetX, startY);
            onComplete?.Invoke();
        }

        private IEnumerator FadeOut(RectTransform card, Action onComplete)
        {
            var images = card.GetComponentsInChildren<Graphic>();
            float elapsed = 0f;
            float fadeDuration = slideDuration * 0.5f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - (elapsed / fadeDuration);
                foreach (var img in images)
                {
                    var c = img.color;
                    c.a = alpha;
                    img.color = c;
                }
                yield return null;
            }

            card.gameObject.SetActive(false);
            // Reset alpha for reuse
            foreach (var img in images)
            {
                var c = img.color;
                c.a = 1f;
                img.color = c;
            }
            onComplete?.Invoke();
        }
    }
}
```

**Step 2: Rewrite VerbBarDisplay for interactivity**

Replace `Assets/Scripts/UI/VerbBarDisplay.cs`:

```csharp
using System;
using System.Collections.Generic;
using TMPro;
using Starquill.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Starquill.UI
{
    public class VerbBarDisplay : MonoBehaviour
    {
        [SerializeField] private Transform cardContainer;
        [SerializeField] private Vector2 cardSize = new Vector2(300, 100);
        [SerializeField] private int maxCards = 3;

        private readonly List<GameObject> cards = new();
        private bool locked;

        public event Action<int> OnCardTapped;

        public void SetLocked(bool isLocked)
        {
            locked = isLocked;
            UpdateLockVisual();
        }

        public void RebuildFromSlots(IReadOnlyList<DrawnVerb> slots)
        {
            ClearCards();
            for (int i = 0; i < slots.Count && i < maxCards; i++)
            {
                var verb = slots[i];
                var color = StatTypeColors.GetColor(verb.Verb.statType);
                CreateCard(verb.Verb.displayName, color, i);
            }
        }

        public void PopulateWithPlaceholders()
        {
            ClearCards();

            var placeholders = new[]
            {
                ("Bash", new Color(0.9f, 0.5f, 0.1f)),
                ("Analyze", new Color(0.2f, 0.5f, 0.9f)),
                ("Slash", new Color(0.7f, 0.7f, 0.7f)),
            };

            for (int i = 0; i < placeholders.Length && i < maxCards; i++)
            {
                var (verbName, color) = placeholders[i];
                CreateCard(verbName, color, i);
            }
        }

        private void CreateCard(string verbName, Color bgColor, int index)
        {
            var parent = cardContainer != null ? cardContainer : transform;

            var card = new GameObject($"VerbCard_{cards.Count}");
            card.transform.SetParent(parent, false);

            var cardRT = card.AddComponent<RectTransform>();
            cardRT.sizeDelta = cardSize;

            var bg = card.AddComponent<Image>();
            bg.color = locked ? Color.Lerp(bgColor, Color.gray, 0.5f) : bgColor;

            var btn = card.AddComponent<Button>();
            int capturedIndex = index;
            btn.onClick.AddListener(() =>
            {
                if (!locked) OnCardTapped?.Invoke(capturedIndex);
            });

            var nameObj = new GameObject("VerbName");
            nameObj.transform.SetParent(card.transform, false);
            var nameRT = nameObj.AddComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 0.3f);
            nameRT.anchorMax = new Vector2(1, 1);
            nameRT.offsetMin = Vector2.zero;
            nameRT.offsetMax = Vector2.zero;
            var nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
            nameTMP.text = verbName;
            nameTMP.fontSize = 28;
            nameTMP.alignment = TextAlignmentOptions.Center;
            nameTMP.color = Color.white;
            nameTMP.raycastTarget = false;

            cards.Add(card);
        }

        private void UpdateLockVisual()
        {
            foreach (var card in cards)
            {
                if (card == null) continue;
                var img = card.GetComponent<Image>();
                if (img == null) continue;
                float gray = locked ? 0.5f : 0f;
                img.color = Color.Lerp(img.color, Color.gray, gray);
            }
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

**Step 3: Verify no compile errors**

Use MCP `check_compile_errors`.

**Step 4: Commit**

```
git add Assets/Scripts/UI/VerbCardAnimator.cs Assets/Scripts/UI/VerbBarDisplay.cs
git commit -m "Add VerbCardAnimator and make VerbBarDisplay interactive with tap callbacks"
```

---

### Task 8: ExploreSceneController Wiring

**Files:**
- Modify: `Assets/Scripts/UI/ExploreSceneController.cs`

**Step 1: Rewrite ExploreSceneController for GameManager wiring**

Replace `Assets/Scripts/UI/ExploreSceneController.cs` with the wired version:

```csharp
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Combat;
using Starquill.Core;
using Starquill.Data;
using Starquill.Display;
using Starquill.Managers;

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

        [Header("Enemy Display")]
        [SerializeField] private EnemyDisplayController enemyDisplay;

        [Header("Combat Feedback")]
        [SerializeField] private DamageNumberSpawner damageNumbers;
        [SerializeField] private GoldCounterAnimator goldCounter;

        private readonly List<CharacterDisplay> characterDisplays = new();
        private readonly List<Texture2D> placeholderTextures = new();
        private GameManager gm;
        private Coroutine verbLockCoroutine;

        private void Start()
        {
            SetupPlaceholderParallax();
            SetupPlaceholderUI();

            gm = GameManager.Instance;
            if (gm != null)
            {
                BindToGameManager();
            }
            else
            {
                // Fallback: no GameManager — show placeholders
                SetupPlaceholderParty();
                SetupPlaceholderEnemies();
            }
        }

        private void OnEnable()
        {
            if (gm != null) BindToGameManager();
        }

        private void OnDisable()
        {
            if (gm != null) UnbindFromGameManager();
        }

        private void BindToGameManager()
        {
            gm.OnCombatTick += HandleCombatTick;
            gm.OnGoldChanged += HandleGoldChanged;
            gm.OnWaveStarted += HandleWaveStarted;
            gm.OnWaveCleared += HandleWaveCleared;
            gm.OnVerbActivated += HandleVerbActivated;

            if (verbBar != null)
                verbBar.OnCardTapped += HandleVerbCardTapped;

            // Initialize UI from current state
            if (goldCounter != null)
                goldCounter.SetImmediate(gm.gold);
            if (topBar != null)
            {
                topBar.SetQuestLevel(gm.questLevel);
                topBar.SetWaveInfo(gm.Exploration.CurrentWave, 5);
            }

            // Refresh verb cards
            if (verbBar != null && gm.VerbPool != null)
                verbBar.RebuildFromSlots(gm.VerbPool.DrawnSlots);

            // Setup enemies from current wave
            if (enemyDisplay != null && gm.CurrentEnemies.Count > 0)
                enemyDisplay.SetupEnemies(gm.CurrentEnemies);
        }

        private void UnbindFromGameManager()
        {
            gm.OnCombatTick -= HandleCombatTick;
            gm.OnGoldChanged -= HandleGoldChanged;
            gm.OnWaveStarted -= HandleWaveStarted;
            gm.OnWaveCleared -= HandleWaveCleared;
            gm.OnVerbActivated -= HandleVerbActivated;

            if (verbBar != null)
                verbBar.OnCardTapped -= HandleVerbCardTapped;
        }

        private void HandleCombatTick(CombatTickResult result)
        {
            if (topBar != null && gm != null)
                topBar.SetWaveInfo(gm.Exploration.CurrentWave, 5);

            // Rebuild verb cards (cooldowns may have changed)
            if (verbBar != null && gm.VerbPool != null && !gm.VerbsLocked)
                verbBar.RebuildFromSlots(gm.VerbPool.DrawnSlots);
        }

        private void HandleGoldChanged(double newGold)
        {
            if (goldCounter != null)
                goldCounter.SetTarget(newGold);
        }

        private void HandleWaveStarted(IReadOnlyList<EnemyState> enemies)
        {
            if (enemyDisplay != null)
                enemyDisplay.SetupEnemies(enemies);
        }

        private void HandleWaveCleared()
        {
            if (topBar != null && gm != null)
                topBar.SetWaveInfo(gm.Exploration.CurrentWave, 5);
        }

        private void HandleVerbCardTapped(int slotIndex)
        {
            if (gm == null || gm.VerbsLocked) return;
            gm.OnVerbTapped(slotIndex);
        }

        private void HandleVerbActivated(int slotIndex, CombatTickResult result)
        {
            // Spawn damage numbers
            if (damageNumbers != null && gm != null)
            {
                // Distribute damage numbers across enemies
                var alive = gm.CurrentEnemies.Where(e => e.IsAlive || e.CurrentHP <= 0).ToList();
                if (alive.Count > 0 && enemyDisplay != null)
                {
                    for (int i = 0; i < alive.Count && i < 3; i++)
                    {
                        var pos = enemyDisplay.GetEnemyPosition(i);
                        if (result.TotalDamageDealt > 0)
                        {
                            var advColor = result.AdvantageHits > 0
                                ? StatTypeColors.GetAdvantageColor(Advantage.Strong)
                                : result.DisadvantageHits > 0
                                    ? StatTypeColors.GetAdvantageColor(Advantage.Weak)
                                    : Color.white;
                            damageNumbers.SpawnDamage(pos, result.TotalDamageDealt / alive.Count, advColor, true);
                        }
                    }
                }

                if (result.GoldEarned > 0 && enemyDisplay != null)
                {
                    var goldPos = enemyDisplay.GetEnemyPosition(0) + Vector2.up * 50;
                    damageNumbers.SpawnGold(goldPos, result.GoldEarned);
                }
            }

            // Lock verb bar
            if (verbBar != null)
            {
                verbBar.SetLocked(true);
                if (verbLockCoroutine != null) StopCoroutine(verbLockCoroutine);
                verbLockCoroutine = StartCoroutine(UnlockVerbBarAfterDelay());
            }
        }

        private IEnumerator UnlockVerbBarAfterDelay()
        {
            float lockDuration = gm != null ? gm.economyConfig.verbLockDuration : 3f;
            yield return new WaitForSeconds(lockDuration);
            if (verbBar != null)
            {
                verbBar.SetLocked(false);
                if (gm != null && gm.VerbPool != null)
                    verbBar.RebuildFromSlots(gm.VerbPool.DrawnSlots);
            }
        }

        // === PLACEHOLDER SETUP (no GameManager fallback) ===

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
            if (registry.Species.Count == 0)
                registry.LoadAll();
            var speciesKeys = registry.Species.Keys.ToList();
            if (speciesKeys.Count == 0) return;

            var builder = new DisplayBuilder(registry);
            var resolver = new ImageResolver();

            for (int i = 0; i < partySlots.Length && i < 4; i++)
            {
                if (partySlots[i] == null) continue;

                var speciesKey = speciesKeys[i % speciesKeys.Count];
                var speciesData = registry.Species[speciesKey];
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
            // Enemy silhouettes already have their color set by ExploreSceneBuilder
        }

        private void SetupPlaceholderUI()
        {
            if (topBar != null)
            {
                topBar.SetGold("1.2M");
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
                    if (stripeSpacing > 0 && x % stripeSpacing < stripeSpacing / 10)
                        c = Color.Lerp(c, Color.white, 0.15f);
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
            foreach (var display in characterDisplays)
                if (display != null) Destroy(display.gameObject);
            characterDisplays.Clear();

            foreach (var tex in placeholderTextures)
                if (tex != null) Destroy(tex);
            placeholderTextures.Clear();
        }
    }
}
```

**Step 2: Verify no compile errors**

Use MCP `check_compile_errors`.

**Step 3: Commit**

```
git add Assets/Scripts/UI/ExploreSceneController.cs
git commit -m "Wire ExploreSceneController to GameManager events with combat feedback"
```

---

### Task 9: Update ExploreSceneBuilder + Rebuild Scene

**Files:**
- Modify: `Assets/Editor/ExploreSceneBuilder.cs`

**Step 1: Update ExploreSceneBuilder to wire new components**

Add the following to `Assets/Editor/ExploreSceneBuilder.cs` in the `Execute()` method.

After the enemy container setup (~line 79) and before the FG_Grass creation, add the EnemyDisplayController:

```csharp
// EnemyDisplayController on enemyContainer
var enemyDisplayCtrl = enemyContainer.AddComponent<Starquill.UI.EnemyDisplayController>();
SetPrivateField(enemyDisplayCtrl, "silhouettes", new Image[] {
    enemy0.GetComponent<Image>(),
    enemy1.GetComponent<Image>(),
    enemy2.GetComponent<Image>()
});
```

After the VerbGrid creation (~line 149), add the VerbCardAnimator:

```csharp
var verbAnimator = verbBar.AddComponent<Starquill.UI.VerbCardAnimator>();
```

After the bottom nav section, add DamageNumberSpawner and GoldCounterAnimator:

```csharp
// DamageNumberSpawner on CombatAreaPanel
var damageSpawner = combatArea.AddComponent<Starquill.UI.DamageNumberSpawner>();

// GoldCounterAnimator on TopBarPanel
var goldAnimator = topBar.AddComponent<Starquill.UI.GoldCounterAnimator>();
SetPrivateField(goldAnimator, "label", goldLabel.GetComponent<TMPro.TMP_Text>());
```

Update the ExploreSceneController wiring section to include new fields:

```csharp
// Replace the existing SetPrivateField calls for controller with:
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
```

Remove the old `enemySilhouettes` wiring since EnemyDisplayController now owns those.

**Step 2: Open ExploreScene, run the builder, save**

```
# Via MCP:
# 1. open_scene("Assets/Scenes/ExploreScene.unity")
# 2. execute_script(ExploreSceneBuilder)
# 3. save_scene("Assets/Scenes/ExploreScene")
```

**Step 3: Verify no compile errors**

Use MCP `check_compile_errors`.

**Step 4: Commit**

```
git add Assets/Editor/ExploreSceneBuilder.cs Assets/Scenes/ExploreScene.unity
git commit -m "Update ExploreSceneBuilder with combat feedback components, rebuild scene"
```

---

### Task 10: Run Tests + Manual Verification

**Step 1: Run all edit mode tests in Unity Editor**

Open Unity Editor → Window → Test Runner → EditMode → Run All.
Expected: All tests pass (previous 75 + new VerbPool pass-count tests + StatTypeColors + NumberFormatter ≈ 88+).

**Step 2: Play mode verification checklist**

Enter Play Mode in ExploreScene and verify:
- [ ] Parallax layers scroll (placeholder textures)
- [ ] Party paper dolls render (if species data exists)
- [ ] Enemy silhouettes visible with stat type tint
- [ ] Verb cards display (placeholders if no GameManager in scene)
- [ ] Gold counter displays
- [ ] No console errors

**Note:** Full combat feedback (damage numbers, HP bars, verb tap interaction) requires a GameManager GameObject in the scene with EconomyConfig and AdvantageMatrix ScriptableObjects assigned. This can be tested by:
1. Creating a GameManager prefab with config assets
2. Adding it to ExploreScene
3. Creating test party members with verbs

This full integration test is optional for Sprint 4 — the components are wired and ready for when GameManager is placed in the scene.

**Step 3: Commit any fixes**

If any issues found during testing, fix and commit.

**Step 4: Final commit — Sprint 4 complete**

```
git add -A
git commit -m "Sprint 4: Minimum viable combat wiring complete"
```

---

### Task Summary

| Task | Description | Files | Tests |
|------|-------------|-------|-------|
| 1 | DrawnVerb PassCount + VerbPool events | 3 | 3 new |
| 2 | StatTypeColors + NumberFormatter | 4 | 8 new |
| 3 | EconomyConfig + GameManager singleton | 2 | — |
| 4 | UI asmdef + GoldCounterAnimator | 2 | — |
| 5 | DamageNumberSpawner | 1 | — |
| 6 | EnemyDisplayController | 1 | — |
| 7 | VerbCardAnimator + VerbBarDisplay | 2 | — |
| 8 | ExploreSceneController wiring | 1 | — |
| 9 | ExploreSceneBuilder + scene rebuild | 2 | — |
| 10 | Test + verify | — | Run all |

**Total: ~18 files touched, ~11 new tests, 10 commits**
