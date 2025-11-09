using UnityEngine;

public class ArrowControl : MonoBehaviour {
    private SpriteRenderer sr;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private int[] offsets;
    [SerializeField] private float delay = .1f;
    private float timer = 0f;
    private int cf = 0;

    void Start() {
        sr = GetComponent<SpriteRenderer>();
        hide();
    }

    void Update() {
        timer += Time.deltaTime;
        if (timer >= delay) {
            timer -= delay;
            ChangeYOffset(offsets[cf]);
            cf = (cf+1)%3;
            sr.sprite = frames[cf];
        }
    }

    public void show() {
        sr.enabled = true;
    }
    public void hide() {
        sr.enabled = false;
    }

    public void ChangeYOffset(int offset) {
        Vector3 pos = transform.position;
        pos.y += (float)offset;          // offset is an int, automatically converted to float
        transform.position = pos;
    }

}
