using UnityEngine;

public class tile_control : MonoBehaviour {
    
    [SerializeField] private Sprite[] red_frames;
    [SerializeField] private Sprite[] blue_frames;
    private SpriteRenderer sr;

    [SerializeField] public string state = "hide";
    [SerializeField] public string meta_state = "static";
    [SerializeField] private float frameRate = 10f;

    void Start() {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update() {
//        if (meta_state == "")


        float g = Time.time;

        if (state == "hide" || state == "queued") {
            sr.enabled = false;
        } else if (state == "blue") {
             sr.enabled = true;
            int fx = (int) (g * frameRate) % blue_frames.Length;
            sr.sprite = blue_frames[fx];
        } else if (state == "red") {
             sr.enabled = true;
            int fx = (int) (g * frameRate) % red_frames.Length;
            sr.sprite = red_frames[fx];
        }
    }

    public void set_state_blue() {
        state = "blue";
    }
    public void set_state_red() {
        state = "red";
    }
}
