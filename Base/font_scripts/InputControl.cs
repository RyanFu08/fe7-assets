using UnityEngine;
using System.Collections.Generic;

public class InputControl : MonoBehaviour {
    public GameObject charPrefab;
    public List<GameObject> cs = new List<GameObject>();
    [Tooltip("Enable text input when true")]
    public bool is_selected = false;
    [Tooltip("Current text")]
    public string s = "";
    [Tooltip("Max characters allowed")]
    public int maxLength = 10;

    public float defaultSpacing = 6f;
    public float iSpacing = 2f;
    public float lSpacing = 2f;
    public float fSpacing = 4f;
    public float rSpacing = 4f;
    public float tSpacing = 4f;

    Dictionary<char, float> kerning;

    void Awake() {
        kerning = new Dictionary<char, float> {
            {'i', iSpacing},
            {'l', lSpacing},
            {'f', fSpacing},
            {'r', rSpacing},
            {'t', tSpacing}
        };
    }

    void Update() {
        if (is_selected) {
            foreach (char c in Input.inputString) {
                if (c == '\b') {
                    // backspace
                    if (s.Length > 0)
                        s = s.Substring(0, s.Length - 1);
                }
                else if (s.Length < maxLength) {
                    // Only allow a-z, A-Z, 0-9
                    if ((c >= 'a' && c <= 'z') || 
                        (c >= 'A' && c <= 'Z') || 
                        (c >= '0' && c <= '9')) 
                    {
                        s += c;
                    }
                }
            }
        }

        while (cs.Count < s.Length) {
            GameObject newChar = Instantiate(charPrefab, transform);
            cs.Add(newChar);
        }

        // Update each CharControl
        for (int i = 0; i < cs.Count; i++) {
            CharControl cc = cs[i].GetComponent<CharControl>();
            if (i < s.Length) {
                cc.c = s[i];
                cs[i].SetActive(true);
            } else {
                cc.c = ' ';
                cs[i].SetActive(false);
            }
        }

        // Position with kerning
        float x = 0f;
        for (int i = 0; i < s.Length; i++) {
            cs[i].GetComponent<CharControl>().MoveLocalX(x);
            float spacing = kerning.ContainsKey(s[i]) ? kerning[s[i]] : defaultSpacing;
            x += spacing;
        }
    }
}
