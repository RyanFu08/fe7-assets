using System.Collections;
using UnityEngine;

// Run late so other scripts can place the object first
[DefaultExecutionOrder(10000)]
public class WingScript : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float duration = 1.19f;
    [SerializeField] private bool useUnscaledTime = false;

    [Header("Motion (LOCAL space)")]
    [SerializeField] private float amplitude = 3f;                  // peak offset relative to anchor
    [SerializeField] private Vector3 localDirection = Vector3.down; // relative to parent

    [Header("Behavior")]
    [Tooltip("If true, capture the anchor after one frame so inspector/other scripts can set the final position first.")]
    [SerializeField] private bool deferAnchorCaptureOneFrame = true;
    [Tooltip("Start animating automatically when enabled.")]
    [SerializeField] private bool autoActivate = true;

    private SpriteRenderer sr;
    private Vector3 anchorLocalPos;      // captured once, never overwritten
    private bool anchorCaptured = false; // gate to avoid recapture
    private bool isActive = false;
    private float t0; // cycle start time

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        // Defer anchor capture so any parent/other script placements finish first
        if (!anchorCaptured)
        {
            if (deferAnchorCaptureOneFrame) StartCoroutine(CaptureAnchorNextFrame());
            else CaptureAnchorImmediate();
        }

        if (autoActivate) set_active();
        else SnapToAnchor(); // ensure we don't jump to 0,0 visually
    }

    void OnDisable()
    {
        isActive = false;
    }

    private IEnumerator CaptureAnchorNextFrame()
    {
        // Wait for end of frame so all Start/Awake/OnEnable placements have happened
        yield return null; // one frame
        CaptureAnchorImmediate();
        if (!autoActivate) SnapToAnchor();
    }

    private void CaptureAnchorImmediate()
    {
        if (anchorCaptured) return;
        anchorLocalPos = transform.localPosition; // this now reads the inspector-final value
        anchorCaptured = true;
        t0 = CurrentTime();
    }

    void Update()
    {
        if (!isActive)
        {
            // Hard lock to anchor when inactive to prevent drift from other systems
            SnapToAnchor();
            return;
        }

        // Exact phase based on absolute time
        float t = Mathf.Repeat(CurrentTime() - t0, Mathf.Max(0.0001f, duration));
        float n = t / duration; // 0..1
        float s = 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * n)); // smooth 0 -> 1 -> 0

        Vector3 dir = localDirection.sqrMagnitude > 0f ? localDirection.normalized : Vector3.down;
        Vector3 localOffset = dir * (amplitude * s);

        transform.localPosition = anchorLocalPos + localOffset;
    }

    public void set_active()
    {
        if (!anchorCaptured) CaptureAnchorImmediate();
        if (sr) sr.enabled = true;
        SnapToAnchor();           // never jump to 0,0
        t0 = CurrentTime();       // restart cycle cleanly
        isActive = true;
    }

    public void set_inactive()
    {
        isActive = false;
        SnapToAnchor();
        if (sr) sr.enabled = false;
    }

    public void ResetToStart()   // only if you *intentionally* want a NEW anchor
    {
        anchorLocalPos = transform.localPosition;
        t0 = CurrentTime();
        anchorCaptured = true;
    }

    private void SnapToAnchor()
    {
        if (anchorCaptured && transform.localPosition != anchorLocalPos)
            transform.localPosition = anchorLocalPos;
    }

    private float CurrentTime() => useUnscaledTime ? Time.unscaledTime : Time.time;
}
