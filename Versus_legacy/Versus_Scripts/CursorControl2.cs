using UnityEngine;
using System.Collections.Generic;

public class CursorControl2 : MonoBehaviour
{
    [Header("Cursor Movement")]
    public float moveSpeed        = 400f;    // world units/sec
    public float gridSize         = 16f;     // world units per arrow-step

    [Header("Key Repeat")]
    public float keyRepeatDelay    = 0.1f;   // sec before repeating
    public float keyRepeatInterval = 0.04f;  // sec between repeats

    [Header("Cursor Animation")]
    public Sprite[] cursorFrames;            // drag your frames here
    public Sprite large;
    public float   frameInterval  = 0.1f;    // sec/frame

    [Header("Camera Edge-Pan")]
    public float edgeMargin        = 48f;    // px from screen edge

    [HideInInspector]
    public bool cameraLocked = false;

    [HideInInspector]
    public Camera cam;

    [HideInInspector]
    public Vector3 targetPos;

    private SpriteRenderer sr;
    private int frameIndex;
    private float frameTimer;
    private Dictionary<KeyCode, float> holdTimers;

    private bool is_frozen = false;

    void Start()
    {
        sr         = GetComponent<SpriteRenderer>();
        cam        = Camera.main;
        holdTimers = new Dictionary<KeyCode, float>();

        // Snap start position to grid
        Vector3 start = transform.position;
        start.x = Mathf.Round(start.x / gridSize) * gridSize;
        start.y = Mathf.Round(start.y / gridSize) * gridSize;
        transform.position = start;
        targetPos = start;

        if (cursorFrames != null && cursorFrames.Length > 0)
            sr.sprite = cursorFrames[0];

        if (!cam.orthographic)
            Debug.LogWarning("CursorControl2.PanCameraIfNeeded assumes an orthographic camera.");

        
    }

    void Update() {
        // 1) hide or show sprite based on lock
        sr.enabled = !cameraLocked;

        // 2) if locked, still smooth‐move to new position but skip input & pan
        if (cameraLocked)
        {
            MoveCursorSmooth();
            return;
        }

        // 3) normal behavior when unlocked
        HandleInput();
        AnimateCursor();
        MoveCursorSmooth();
        PanCameraIfNeeded();
    }

    private bool ok_pos(Vector3 t) {
        if (t.x > 240) return false;
        if (t.x < -224) return false;
        if (t.y < -224) return false;
        if (t.y > 208) return false;
        return true;
    }

    private void HandleInput() {
        var directions = new[]
        {
            KeyCode.UpArrow,
            KeyCode.DownArrow,
            KeyCode.LeftArrow,
            KeyCode.RightArrow
        };

        foreach (var key in directions) {
            if (Input.GetKeyDown(key)) {
                MoveByKey(key);
                holdTimers[key] = 0f;
            }
            if (Input.GetKey(key)) {
                if (holdTimers.ContainsKey(key)) {
                    float t = holdTimers[key] + Time.deltaTime;
                    if (t >= keyRepeatDelay) {
                        float excess = t - keyRepeatDelay;
                        int reps = Mathf.FloorToInt(excess / keyRepeatInterval);
                        for (int i = 0; i < reps; i++)
                            MoveByKey(key);
                        t -= reps * keyRepeatInterval;
                    }
                    holdTimers[key] = t;
                } else {
                    holdTimers[key] = Time.deltaTime;
                }
            }
            if (Input.GetKeyUp(key))
                holdTimers.Remove(key);
        }
    }

    private void MoveByKey(KeyCode key) {
        Vector3 dir = Vector3.zero;
        if      (key == KeyCode.UpArrow)    dir = Vector3.up;
        else if (key == KeyCode.DownArrow)  dir = Vector3.down;
        else if (key == KeyCode.LeftArrow)  dir = Vector3.left;
        else if (key == KeyCode.RightArrow) dir = Vector3.right;

        targetPos += dir * gridSize;
        if (!ok_pos(targetPos)) targetPos -= dir * gridSize;
    }

    private void AnimateCursor()
    {
        if (is_frozen) return;
        if (cursorFrames == null || cursorFrames.Length == 0) 
            return;

        frameTimer += Time.deltaTime;
        if (frameTimer >= frameInterval)
        {
            frameTimer -= frameInterval;
            frameIndex = (frameIndex + 1) % cursorFrames.Length;
            sr.sprite  = cursorFrames[frameIndex];
        }
    }

    private void MoveCursorSmooth()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPos,
            moveSpeed * Time.deltaTime
        );
    }

    private void PanCameraIfNeeded()
    {
        float worldPerPixelY = cam.orthographicSize * 2f / Screen.height;
        float worldPerPixelX = worldPerPixelY * cam.aspect;

        Vector3 screenPos = cam.WorldToScreenPoint(transform.position);
        Vector3 shift     = Vector3.zero;

        if (screenPos.x < edgeMargin)
            shift.x = -(edgeMargin - screenPos.x) * worldPerPixelX;
        else if (screenPos.x > Screen.width - edgeMargin)
            shift.x = (screenPos.x - (Screen.width - edgeMargin)) * worldPerPixelX;

        if (screenPos.y < edgeMargin)
            shift.y = -(edgeMargin - screenPos.y) * worldPerPixelY;
        else if (screenPos.y > Screen.height - edgeMargin)
            shift.y = (screenPos.y - (Screen.height - edgeMargin)) * worldPerPixelY;

        if (shift != Vector3.zero)
            cam.transform.position += new Vector3(shift.x, shift.y, 0f);
    }

    public void freeze_big() {
        is_frozen = true;
        sr.sprite = large;
    }

    public void unfreeze_big() {
        is_frozen = false;
    }

}
