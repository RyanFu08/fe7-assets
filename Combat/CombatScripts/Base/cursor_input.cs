using UnityEngine;

public class cursor_input : MonoBehaviour {
    private cursor_movement cm;
    [SerializeField] private float timer, last_pressed;
    [SerializeField] private float key_delay = 0.1f;

    private bool active = true;

    void Awake() {
        cm = GetComponent<cursor_movement>();
    }
    void Start() {
        timer = 0f;
    }
    void Update() {
        if (!active) return;
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow)) {
            timer -= Time.deltaTime;
        }

        if (!cm.canBeMoved) {return;}

        /* immediate step on key‑down */
        if (Input.GetKeyDown(KeyCode.UpArrow))    {cm.AddToTargetY(16); timer = 8*key_delay;}
        if (Input.GetKeyDown(KeyCode.DownArrow))  {cm.AddToTargetY(-16); timer = 8*key_delay;}
        if (Input.GetKeyDown(KeyCode.RightArrow)) {cm.AddToTargetX(16); timer = 8*key_delay;}
        if (Input.GetKeyDown(KeyCode.LeftArrow))  {cm.AddToTargetX(-16); timer = 8*key_delay;}

        /* repeat step after the delay */
        if (timer > 0) return;        // not time yet

        /*  ─── held keys, but skip the very first frame (no double count) ─── */
        if (Input.GetKey(KeyCode.UpArrow)    && !Input.GetKeyDown(KeyCode.UpArrow))    {
            cm.AddToTargetY(16);  last_pressed = timer;
            timer += key_delay;
        }
        if (Input.GetKey(KeyCode.DownArrow)  && !Input.GetKeyDown(KeyCode.DownArrow))  { 
            cm.AddToTargetY(-16); last_pressed = timer;
            timer += key_delay;
        }
        if (Input.GetKey(KeyCode.RightArrow) && !Input.GetKeyDown(KeyCode.RightArrow)) { 
            cm.AddToTargetX(16);  last_pressed = timer; 
            timer += key_delay;
        }
        if (Input.GetKey(KeyCode.LeftArrow)  && !Input.GetKeyDown(KeyCode.LeftArrow))  { 
            cm.AddToTargetX(-16); last_pressed = timer; 
            timer += key_delay;
        }
    }

    /***PUBLIC***/

    public void activate() {
        active = true;
    }

    public void deactivate() {
        active = false;
    }

}