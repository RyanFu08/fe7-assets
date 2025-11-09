using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public static class IntPairStorage
{
    public static string GetPath(string fileName)
    {
        return Path.Combine(Application.persistentDataPath, fileName);
    }

    // Saves as plain text: one pair per line -> "a,b"
    // Logs the full content and the destination path.
    public static void Save(string fileName, List<(int,int)> data, bool log = true)
    {
        string path = GetPath(fileName);
        string dir  = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        int count = data?.Count ?? 0;
        var sb = new StringBuilder(Mathf.Max(count * 8, 128)); // rough capacity

        for (int i = 0; i < count; i++)
        {
            var (a, b) = data[i];
            sb.Append(a).Append(',').Append(b).Append('\n');
        }

        string content = sb.ToString();
        File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        if (log)
        {
            Debug.Log($"[IntPairTextStorage] Saved {count} pairs to:\n{path}\n--- BEGIN FILE ---\n{content}--- END FILE ---");
        }
    }

    // Loads from persistentDataPath or falls back to Resources.
    public static List<(int,int)> Load(string fileName, bool log = true, bool tryResources = true)
    {
        string path = GetPath(fileName);
        var list = new List<(int,int)>(256);

        if (File.Exists(path))
        {
            // Load from persistentDataPath
            ParseLines(File.ReadLines(path), list, log, path);
        }
        else if (tryResources)
        {
            // Strip extension for Resources.Load
            string resourceName = Path.GetFileNameWithoutExtension(fileName);
            TextAsset asset = Resources.Load<TextAsset>(resourceName);

            if (asset == null)
                throw new FileNotFoundException($"No file at {path} and no TextAsset in Resources named {resourceName}");

            using (var sr = new StringReader(asset.text))
            {
                var lines = ReadLines(sr);
                ParseLines(lines, list, log, $"Resources/{resourceName}");
            }
        }
        else
        {
            throw new FileNotFoundException($"No file at {path}", path);
        }

        return list;
    }

    // Shared parsing logic
    private static void ParseLines(IEnumerable<string> lines, List<(int,int)> list, bool log, string source)
    {
        int lineNo = 0;

        foreach (var raw in lines)
        {
            lineNo++;
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;

            // Accept "a,b" or "a b"
            string[] parts = line.IndexOf(',') >= 0
                ? line.Split(',')
                : line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
            {
                Debug.LogWarning($"[IntPairTextStorage] Skip line {lineNo}: expected 2 values -> \"{line}\"");
                continue;
            }

            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int a) ||
                !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int b))
            {
                Debug.LogWarning($"[IntPairTextStorage] Skip line {lineNo}: parse failed -> \"{line}\"");
                continue;
            }

            list.Add((a, b));
        }

        if (log)
            Debug.Log($"[IntPairTextStorage] Loaded {list.Count} pairs from:\n{source}");
    }

    // Utility: enumerate lines from a StringReader
    private static IEnumerable<string> ReadLines(StringReader sr)
    {
        string line;
        while ((line = sr.ReadLine()) != null)
            yield return line;
    }

#if UNITY_EDITOR
    // Convenience: open the save folder in Finder
    [UnityEditor.MenuItem("Tools/Show Persistent Data Folder")]
    private static void ShowPersistentDataFolder()
    {
        UnityEditor.EditorUtility.RevealInFinder(Application.persistentDataPath);
    }

    // Optional: Save to Resources (Editor only). 
    // Lets you create default seed files under Assets/Resources/
    public static void SaveToResources(string fileName, List<(int,int)> data)
    {
        string path = Path.Combine(Application.dataPath, "Resources", fileName);
        string dir  = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        int count = data?.Count ?? 0;
        var sb = new StringBuilder(Mathf.Max(count * 8, 128));
        for (int i = 0; i < count; i++)
        {
            var (a, b) = data[i];
            sb.Append(a).Append(',').Append(b).Append('\n');
        }

        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        Debug.Log($"[IntPairTextStorage] Wrote {count} pairs to Resources:\n{path}");
    }
#endif
}
