using UnityEngine;
using UnityEditor;

public class RestoreTextureQuality : EditorWindow
{
    [MenuItem("Tools/Restore Texture Quality")]
    public static void RestoreFullQualityTextures()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
        int count = 0;
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            if (importer != null)
            {
                bool changed = false;

                // Remove compression
                if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    changed = true;
                }
                // Restore max size (change if you need larger/smaller)
                if (importer.maxTextureSize < 2048)
                {
                    importer.maxTextureSize = 2048;
                    changed = true;
                }
                // Disable mipmaps
                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    changed = true;
                }

                if (changed)
                {
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                    count++;
                }
            }
        }
        Debug.Log($"Restored full quality for {count} textures.");
    }
}
