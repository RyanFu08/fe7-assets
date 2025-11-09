using UnityEngine;

[RequireComponent(typeof(Camera))]
public class resolutionforcer : MonoBehaviour
{
    [SerializeField] float targetWidth = 480f;
    [SerializeField] float targetHeight = 320f;

    void Start()  { Apply(); }
    void OnRectTransformDimensionsChange() { Apply(); } // catches browser resizes

    void Apply()
    {
        var cam = GetComponent<Camera>();
        float target = targetWidth / targetHeight;          // 1.5
        float window = (float)Screen.width / Screen.height; // current

        if (Mathf.Abs(window - target) < 0.0001f)
        {
            cam.rect = new Rect(0, 0, 1, 1);
            return;
        }

        if (window > target)
        {
            // Window wider than 3:2 → pillarbox
            float w = target / window;                      // fraction of width to use
            float x = (1f - w) * 0.5f;
            cam.rect = new Rect(x, 0, w, 1);
        }
        else
        {
            // Window taller than 3:2 → letterbox
            float h = window / target;                      // fraction of height to use
            float y = (1f - h) * 0.5f;
            cam.rect = new Rect(0, y, 1, h);
        }
    }
}
