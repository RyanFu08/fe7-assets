using UnityEngine;

public class HitFXController : MonoBehaviour {
        
    private SpriteRenderer sr;
    public Sprite[] nocrit_frames;
    public Sprite[] crit_frames;
    public Sprite[] miss_frames;

    public int cframe = -1;
    public float timer = 0f;
    public float anim_speed = 0.2f;

    public string hit_type = "nocrit";

    void Start() {
        sr = GetComponent<SpriteRenderer>();

    }

    void Update() {
        if (cframe == -1) {
            sr.enabled = false;
            return;
        }

        if (hit_type == "nocrit") {    
            sr.enabled = true;
            sr.sprite = nocrit_frames[cframe];
            timer += Time.deltaTime;
            if (timer >= anim_speed) {
                timer -= anim_speed;
                cframe += 1;
                if (cframe >= nocrit_frames.Length)
                    cframe = -1;
            }
        } else if (hit_type == "critical") {
            sr.enabled = true;
            sr.sprite = crit_frames[cframe];
            timer += Time.deltaTime;
            if (timer >= anim_speed) {
                timer -= anim_speed;
                cframe += 1;
                if (cframe >= crit_frames.Length)
                    cframe = -1;
            }
        } else if (hit_type == "miss") {
            sr.enabled = true;
            sr.sprite = miss_frames[cframe];
            timer += Time.deltaTime;
            if (timer >= anim_speed) {
                timer -= anim_speed;
                cframe += 1;
                if (cframe >= miss_frames.Length)
                    cframe = -1;
            }
        }
    }

    public void set_orientation(int val) {
        transform.localScale = new Vector3(val, transform.localScale.y, transform.localScale.z);
    }

}
