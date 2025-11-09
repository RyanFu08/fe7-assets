using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class arrow : MonoBehaviour
{
    // ---------- Inspector fields ----------
    public Sprite start_n, start_e, start_s, start_w;
    public Sprite end_n,   end_e,   end_s,   end_w;
    public Sprite ne, wn, se, ws;
    public Sprite ns, we;

    [Tooltip("Arrow sprite identifier; set to one of the names below")]
    public string state = "none";
    private string lastState;

    private SpriteRenderer sr;

    private void Awake() {
        sr = GetComponent<SpriteRenderer>();
        ApplyState();
    }

    private void Update() {
        if (state != lastState)
            ApplyState();
    }

    private void ApplyState() {
        Sprite newSprite = null;
        bool visible = true;

        switch (state) {
            case "start_n": newSprite = start_n; break;
            case "start_e": newSprite = start_e; break;
            case "start_s": newSprite = start_s; break;
            case "start_w": newSprite = start_w; break;

            case "end_n":   newSprite = end_n;   break;
            case "end_e":   newSprite = end_e;   break;
            case "end_s":   newSprite = end_s;   break;
            case "end_w":   newSprite = end_w;   break;

            case "ne": newSprite = ne; break;
            case "wn": newSprite = wn; break;
            case "se": newSprite = se; break;
            case "ws": newSprite = ws; break;

            case "ns": newSprite = ns; break;
            case "we": newSprite = we; break;

            case "none":
                visible = false;
                break;

            default:
                Debug.LogWarning($"arrow: unknown state '{state}'.");
                visible = false;
                break;
        }
        lastState = state;
        sr.sprite  = newSprite;
        sr.enabled = visible;
    }

    public void go_to(int x, int y) {
        transform.position = new Vector3(x, y, transform.position.z);
    }
}
