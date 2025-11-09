using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class MapSprite_Control : MonoBehaviour {

    // Track whether we've ever glided
    public bool has_glided = false;

    public GameObject cursor;

    [Tooltip("Color mode: blue, red, green, grayscale")]
    public string mode = "blue";

    public Sprite current_frame;
    public Sprite[] stand_frames, ready_frames;
    public Sprite[] up_frames, down_frames, right_frames;

    public float[] stand_frames_timing = {0.3f,0.3f,0.3f,0.3f},
                   ready_frames_timing = {0.3f,0.3f,0.3f,0.3f},
                   up_frames_timing    = {0.3f,0.1f,0.25f,0.1f},
                   down_frames_timing  = {0.3f,0.1f,0.25f,0.1f},
                   right_frames_timing = {0.3f,0.1f,0.25f,0.1f};

    public float[] stand_offset_x = {0f,0f,0f,0f},
                   stand_offset_y = {5f,5f,5f,5f},
                   ready_offset_x = {0f,0f,0f,0f},
                   ready_offset_y = {5f,5f,5f,5f},
                   up_offset_x    = {0f,0f,0f,0f},
                   up_offset_y    = {5f,5f,5f,5f},
                   down_offset_x  = {0f,0f,0f,0f},
                   down_offset_y  = {5f,5f,5f,5f},
                   right_offset_x = {0f,0f,0f,0f},
                   right_offset_y = {5f,5f,5f,5f};

    public string state = "stand";
    public float cx, cy, timer;
    public int frame;

    [Header("Glide Settings")]
    public float glideSpeed = 8f;

    private SpriteRenderer sr;
    private Camera cursorCam;
    private CursorControl2 cc;

    private Queue<Vector2> glideQueue;
    private bool isProcessingGlide;

    // cached color variants
    private Sprite[] stand_red, stand_green, stand_gray;
    private Sprite[] ready_red, ready_green, ready_gray;
    private Sprite[] up_red, up_green, up_gray;
    private Sprite[] down_red, down_green, down_gray;
    private Sprite[] right_red, right_green, right_gray;

    void Awake() {
        sr = GetComponent<SpriteRenderer>();
        cc = cursor.GetComponent<CursorControl2>();
        cursorCam = Camera.main;
        glideQueue = new Queue<Vector2>();

        CreateVariants(stand_frames, out stand_red, out stand_green, out stand_gray);
        CreateVariants(ready_frames, out ready_red, out ready_green, out ready_gray);
        CreateVariants(up_frames,    out up_red,    out up_green,    out up_gray);
        CreateVariants(down_frames,  out down_red,  out down_green,  out down_gray);
        CreateVariants(right_frames, out right_red, out right_green, out right_gray);

        set_stand();
        go_to(cx, cy);
    }

    void Start() {

    }

    void CreateVariants(Sprite[] blue, out Sprite[] red, out Sprite[] green, out Sprite[] gray) {
        int n = blue.Length;
        red   = new Sprite[n]; green = new Sprite[n]; gray  = new Sprite[n];
        for (int i = 0; i < n; i++) {
            red[i]   = SpriteColorSwap.SwapBlueToRed(  blue[i]);
            green[i] = SpriteColorSwap.SwapBlueToGreen(blue[i]);
            gray[i]  = SpriteColorSwap.SwapToGrayscale(blue[i]);
        }
    }

    void Update() {
        if (!isProcessingGlide) {
            go_to(cx, cy);
            if (state == "up" || state == "down" || state == "left" || state == "right")
                state = "stand";
        }
        timer += Time.deltaTime;
        UpdateAnimation();
    }

    void UpdateAnimation() {
        Sprite[] frames; float[] timings;
        Sprite[] reds, greens, grays;
        bool flipX = false;

        switch (state) {
            case "ready":
                frames  = ready_frames;  timings = ready_frames_timing;
                reds    = ready_red;     greens = ready_green;    grays = ready_gray;
                break;
            case "up":
                frames  = up_frames;     timings = up_frames_timing;
                reds    = up_red;        greens = up_green;       grays = up_gray;
                break;
            case "down":
                frames  = down_frames;   timings = down_frames_timing;
                reds    = down_red;      greens = down_green;     grays = down_gray;
                break;
            case "right":
                flipX   = true;
                frames  = right_frames;  timings = right_frames_timing;
                reds    = right_red;     greens = right_green;    grays = right_gray;
                break;
            case "left":
                flipX   = false;
                frames  = right_frames;  timings = right_frames_timing;
                reds    = right_red;     greens = right_green;    grays = right_gray;
                break;
            default:
                frames  = stand_frames;  timings = stand_frames_timing;
                reds    = stand_red;     greens = stand_green;    grays = stand_gray;
                break;
        }

        if (timer > timings[frame]) {
            timer = 0;
            frame = (frame + 1) % frames.Length;
            current_frame = GetFrame(frames, reds, greens, grays, frame);
        }
        sr.flipX = flipX;
        sr.sprite = current_frame;
    }

    public void set_ready()  => SwitchState("ready");
    public void set_stand()  => SwitchState("stand");
    public void set_up()     => SwitchState("up");
    public void set_down()   => SwitchState("down");
    public void set_right()  => SwitchState("right");
    public void set_left()   => SwitchState("left");

    void SwitchState(string newState) {
        if (state != newState) {
            state = newState;
            frame = 0;
            timer = 0;
        }
        UpdateCurrentFrame();
        sr.sprite = current_frame;
    }

    void UpdateCurrentFrame() {
        Sprite[] frames, reds, greens, grays;
        switch (state) {
            case "ready": frames = ready_frames; reds = ready_red; greens = ready_green; grays = ready_gray; break;
            case "up":    frames = up_frames;    reds = up_red;    greens = up_green;    grays = up_gray;    break;
            case "down":  frames = down_frames;  reds = down_red;  greens = down_green;  grays = down_gray;  break;
            case "right":
            case "left":  frames = right_frames; reds = right_red; greens = right_green; grays = right_gray; break;
            default:      frames = stand_frames; reds = stand_red; greens = stand_green; grays = stand_gray; break;
        }
        current_frame = GetFrame(frames, reds, greens, grays, frame);
    }

    public void go_to(float x, float y) {
        cx = x; cy = y;
        float[] offsetX, offsetY;
        switch (state) {
            case "ready": offsetX = ready_offset_x; offsetY = ready_offset_y; break;
            case "up":    offsetX = up_offset_x;    offsetY = up_offset_y;    break;
            case "down":  offsetX = down_offset_x;  offsetY = down_offset_y;  break;
            case "right": offsetX = right_offset_x; offsetY = right_offset_y; break;
            case "left":  offsetX = right_offset_x; offsetY = right_offset_y; break;
            default:      offsetX = stand_offset_x; offsetY = stand_offset_y; break;
        }
        transform.position = new Vector3(
            16 * cx + offsetX[frame],
            16 * cy + offsetY[frame],
            transform.position.z
        );
    }

    Sprite GetFrame(Sprite[] blue, Sprite[] red, Sprite[] green, Sprite[] gray, int i) {
        switch (mode) {
            case "red":       return red[i];
            case "green":     return green[i];
            case "grayscale": return gray[i];
            default:          return blue[i];
        }
    }

    public void GlideUp()    => EnqueueGlide(Vector2.up);
    public void GlideDown()  => EnqueueGlide(Vector2.down);
    public void GlideRight() => EnqueueGlide(Vector2.right);
    public void GlideLeft()  => EnqueueGlide(Vector2.left);

    private void EnqueueGlide(Vector2 dir) {
        glideQueue.Enqueue(dir);
        if (!isProcessingGlide) {
            isProcessingGlide = true;
            cc.cameraLocked   = true;
            StartCoroutine(ProcessGlides());
        }
    }

    private IEnumerator ProcessGlides() {
        while (glideQueue.Count > 0) {
            yield return GlideStep(glideQueue.Dequeue());
        }

        // Snap everything precisely to grid
        go_to(cx, cy);
        Vector3 gridPos = new Vector3(16f * cx, 16f * cy, transform.position.z);
        transform.position        = gridPos;
        cursor.transform.position = gridPos;
        cc.targetPos             = gridPos;
        cc.cam.transform.position = new Vector3(gridPos.x, gridPos.y, cc.cam.transform.position.z);

        // Unlock camera and mark glide complete
        cc.cameraLocked   = false;
        isProcessingGlide = false;
        has_glided        = true;  // <-- now set to true once any glide finishes
    }

    private IEnumerator GlideStep(Vector2 dir) {
        if      (dir == Vector2.up)    set_up();
        else if (dir == Vector2.down)  set_down();
        else if (dir == Vector2.right) set_right();
        else if (dir == Vector2.left)  set_left();

        Vector3 start = transform.position;
        Vector3 end   = start + new Vector3(dir.x, dir.y, 0f) * 16f;
        float duration = 16f / glideSpeed;
        float elapsed = 0f;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 pos = Vector3.Lerp(start, end, t);
            transform.position           = pos;
            cursorCam.transform.position = new Vector3(pos.x, pos.y, cursorCam.transform.position.z);
            yield return null;
        }

        // Final snap for this step
        transform.position           = end;
        cursorCam.transform.position = new Vector3(end.x, end.y, cursorCam.transform.position.z);
        cc.transform.position        = end;
        cc.targetPos                 = end;
        cx += dir.x; 
        cy += dir.y;
    }

    public void SetBlueMode()                => ChangeMode("blue");
    public void SetRedMode(bool toRed = true)   => ChangeMode(toRed   ? "red"       : "blue");
    public void SetGreenMode(bool toGreen = true) => ChangeMode(toGreen ? "green"     : "blue");
    public void SetGrayscaleMode(bool toGray = true) => ChangeMode(toGray ? "grayscale" : "blue");

    private void ChangeMode(string newMode) {
        mode = newMode;
        UpdateCurrentFrame();
        sr.sprite = current_frame;
    }

    public static class SpriteColorSwap {
        public static Sprite SwapBlueToRed(Sprite o) => Swap(o, c =>
            (c.b > c.r + .15f && c.b > c.g - .05f && c.a > .1f)
                ? new Color(c.b, Mathf.Max(0, c.r - .4f), Mathf.Max(0, c.g - .4f), c.a)
                : c
        );
        public static Sprite SwapBlueToGreen(Sprite o) => Swap(o, c =>
            (c.b > c.r + .15f && c.b > c.g - .05f && c.a > .1f)
                ? new Color(Mathf.Max(0, c.r - .4f), c.b, Mathf.Max(0, c.g - .4f), c.a)
                : c
        );
        public static Sprite SwapToGrayscale(Sprite o) => Swap(o, c => {
            float lum = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
            return new Color(lum, lum, lum, c.a);
        });
        static Sprite Swap(Sprite o, Func<Color, Color> f) {
            var texSrc = o.texture;
            var r = o.rect;
            int x = (int)r.x, y = (int)r.y, w = (int)r.width, h = (int)r.height;
            var px = texSrc.GetPixels(x, y, w, h);
            for (int i = 0; i < px.Length; i++) px[i] = f(px[i]);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) {
                filterMode = texSrc.filterMode,
                wrapMode   = texSrc.wrapMode,
                anisoLevel = texSrc.anisoLevel
            };
            tex.SetPixels(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), 
                                 new Vector2(o.pivot.x/r.width, o.pivot.y/r.height), 
                                 o.pixelsPerUnit);
        }
    }
}
