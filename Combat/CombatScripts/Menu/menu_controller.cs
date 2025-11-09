using System;
using System.Collections.Generic;
using UnityEngine;

public class menu_controller : MonoBehaviour {
    private SpriteRenderer sr;
    [SerializeField] private GameObject string_prefab;
    [SerializeField] private GameObject cursor;

    [SerializeField] private Sprite[] menu_sprites;   // index 0 → 1‑option sprite, …, 4 → 5‑option sprite
    private int state = 0; // 0 = hidden, 1…5 = that many options
    private List<GameObject> strings = new List<GameObject>();

    private int cselect = 0;
    public int selection = -1;

    private bool just_spawn = false;

    void Awake() {
        sr = GetComponent<SpriteRenderer>();
    }

    void Start() {
        state = 0;
        sr.enabled = false;
    }

    void Update() {
        if (state == 0) return;
        Update_Highlight();
        RespondToArrowKeys();
        just_spawn = false;
    }

    private void RespondToArrowKeys() {
        if (Input.GetKeyDown(KeyCode.UpArrow)) {
            cselect = (cselect - 1 + strings.Count) % strings.Count;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow)) {
            cselect = (cselect + 1) % strings.Count;
        }
        if (!just_spawn && Input.GetKeyDown(KeyCode.X)) {
            display_menu(new List<string>());
            selection = cselect;
            cursor.GetComponent<cursor_input>().activate();
        } else if (Input.GetKeyDown(KeyCode.Z)) {
            display_menu(new List<string>());
            selection = -2;
            cursor.GetComponent<cursor_input>().activate();
        }
    }

    private void Update_Highlight() {
        for (int i=0; i<strings.Count; i++) {
            strings[i].GetComponent<StringControl>().yellow = false;
            strings[i].transform.localScale = new Vector3(1f,1f,1f);
        }
        strings[cselect].GetComponent<StringControl>().yellow = true;
        strings[cselect].transform.localScale = new Vector3(1f,1f,1f);
    }

    /*** PUBLIC ***/

    public void display_menu(List<string> opt) {

        if (opt.Count == 0) {
            sr.enabled = false;
            foreach (GameObject go in strings) {Destroy(go);}
            strings.Clear();
            state = 0;
            return;
        }

        /* sprite stuff */
        Vector3 pos = cursor.transform.position;
        transform.position = new Vector3(pos.x + 8f, pos.y + 8f, 0f);
        sr.sprite  = menu_sprites[opt.Count];
        sr.enabled = true;
        state = opt.Count;


        /* string stuff */
        foreach (GameObject go in strings) {
            Destroy(go);
        }
        strings.Clear();
        for (int i=0; i<opt.Count; i++) {
            Debug.Log(i);
            GameObject go = GameObject.Instantiate(string_prefab,
                                                   new Vector3(pos.x+20f, pos.y-5f-16*i, 0f),
                                                   Quaternion.identity);
            go.GetComponent<StringControl>().s = opt[i];
            Debug.Log(opt[i]);
            strings.Add(go);
        }

        cselect = 0; selection = -1;
        cursor.GetComponent<cursor_input>().deactivate();

        just_spawn = true;
    }
}
