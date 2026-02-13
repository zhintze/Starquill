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

            cache[spritePath] = sprite;
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
