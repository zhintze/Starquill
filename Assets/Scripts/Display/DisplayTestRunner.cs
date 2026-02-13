using UnityEngine;
using UnityEngine.UI;

namespace Starquill.Display
{
    public class DisplayTestRunner : MonoBehaviour
    {
        [SerializeField] private CharacterDisplay characterDisplay;
        [SerializeField] private RawImage outputImage;

        private void Start()
        {
            var registry = DisplayDataRegistry.Instance;
            registry.LoadAll();

            var imageResolver = new ImageResolver();
            characterDisplay.Initialize(imageResolver);

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
