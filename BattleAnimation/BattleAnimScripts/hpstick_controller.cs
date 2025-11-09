using UnityEngine;

public class hpstick_controller : MonoBehaviour {
    private SpriteRenderer sr;
    public string state = "full";
    public Sprite full, empty;
    void Awake() {
        sr = GetComponent<SpriteRenderer>();
    }
    public void set_full() {
        state = "full";
        sr.sprite = full;
    }
    public void set_empty() {
        state = "empty";
        sr.sprite = empty;
    }
    public void MoveLocalX(float x) {
        Vector3 pos = transform.localPosition;
        pos.x = x;
        transform.localPosition = pos;
    }
    public void MoveLocalY(float y) {
        Vector3 pos = transform.localPosition;
        pos.y = y;
        transform.localPosition = pos;
    }
}