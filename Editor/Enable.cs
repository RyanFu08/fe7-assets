// Place this script in an Editor folder
using UnityEngine;
using UnityEditor;

public class Enable
{
    [MenuItem("Tools/Enable Read/Write On All Textures")]
    static void EnableReadWrite()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
                Debug.Log("Enabled Read/Write for: " + path);
            }
        }
        Debug.Log("All textures set to Read/Write.");
    }
}
