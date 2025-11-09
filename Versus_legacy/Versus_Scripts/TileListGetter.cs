using UnityEngine;
using System;
using System.IO;

public class TileListGetter : MonoBehaviour
{
    [Tooltip("Drag your cursor GameObject here")]
    public GameObject cc;
    public GameObject prefab;
    private string filePath;

    void Start()
    {
        // Write into persistentDataPath/positions.txt
        string userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string downloads = Path.Combine(userHome, "Downloads");
        filePath = Path.Combine(downloads, "positions.txt");
        Debug.Log($"PositionLogger will write to: {filePath}");

        // Optional: clear file at start of play session
        // File.WriteAllText(filePath, "");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.X) && cc != null)
        {
            Vector3 pos = cc.transform.position;
            // Format with two decimals and trailing comma
            string tuple = $"({(int)pos.x}, {(int)pos.y}),";
            File.AppendAllText(filePath, tuple);
            Debug.Log($"Logged position {tuple}");


            GameObject go = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            TileHighlight pf = go.GetComponent<TileHighlight>();
            Vector3 cpos = cc.transform.position;
            int tx = Mathf.RoundToInt(cpos.x);
            int ty = Mathf.RoundToInt(cpos.y);

            // Move & color
            pf.go_to(tx, ty);
            pf.set("blue");
        }
    }
}
