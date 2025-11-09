using UnityEngine;

public class cursor_animator : MonoBehaviour {
    private SpriteRenderer sr;

    [SerializeField] private string state = "breathe"; //breathe, expand, hide
        [SerializeField] private Sprite[] breathe_frames;
        [SerializeField] private float breathe_delay = 0.1f;
        private int current_breathe_frame = 0;

        [SerializeField] private Sprite[] expand_frames;
        [SerializeField] private float expand_delay = 0.1f;
        private int current_expand_frame = 0;

        public bool showw = true;

    private float timer = 0.11f;

    void Start() {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update() {
        timer += Time.deltaTime;
        if (state == "breathe") {
            if (timer < breathe_delay) return;
            timer -= breathe_delay;
            current_breathe_frame = (current_breathe_frame+1)%breathe_frames.Length;
            sr.sprite = breathe_frames[current_breathe_frame];
        } else if (state == "expand") {
            if (timer < expand_delay) return;
            timer -= expand_delay;
            current_expand_frame = (current_expand_frame+1)%expand_frames.Length;
            sr.sprite = expand_frames[current_expand_frame];
        }
    }

    public void set_breathe() {
        if (state == "breathe") return;
        state = "breathe";
        current_breathe_frame = 0;
    }
    public void set_expand() {
        if (state == "expand") return;
        state = "expand";
        current_expand_frame = 0;
    }
    public void hide() {
        sr.enabled = false;
        showw = false;
    }
    public void show() {
        sr.enabled = true;
        showw = true;
    }
    
}
