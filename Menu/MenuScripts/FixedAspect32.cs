using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FixedAspect32 : MonoBehaviour
{
    [SerializeField] int ratioW = 3;
    [SerializeField] int ratioH = 2;

    Camera cam;
    int lastW, lastH;

    void Awake()
    {
        cam = GetComponent<Camera>();
        // Bars color comes from the camera background
        cam.clearFlags = CameraClearFlags.SolidColor;
        Apply();
    }

    void Update()
    {
        if (Screen.width != lastW || Screen.height != lastH) Apply();
    }

    void Apply()
    {
        lastW = Screen.width;
        lastH = Screen.height;

        float target = (float)ratioW / ratioH;                // 1.5 for 3:2
        float window = (float)Screen.width / Screen.height;

        if (Mathf.Approximately(window, target))
        {
            cam.rect = new Rect(0, 0, 1, 1);
            return;
        }

        if (window > target)
        {
            // Window wider than 3:2, add pillarboxes
            float w = target / window;
            float x = (1f - w) * 0.5f;
            cam.rect = new Rect(x, 0, w, 1);
        }
        else
        {
            // Window taller than 3:2, add letterboxes
            float h = window / target;
            float y = (1f - h) * 0.5f;
            cam.rect = new Rect(0, y, 1, h);
        }
    }
}
