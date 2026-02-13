using System.Collections.Generic;
using System.Linq;
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
            var speciesKeys = registry.Species.Keys.ToList();
            if (speciesKeys.Count == 0) return;

            var builder = new DisplayBuilder();
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
            foreach (var tex in placeholderTextures)
                if (tex != null) Destroy(tex);
            placeholderTextures.Clear();
        }
    }
}
