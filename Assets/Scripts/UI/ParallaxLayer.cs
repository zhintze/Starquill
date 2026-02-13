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
