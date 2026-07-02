using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Combat;
using Starquill.Core;
using Starquill.Data;
using Starquill.Display;
using Starquill.Equipment;
using Starquill.Managers;
using Starquill.Characters;

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
        private ScreenManager screenManager;
        private Coroutine verbLockCoroutine;
        private RectTransform spawnerRT;
        private bool isMuted;

        private void Start()
        {
            SetupPlaceholderParallax();
            CreateDungeonHud();

            if (damageNumbers != null)
                spawnerRT = (RectTransform)damageNumbers.transform;

            // Bottom nav buttons are always needed for screen switching
            if (bottomNav != null)
            {
                bottomNav.CreatePlaceholderButtons();
                bottomNav.SetActiveTab(0);
            }

            // Self-mute when not on explore tab (ScreenManager is on this Canvas)
            screenManager = GetComponent<ScreenManager>();
            if (screenManager != null)
                screenManager.OnScreenChanged += HandleScreenChanged;

            gm = GameManager.Instance;
            if (gm != null)
            {
                BindToGameManager();
                StartCoroutine(DeferredInitialSync());
            }
            else
            {
                SetupPlaceholderParty();
                SetupPlaceholderUI();
            }
        }

        /// Runtime-built (no scene rebuild needed): sits in the quest banner's
        /// column, one slot below it, so an in-run strip never overlaps a
        /// retreated-quest retry notice. DungeonHudDisplay hides itself
        /// whenever no dungeon is active.
        private void CreateDungeonHud()
        {
            var banner = GetComponentInChildren<QuestBannerDisplay>(true);
            var parent = banner != null ? banner.transform.parent : transform;

            var hud = new GameObject("DungeonHud", typeof(RectTransform));
            hud.transform.SetParent(parent, false);
            var rt = (RectTransform)hud.transform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            // QuestBanner occupies -72..-172; leave a Space1 gap below it.
            rt.anchoredPosition = new Vector2(0, -180f);
            rt.sizeDelta = new Vector2(-32f, 120f);
            hud.AddComponent<DungeonHudDisplay>();
        }

        private IEnumerator DeferredInitialSync()
        {
            // Wait one frame so GameManager.Start() has run
            yield return null;

            if (gm == null) yield break;

            OfflineClaimSheet.ShowIfPending(transform.root);

            SetupPlaceholderParty();

            if (goldCounter != null)
                goldCounter.SetImmediate(gm.gold);
            if (topBar != null)
            {
                topBar.SetQuestLevel(gm.questLevel);
                topBar.SetWaveInfo(gm.Exploration.CurrentWave, 0);
            }
            if (enemyDisplay != null && gm.CurrentEnemies.Count > 0)
                enemyDisplay.SetupEnemies(gm.CurrentEnemies);
            if (verbBar != null && gm.VerbPool != null)
                verbBar.RebuildFromSlots(gm.VerbPool.DrawnSlots);
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

        public void SetMuted(bool muted)
        {
            isMuted = muted;
        }

        private void HandleScreenChanged(int screenIndex)
        {
            isMuted = screenIndex != 0;
        }

        private void HandleCombatTick(CombatTickResult result)
        {
            if (isMuted) return;

            if (topBar != null && gm != null)
                topBar.SetWaveInfo(gm.Exploration.CurrentWave, 0);

            if (verbBar != null && gm.VerbPool != null && !gm.VerbsLocked)
                verbBar.RebuildFromSlots(gm.VerbPool.DrawnSlots);

            // Spawn auto-attack damage numbers
            if (damageNumbers != null && enemyDisplay != null && result.TotalDamageDealt > 0)
            {
                int aliveCount = 0;
                for (int i = 0; i < gm.CurrentEnemies.Count; i++)
                    if (gm.CurrentEnemies[i].IsAlive) aliveCount++;

                if (aliveCount > 0)
                {
                    float perEnemy = result.TotalDamageDealt / aliveCount;
                    for (int i = 0; i < gm.CurrentEnemies.Count && i < 3; i++)
                    {
                        if (gm.CurrentEnemies[i].IsAlive)
                        {
                            var pos = EnemyPositionInSpawnerSpace(i);
                            damageNumbers.SpawnDamage(pos, perEnemy, Color.white, false);
                        }
                    }
                }
            }
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
                topBar.SetWaveInfo(gm.Exploration.CurrentWave, 0);
        }

        private void HandleVerbCardTapped(int slotIndex)
        {
            if (gm == null || gm.VerbsLocked) return;
            gm.OnVerbTapped(slotIndex);
        }

        private void HandleVerbActivated(int slotIndex, CombatTickResult result)
        {
            if (isMuted) return;

            if (damageNumbers != null && gm != null && enemyDisplay != null)
            {
                var alive = gm.CurrentEnemies.Where(e => e.IsAlive || e.CurrentHP <= 0).ToList();
                if (alive.Count > 0)
                {
                    for (int i = 0; i < alive.Count && i < 3; i++)
                    {
                        var pos = EnemyPositionInSpawnerSpace(i);
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

                if (result.GoldEarned > 0)
                {
                    var goldPos = EnemyPositionInSpawnerSpace(0) + Vector2.up * 50;
                    damageNumbers.SpawnGold(goldPos, result.GoldEarned);
                }
            }

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

        private Vector2 EnemyPositionInSpawnerSpace(int index)
        {
            Vector3 worldPos = enemyDisplay.GetEnemyWorldPosition(index);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                spawnerRT, RectTransformUtility.WorldToScreenPoint(null, worldPos),
                null, out var localPoint);
            return localPoint;
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

            var builder = new DisplayBuilder(registry);
            var resolver = new ImageResolver();

            CharacterInstance[] activeParty = null;
            if (gm != null && gm.Roster != null)
                activeParty = gm.Roster.GetActiveParty();

            var speciesKeys = registry.Species.Keys.ToList();
            if (speciesKeys.Count == 0) return;

            for (int i = 0; i < partySlots.Length && i < 4; i++)
            {
                if (partySlots[i] == null) continue;

                string speciesKey;
                EquipmentInstance[] equipment = null;

                if (activeParty != null && activeParty[i] != null)
                {
                    speciesKey = activeParty[i].speciesId;
                    equipment = activeParty[i].equipment;
                }
                else
                {
                    speciesKey = speciesKeys[i % speciesKeys.Count];
                }

                if (!registry.Species.TryGetValue(speciesKey, out var speciesData))
                    speciesData = registry.Species[speciesKeys[i % speciesKeys.Count]];

                SpeciesInstanceData instance;
                if (activeParty != null && activeParty[i] != null)
                    instance = activeParty[i].GetOrCreateAppearance(speciesData, registry);
                else
                    instance = SpeciesInstanceData.CreateFrom(speciesData, registry);

                var equipDisplayList = new List<EquipmentDisplayInfo>();
                if (equipment != null)
                {
                    foreach (var eq in equipment)
                    {
                        if (eq == null) continue;
                        var info = new EquipmentDisplayInfo
                        {
                            ItemType = eq.ItemType,
                            ItemNum = eq.ItemNum,
                            BaseColor = eq.BaseColor,
                            VarianceColors = eq.VarianceColors as Dictionary<int, Color>
                                ?? new Dictionary<int, Color>(eq.VarianceColors),
                            IsOffhand = eq.Slot == EquipmentSlot.OffHand,
                            LayerVariants = eq.LayerVariants
                        };
                        equipDisplayList.Add(info);
                    }
                }

                var pieces = builder.Build(instance, speciesData, equipDisplayList);

                var displayObj = new GameObject($"CharDisplay_{i}");
                displayObj.transform.SetParent(transform);
                var display = displayObj.AddComponent<CharacterDisplay>();
                display.Initialize(resolver);
                display.SetPieces(pieces);

                partySlots[i].texture = display.Texture;
                characterDisplays.Add(display);
            }
        }

        private void SetupPlaceholderUI()
        {
            if (topBar != null)
            {
                topBar.SetGold("1.2M");
                topBar.SetFragments(7, 12);
                topBar.SetWaveInfo(3, 0);
                topBar.SetQuestLevel(34);
            }

            if (verbBar != null)
                verbBar.PopulateWithPlaceholders();

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
            if (screenManager != null)
                screenManager.OnScreenChanged -= HandleScreenChanged;

            foreach (var display in characterDisplays)
                if (display != null) Destroy(display.gameObject);
            characterDisplays.Clear();

            foreach (var tex in placeholderTextures)
                if (tex != null) Destroy(tex);
            placeholderTextures.Clear();
        }
    }
}
