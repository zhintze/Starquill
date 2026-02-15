using NUnit.Framework;
using UnityEngine;
using Starquill.Display;

namespace Starquill.Tests.Display
{
    public class CharacterPortraitRendererTests
    {
        [Test]
        public void Initialize_CreatesRenderTexture()
        {
            var go = new GameObject("Portrait");
            var renderer = go.AddComponent<CharacterPortraitRenderer>();
            renderer.Initialize(new ImageResolver(), 100);

            Assert.IsNotNull(renderer.Texture);
            Assert.AreEqual(100, renderer.Texture.width);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void SetHeadCrop_AdjustsCameraPosition()
        {
            var go = new GameObject("Portrait");
            var renderer = go.AddComponent<CharacterPortraitRenderer>();
            renderer.Initialize(new ImageResolver(), 100);
            renderer.SetHeadCrop(0.5f, 0.4f);

            Assert.AreEqual(0.5f, renderer.CameraYOffset, 0.01f);
            Assert.AreEqual(0.4f, renderer.CameraOrthoSize, 0.01f);

            Object.DestroyImmediate(go);
        }
    }
}
