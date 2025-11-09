using UnityEngine;

public class CharControl : MonoBehaviour {
    private SpriteRenderer sr;
    private string sprite_path = "font/font_";
    public char c = ' ';
    public bool is_highlighted = false;
    void Start() {
        sr = GetComponent<SpriteRenderer>();
       // Debug.Log("creating character!!");
    }
    void Update() {
        if (is_highlighted && c != ' ') sprite_path = "font_yellow/font_";
        else sprite_path = "font/font_";
        Sprite char_sprite = Resources.Load<Sprite>(sprite_path+(int)c);
        sr.sprite = char_sprite;
    }
    public void MoveLocalX(float x) {
        Vector3 pos = transform.localPosition;
        pos.x = x;
        transform.localPosition = pos;
    }
}