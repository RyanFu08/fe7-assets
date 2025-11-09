using UnityEngine;
using UnityEditor;

public class AggressiveTextureCompression : EditorWindow
{
    [MenuItem("Tools/Aggressive Texture Compression")]
    public static void AggressivelyCompressTextures()
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

                // Set maximum compression
                if (importer.textureCompression != TextureImporterCompression.CompressedHQ)
                {
                    importer.textureCompression = TextureImporterCompression.CompressedHQ;
                    changed = true;
                }
                // Reduce max size
                if (importer.maxTextureSize != 32)
                {
                    importer.maxTextureSize = 32;
                    changed = true;
                }
                // Optionally: turn off mipmaps (pixel art often doesn't need them)
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
        Debug.Log($"Aggressive compression applied to {count} textures.");
    }
}
