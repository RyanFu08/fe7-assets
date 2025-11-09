using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class TileHighlight : MonoBehaviour
{
    // ---------------- Inspector fields ----------------
    [Header("Sprites")]
    public Sprite r;
    public Sprite g;
    public Sprite b;

    [Header("Pulse settings")]
    [Tooltip("Breaths per second (2 = twice per second)")]
    public float pulseSpeed = 2f;        // ← set to 2 for 2 cycles/sec
    [Tooltip("Max fractional shrink (0.2 = –20 %)")]
    [Range(0f, 0.5f)]
    public float pulseMagnitude = 0.15f;

    // ---------------- private state -------------------
    private SpriteRenderer sr;
    private Vector3 baseScale;
    private bool isAnimating = true;

    // ---------------------------------------------------
    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;

        set("red");          // default colour
        go_to(0, 0);         // default position
    }

    private void Update()
    {
        if (!isAnimating) return;

        // One full breath = 2π radians, so ω = 2π * pulseSpeed
        float t = Time.time * pulseSpeed * 2f * Mathf.PI;

        // Cosine wave mapped to 0‥1‥0
        float shrinkFactor = (1f - Mathf.Cos(t)) * 0.5f;

        // Scale oscillates: 1  →  1-pulseMagnitude  →  1
        float scale = 1f - shrinkFactor * pulseMagnitude;

        transform.localScale = baseScale * scale;
    }

    // ---------------------------------------------------
    // public helpers
    // ---------------------------------------------------
    public void go_to(int x, int y)
    {
        transform.position = new Vector3(x, y, transform.position.z);
    }

    /// <summary>
    /// "red" / "green" / "blue" → show highlight (with pulse)  
    /// "none"                   → hide highlight (stop pulse)
    /// </summary>
    public void set(string t)
    {
        switch (t)
        {
            case "red":   sr.sprite = r; sr.enabled = true; isAnimating = true; break;
            case "green": sr.sprite = g; sr.enabled = true; isAnimating = true; break;
            case "blue":  sr.sprite = b; sr.enabled = true; isAnimating = true; break;
            case "none":
                sr.enabled = false;
                isAnimating = false;
                transform.localScale = baseScale; // reset scale
                break;
            default:
                Debug.LogWarning($"TileHighlight: unknown type '{t}'.");
                break;
        }
    }
}
