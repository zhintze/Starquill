using UnityEditor;
using UnityEngine;

public static class SpriteImportFixer
{
    [MenuItem("Tools/Fix Sprite Import Settings")]
    public static void FixAll()
    {
        string[] folders = {
            "Assets/Resources/Images/species",
            "Assets/Resources/Images/equipment",
            "Assets/Resources/Images/weapons"
        };

        int count = 0;
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", folders);

        Debug.Log("SpriteImportFixer: Found " + guids.Length + " textures to process...");

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                bool changed = false;

                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    changed = true;
                }

                if (importer.filterMode != FilterMode.Bilinear)
                {
                    importer.filterMode = FilterMode.Bilinear;
                    changed = true;
                }

                if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    changed = true;
                }

                if (importer.maxTextureSize != 256)
                {
                    importer.maxTextureSize = 256;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                    count++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.Refresh();
        Debug.Log("SpriteImportFixer: Updated " + count + " of " + guids.Length + " textures.");
    }
}
