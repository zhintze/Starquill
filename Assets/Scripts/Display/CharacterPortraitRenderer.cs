using System.Collections.Generic;
using UnityEngine;

namespace Starquill.Display
{
    public class CharacterPortraitRenderer : MonoBehaviour
    {
        private RenderTexture renderTexture;
        private Camera compositingCamera;
        private Transform compositingRoot;
        private readonly List<SpriteRenderer> spritePool = new();
        private ImageResolver imageResolver;
        private int compositingLayer = 31;

        public RenderTexture Texture => renderTexture;
        public float CameraYOffset => compositingCamera != null ? compositingCamera.transform.localPosition.y : 0f;
        public float CameraOrthoSize => compositingCamera != null ? compositingCamera.orthographicSize : 1f;

        public void Initialize(ImageResolver resolver, int textureSize = 100)
        {
            imageResolver = resolver;

            var camObj = new GameObject("PortraitCamera");
            camObj.transform.SetParent(transform);
            camObj.transform.localPosition = new Vector3(0, 0, -10);

            compositingCamera = camObj.AddComponent<Camera>();
            compositingCamera.orthographic = true;
            compositingCamera.orthographicSize = 0.5f;
            compositingCamera.cullingMask = 1 << compositingLayer;
            compositingCamera.clearFlags = CameraClearFlags.SolidColor;
            compositingCamera.backgroundColor = new Color(0, 0, 0, 0);
            compositingCamera.enabled = false;

            compositingRoot = new GameObject("PortraitRoot").transform;
            compositingRoot.SetParent(transform);
            compositingRoot.localPosition = Vector3.zero;

            renderTexture = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGB32);
            renderTexture.filterMode = FilterMode.Bilinear;
            renderTexture.Create();
        }

        public void SetHeadCrop(float yOffset, float zoom)
        {
            if (compositingCamera == null) return;
            var pos = compositingCamera.transform.localPosition;
            pos.y = yOffset;
            compositingCamera.transform.localPosition = pos;
            compositingCamera.orthographicSize = zoom;
        }

        public void SetPieces(List<DisplayPiece> pieces)
        {
            if (compositingCamera == null || imageResolver == null) return;

            while (spritePool.Count < pieces.Count)
                CreatePooledRenderer();

            compositingRoot.gameObject.SetActive(true);

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

            for (int i = pieces.Count; i < spritePool.Count; i++)
                spritePool[i].gameObject.SetActive(false);

            compositingCamera.targetTexture = renderTexture;
            compositingCamera.Render();
            compositingRoot.gameObject.SetActive(false);
        }

        public void RebuildFromData(
            SpeciesInstanceData speciesInstance,
            SpeciesDisplayData speciesData,
            List<EquipmentDisplayInfo> equipment,
            DisplayBuilder builder)
        {
            var pieces = builder.Build(speciesInstance, speciesData, equipment);
            SetPieces(pieces);
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
    }
}
