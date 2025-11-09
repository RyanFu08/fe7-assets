using UnityEngine;

public class DigitControl : MonoBehaviour {
    private SpriteRenderer sr;
    private string sprite_path = "numerical/";
    public int d = -1, ld = -1;
    void Start() {
        sr = GetComponent<SpriteRenderer>();
    }
    void Update() {
        if (d == ld) return;
        ld = d;
        if (d == -1) {sr.enabled = false;}
        else {sr.enabled = true;}
        Sprite char_sprite = Resources.Load<Sprite>(sprite_path+d);
        sr.sprite = char_sprite;
    }
    public void MoveLocalX(float x) {
        Vector3 pos = transform.localPosition;
        pos.x = x;
        transform.localPosition = pos;
    }
}