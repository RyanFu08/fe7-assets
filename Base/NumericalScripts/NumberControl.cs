using UnityEngine;
using System.Collections.Generic;

public class NumberControl : MonoBehaviour {
    public GameObject digitPrefab;
    public List<GameObject> cs = new List<GameObject>();
    public int n = -1, ln = -1;

    public float defaultSpacing = -6f;

    void Update() {
        if (n == ln) return;
        ln = n;
        if (n == -1) {
            foreach (GameObject go in cs) {go.GetComponent<DigitControl>().d = -1;}
        } else {
            string sn = n.ToString();
            while (cs.Count < sn.Length) {
                GameObject go = Instantiate(digitPrefab, transform);
                cs.Add(go);
            }
            for (int i=0; i<sn.Length; i++) {
                int val = int.Parse(sn[i].ToString());
                cs[i].GetComponent<DigitControl>().d = val;
                cs[i].GetComponent<DigitControl>().MoveLocalX(defaultSpacing*(sn.Length-i));
            }
            for (int i=sn.Length; i<cs.Count; i++) {
                cs[i].GetComponent<DigitControl>().d = -1;
            }

        }
    }
}
