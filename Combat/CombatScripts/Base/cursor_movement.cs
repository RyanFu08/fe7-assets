using UnityEngine;

[DisallowMultipleComponent]
public class cursor_movement : MonoBehaviour
{
    /* ─────────────── Cursor glide ─────────────── */
    [Header("Cursor glide")]
    [SerializeField] public Vector2 target      = Vector2.zero;
    [SerializeField] private float   rate        = 0.01f;   // seconds per 1‑unit hop
    [SerializeField] private float   maxDistance = 2f;      // “not too far”

    /* ─────────────── Camera buffer ─────────────── */
    [Header("Camera edge buffer")]
    [SerializeField] private float edgeThreshold = 4f;      // world‑units

    public bool canBeMoved { get; private set; } = true;

    private float timer = 0f, timer2 = 0f;
    private camera_movement camCtrl;

    void Awake() => camCtrl = Camera.main.GetComponent<camera_movement>();

    void Update() {
        canBeMoved = Vector2.Distance(transform.position, target) <= maxDistance;

        if (GetComponent<cursor_animator>().showw) {
            timer2 += Time.deltaTime;
            if (timer2 >= 5f) {
                SnapTargetToGrid16();
            }
        } else {
            timer2 = 0f;
        }

        timer += Time.deltaTime;
        if (timer >= rate) {
            timer -= rate;

            Vector3 current = transform.position;
            Vector3 dest    = new Vector3(target.x, target.y, current.z);
            Vector3 dir     = dest - current;

            if (dir.sqrMagnitude > 1e-8f)
                transform.position = current + dir.normalized * Mathf.Min(2f, dir.magnitude);
        }

        if (camCtrl) MaintainCameraBuffer();
    }

    /* ───────────── Maintain camera safety band ───────────── */
    private void MaintainCameraBuffer()
    {
        if (!camCtrl) return;

        Camera cam      = Camera.main;
        float vertExt   = cam.orthographicSize;
        float horzExt   = vertExt * cam.aspect;

        Vector2 camTgt  = camCtrl.GetTarget();
        Vector2 newCamT = camTgt;

        float left   = camTgt.x - horzExt;
        float right  = camTgt.x + horzExt;
        float bottom = camTgt.y - vertExt;
        float top    = camTgt.y + vertExt;

        if (target.x > right  - edgeThreshold)   newCamT.x += target.x - (right  - edgeThreshold);
        else if (target.x < left + edgeThreshold)newCamT.x += target.x - (left   + edgeThreshold);

        if (target.y > top    - edgeThreshold)   newCamT.y += target.y - (top    - edgeThreshold);
        else if (target.y < bottom+edgeThreshold)newCamT.y += target.y - (bottom + edgeThreshold);

        if (newCamT != camTgt) camCtrl.SetTarget(newCamT);
    }

    /* ──────────── Public movement API ──────────── */
    public void  SetTarget  (Vector2 newTarget)   => target  = newTarget;
    public void  AddToTarget(Vector2 delta)       => target += delta;
    public void  AddToTargetX(float dx)           => target.x += dx;
    public void  AddToTargetY(float dy)           => target.y += dy;
    public Vector2 GetTarget()                    => target;

    public void SetEdgeThreshold(float units)     => edgeThreshold = Mathf.Max(0f, units);
    public void SnapTargetToGrid16() {
        const float G = 16f;
        target.x = Mathf.Round(target.x / G) * G;
        target.y = Mathf.Round(target.y / G) * G;
    }

    /* ───────────── Teleport helpers ───────────── */
    /// <summary>Instantly moves the cursor to <paramref name="pos"/> and
    /// sets its glide target to the same position. The camera safety logic
    /// runs immediately so the camera begins moving if needed.</summary>
/*    public void Teleport(Vector2 pos)
    {
        transform.position = new Vector3(pos.x, pos.y, transform.position.z);
        target             = pos;
        canBeMoved         = true;               // now inside maxDistance
        MaintainCameraBuffer();                  // update camera target once
    }

    /// <summary>Instantly moves the cursor to <paramref name="pos"/> (full 3D),
    /// syncs the glide target to its X/Y, and nudges the camera if needed.</summary>
*/
    public void Teleport(Vector3 pos)
    {
        transform.position = pos;
        target             = new Vector2(pos.x, pos.y);
        canBeMoved         = true;
        MaintainCameraBuffer();
    }

//    /// <summary>Overload for convenience.</summary>
//    public void Teleport(float x, float y) => Teleport(new Vector2(x, y));

}
