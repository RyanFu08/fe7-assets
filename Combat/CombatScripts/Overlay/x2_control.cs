using UnityEngine;

/// Floaty “×2” indicator for a super‑effective attack.
/// Attach it to the indicator sprite (child of the unit).
public class x2_control : MonoBehaviour
{
    private SpriteRenderer sr;
    [Header("Orbit (oval)")]
    [Tooltip("Horizontal radius, world units")]
    public float radiusX = 1.2f;
    [Tooltip("Vertical radius, set < radiusX for a flatter shape")]
    public float radiusY = 0.8f;
    [Tooltip("Revolutions per second")]
    public float revPerSec = 0.6f;

    [Header("Pulse (breathing)")]
    public float pulseAmplitude = 0.15f;   // Δ radius, world units
    public float pulseSpeed     = 2f;      // cycles per second

    [Header("Vertical bob")]
    public float bobAmplitude = 0.1f;      // up–down range, world units
    public float bobSpeed     = 1.3f;      // cycles per second

    Vector3 _baseLocalPos;   // starting offset relative to parent
    float   _theta;          // current orbit angle (radians)

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        _baseLocalPos = transform.localPosition;
        _theta        = 0f;
        hide();
    }

    void Update()
    {
        // 1. Advance the orbit angle
        _theta += revPerSec * 2f * Mathf.PI * Time.deltaTime;

        // 2. Breathing factor (1 ± something)
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * (pulseAmplitude / Mathf.Max(radiusX, radiusY));

        // 3. Oval offset in the XY plane
        float x = Mathf.Cos(_theta) * radiusX * pulse;
        float y = Mathf.Sin(_theta) * radiusY * pulse;

        // 4. Add gentle vertical bob
        y += Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;

        // 5. Apply relative position
        transform.localPosition = _baseLocalPos + new Vector3(x, y, 0f);
    }

    /// Call this if you reuse the object so it orbits around a new parent point.
    public void ResetCenter()
    {
        _baseLocalPos = transform.localPosition;
        _theta        = 0f;
    }

    public void show() {
        sr.enabled = true;
    }
    public void hide() {
        sr.enabled = false;
    }

}
