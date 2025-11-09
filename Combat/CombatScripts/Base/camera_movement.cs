using UnityEngine;

public class camera_movement : MonoBehaviour {
    // Target position in XY (for 2‑D scenes).  Z is taken from the camera’s current Z.
    [SerializeField] private Vector2 target = Vector2.zero;

    // How often (in seconds) the camera should advance by one Euclidean unit.
    [SerializeField] private float rate = 0.1f;

    private float timer = 0f;

    void Update() {
        timer += Time.deltaTime;

        if (timer < rate) return;
        timer -= rate;

        Vector3 current = transform.position;
        Vector3 dest    = new Vector3(target.x, target.y, current.z);

        Vector3 dir = dest - current;
        float dist  = dir.magnitude;
        if (dist < 1e-4f) return;

        // Step exactly 1 unit, clamped so we never overshoot.
        float step = Mathf.Min(2f, dist);
        transform.position = current + dir.normalized * step;
    }

    public void SetTarget(Vector2 newTarget) => target = newTarget;
    public void AddToTarget(Vector2 delta) => target += delta;
    public void AddToTargetX(float dx) => target.x += dx;
    public void AddToTargetY(float dy) => target.y += dy;
    public Vector2 GetTarget() => target;
}
