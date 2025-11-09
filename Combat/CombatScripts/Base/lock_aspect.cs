using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FixedAspect : MonoBehaviour {
    [Header("Target content size (pixels)")]
    [SerializeField] int targetWidthPx  = 480;
    [SerializeField] int targetHeightPx = 320;

    [Header("Scaling")]
    [Tooltip("Use integer multiples only (1x, 2x, 3x...) for crisp pixels")]
    [SerializeField] bool integerScaling = true;

    int lastW, lastH;
    Camera cam;

    void Awake() { cam = GetComponent<Camera>(); cam.rect = new Rect(0,0,1,1); }
    void Start()  { Apply(); }
    void Update() { if (Screen.width != lastW || Screen.height != lastH) Apply(); }

    void Apply() {
        lastW = Screen.width; lastH = Screen.height;

        // Compute scale to fit the 480x320 content inside the window
        float sx = (float)Screen.width  / targetWidthPx;
        float sy = (float)Screen.height / targetHeightPx;
        float scale = Mathf.Min(sx, sy);

        if (integerScaling) {
            int k = Mathf.FloorToInt(scale);
            if (k < 1) k = 1;           // never smaller than 1x
            scale = k;
        }

        // Size of the content area in *screen pixels*
        float contentW = targetWidthPx  * scale;
        float contentH = targetHeightPx * scale;

        // Convert to normalized viewport rect (0..1)
        float vw = contentW / Screen.width;
        float vh = contentH / Screen.height;
        float vx = (1f - vw) * 0.5f;
        float vy = (1f - vh) * 0.5f;

        cam.rect = new Rect(vx, vy, vw, vh);
        cam.backgroundColor = Color.black;    // bars
        cam.clearFlags = CameraClearFlags.SolidColor;
    }
}
