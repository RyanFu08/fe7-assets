using UnityEngine;
using System.Collections.Generic;

public class StringControl : MonoBehaviour {
    public GameObject charPrefab;
    public List<GameObject> cs = new List<GameObject>();
    public string s = "";

    public bool yellow = false;

    [Header("Default Spacing")]
    public float defaultSpacing = 6f;

    [Header("Lowercase Kerning (a-z)")]
    public float aSpacing = 6f, bSpacing = 6f, cSpacing = 6f, dSpacing = 6f, eSpacing = 6f,
                 fSpacing = 6f, gSpacing = 6f, hSpacing = 6f, iSpacing = 6f, jSpacing = 6f,
                 kSpacing = 6f, lSpacing = 6f, mSpacing = 6f, nSpacing = 6f, oSpacing = 6f,
                 pSpacing = 6f, qSpacing = 6f, rSpacing = 6f, sSpacing = 6f, tSpacing = 6f,
                 uSpacing = 6f, vSpacing = 6f, wSpacing = 6f, xSpacing = 6f, ySpacing = 6f, zSpacing = 6f;

    [Header("Uppercase Kerning (A-Z)")]
    public float ASpacing = 6f, BSpacing = 6f, CSpacing = 6f, DSpacing = 6f, ESpacing = 6f,
                 FSpacing = 6f, GSpacing = 6f, HSpacing = 6f, ISpacing = 6f, JSpacing = 6f,
                 KSpacing = 6f, LSpacing = 6f, MSpacing = 6f, NSpacing = 6f, OSpacing = 6f,
                 PSpacing = 6f, QSpacing = 6f, RSpacing = 6f, SSpacing = 6f, TSpacing = 6f,
                 USpacing = 6f, VSpacing = 6f, WSpacing = 6f, XSpacing = 6f, YSpacing = 6f, ZSpacing = 6f;

    Dictionary<char, float> kerning;

    void Awake() {
        kerning = new Dictionary<char, float> {
            // Lowercase
            {'a', aSpacing}, {'b', bSpacing}, {'c', cSpacing}, {'d', dSpacing}, {'e', eSpacing},
            {'f', fSpacing}, {'g', gSpacing}, {'h', hSpacing}, {'i', iSpacing}, {'j', jSpacing},
            {'k', kSpacing}, {'l', lSpacing}, {'m', mSpacing}, {'n', nSpacing}, {'o', oSpacing},
            {'p', pSpacing}, {'q', qSpacing}, {'r', rSpacing}, {'s', sSpacing}, {'t', tSpacing},
            {'u', uSpacing}, {'v', vSpacing}, {'w', wSpacing}, {'x', xSpacing}, {'y', ySpacing}, {'z', zSpacing},

            // Uppercase
            {'A', ASpacing}, {'B', BSpacing}, {'C', CSpacing}, {'D', DSpacing}, {'E', ESpacing},
            {'F', FSpacing}, {'G', GSpacing}, {'H', HSpacing}, {'I', ISpacing}, {'J', JSpacing},
            {'K', KSpacing}, {'L', LSpacing}, {'M', MSpacing}, {'N', NSpacing}, {'O', OSpacing},
            {'P', PSpacing}, {'Q', QSpacing}, {'R', RSpacing}, {'S', SSpacing}, {'T', TSpacing},
            {'U', USpacing}, {'V', VSpacing}, {'W', WSpacing}, {'X', XSpacing}, {'Y', YSpacing}, {'Z', ZSpacing}
        };
    }

    void Update() {
        
        kerning = new Dictionary<char, float> {
            // Lowercase
            {'a', aSpacing}, {'b', bSpacing}, {'c', cSpacing}, {'d', dSpacing}, {'e', eSpacing},
            {'f', fSpacing}, {'g', gSpacing}, {'h', hSpacing}, {'i', iSpacing}, {'j', jSpacing},
            {'k', kSpacing}, {'l', lSpacing}, {'m', mSpacing}, {'n', nSpacing}, {'o', oSpacing},
            {'p', pSpacing}, {'q', qSpacing}, {'r', rSpacing}, {'s', sSpacing}, {'t', tSpacing},
            {'u', uSpacing}, {'v', vSpacing}, {'w', wSpacing}, {'x', xSpacing}, {'y', ySpacing}, {'z', zSpacing},

            // Uppercase
            {'A', ASpacing}, {'B', BSpacing}, {'C', CSpacing}, {'D', DSpacing}, {'E', ESpacing},
            {'F', FSpacing}, {'G', GSpacing}, {'H', HSpacing}, {'I', ISpacing}, {'J', JSpacing},
            {'K', KSpacing}, {'L', LSpacing}, {'M', MSpacing}, {'N', NSpacing}, {'O', OSpacing},
            {'P', PSpacing}, {'Q', QSpacing}, {'R', RSpacing}, {'S', SSpacing}, {'T', TSpacing},
            {'U', USpacing}, {'V', VSpacing}, {'W', WSpacing}, {'X', XSpacing}, {'Y', YSpacing}, {'Z', ZSpacing},
            {' ', 2f}, {'.', 2f}
        };


        while (cs.Count < s.Length) {
            GameObject newChar = Instantiate(charPrefab, transform);
            cs.Add(newChar);
        }

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

        float x = 0f;
        for (int i = 0; i < s.Length; i++) {
            cs[i].GetComponent<CharControl>().MoveLocalX(x);
            float spacing = kerning.ContainsKey(s[i]) ? kerning[s[i]] : defaultSpacing;
            x += spacing;
        }

        if (yellow) {
            for (int i = 0; i < s.Length; i++) {
                cs[i].GetComponent<CharControl>().is_highlighted = true;
            }
        } else {
            for (int i = 0; i < s.Length; i++) {
                cs[i].GetComponent<CharControl>().is_highlighted = false;
            }
        }

    }
}
