using System.Collections;
using UnityEngine;

public class phaes_mover : MonoBehaviour
{
    [Tooltip("Seconds to travel from (220,0) → (-220,0).")]
    public float travelTime = 2f;
    [Tooltip("Pause at the center (seconds). 0 = no pause.")]
    public float midPause = 0.25f;

    private static readonly Vector3 START_POS = new Vector3( 0f, 0f, 0f);
    private static readonly Vector3 END_POS   = new Vector3(-440, 0f, 0f);

    private bool moving;

    // --- new: route movement to the right space (UI vs world) ---
    RectTransform _rt;
    Vector3 GetPos()
    {
        if (_rt) return _rt.anchoredPosition3D;
        return transform.localPosition;
    }
    void SetPos(Vector3 p)
    {
        if (_rt) _rt.anchoredPosition3D = p;
        else transform.localPosition = p;
    }
    // ------------------------------------------------------------

    void Awake() => _rt = GetComponent<RectTransform>();

    void Start() => SetPos(GetPos() + START_POS);   // relative: shift from current

    /// <summary>Launch the move; visible in Inspector via ContextMenu.</summary>
    [ContextMenu("Go")]
    public void go()
    {
        if (!moving) StartCoroutine(MoveRoutine());
    }

    private IEnumerator MoveRoutine()
    {
        moving = true;
        float t = 0f;

        // Relative path: from current as start to start + (END - START)
        Vector3 start = GetPos();
        Vector3 end   = start + (END_POS - START_POS);

        bool held = false;

        while (t < travelTime)
        {
            float u     = Mathf.Clamp01(t / travelTime); // 0 → 1
            float eased = EaseOutInSine(u);               // fast → slow → fast
            SetPos(Vector3.Lerp(start, end, eased));

            // Stop once at the midpoint (visible dwell)
            if (!held && u >= 0.5f)
            {
                held = true;
                SetPos(Vector3.Lerp(start, end, 0.5f));
                if (midPause > 0f) yield return new WaitForSecondsRealtime(midPause);
            }

            t += Time.deltaTime;
            yield return null;
        }

        SetPos(end);       // finish cleanly
        SetPos(start);     // teleport back (keep original behavior)
        moving = false;
    }

    // Out-In Sine easing: quickest at ends, slowest at 0.5
    private static float EaseOutInSine(float u)
    {
        if (u < 0.5f) return 0.5f * Mathf.Sin(Mathf.PI * u);
        float v = u - 0.5f;
        return 0.5f + 0.5f * (1f - Mathf.Cos(Mathf.PI * v));
    }
}
