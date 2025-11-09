using UnityEngine;

[DisallowMultipleComponent]
public class camera_zoom : MonoBehaviour {
    [SerializeField] private float zoomSpeed     = 0.25f;   // seconds to 90%
    [SerializeField] private float zoomMultiplier = 2f;

    private Camera cam;
    private float  normalSize;
    private float  targetSize;
    private bool   zoomedOut = false;

    private float  zoomVelocity = 0f;

    void Awake() {
        cam        = Camera.main;
        normalSize = cam.orthographicSize;
        targetSize = normalSize;
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.C)) {
            zoomedOut  = !zoomedOut;
            targetSize = zoomedOut ? normalSize * zoomMultiplier
                                   : normalSize;
        }

        cam.orthographicSize =
            Mathf.SmoothDamp(cam.orthographicSize,
                             targetSize,
                             ref zoomVelocity,
                             zoomSpeed);

        if (Mathf.Abs(cam.orthographicSize - targetSize) < 0.001f) {
            cam.orthographicSize = targetSize;
            zoomVelocity = 0f;
        }
    }
}
