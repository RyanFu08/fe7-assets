using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer))]
public class BattleAnimator : MonoBehaviour {

    private SpriteRenderer sr;
    public float delay = .25f;
    public float timer = 0f;

    public string state = "none";
    public string prev_state = "none";
    public bool myturn = false;

    public GameObject choreo;
    public CameraShake camShake;

    Dictionary<string, (int,int)> bounds = new Dictionary<string, (int,int)>();
    Dictionary<string, string> paths = new Dictionary<string, string>();

    int frame = -1;

    // For white/undo logic
    private Texture2D originalTexture = null;   // atlas texture backing the sprite
    private Sprite originalSprite = null;
    private bool isWhite = false;
    private Texture2D whiteTextureCurrent = null;   // destroy when restoring

    public float white_timer = 0f;
    public float white_time = 0.2f;

    public string hit_type = "nocrit";

    // Caches
    private static Dictionary<string, (int,int)> BOUNDS_CACHE;
    private static Dictionary<string, string> PATHS_CACHE;
    private readonly Dictionary<string, Sprite[]> spriteCache = new Dictionary<string, Sprite[]>();
    private static readonly string[] PAD3 = BuildPad3();
    private Transform tf;
    private Choreo choreoComp;

    void Awake() {
        if (BOUNDS_CACHE == null || PATHS_CACHE == null)
            BuildTables(out BOUNDS_CACHE, out PATHS_CACHE);

        bounds = BOUNDS_CACHE;
        paths  = PATHS_CACHE;

        tf = transform;
    }

    void Start() {
        sr = GetComponent<SpriteRenderer>();
        if (camShake == null && Camera.main != null)
            camShake = Camera.main.GetComponent<CameraShake>();
        if (choreo != null)
            choreoComp = choreo.GetComponent<Choreo>();
    }

    void Update() {
        if (state == "none") return;

        if (isWhite) {
            white_timer += Time.deltaTime;
            if (white_timer > white_time) {
                white_timer = 0f;
                UndoWhite();
            }
        }

        var seq = bounds[state];   // (hitFrame, endFrame)
        int hitFrame = seq.Item1;
        int endFrame = seq.Item2;

        if (state != prev_state) {
            prev_state = state;
            frame = 1;
            SetFrameSprite();
        } else {
            timer += Time.deltaTime;

            if (frame != endFrame && timer > delay) {
                timer -= delay;
                frame++;
                SetFrameSprite();

                if (frame == hitFrame && myturn && camShake != null) {
                    if (hit_type == "critical") camShake.Shake(0.25f, 10f);
                    else camShake.Shake(0.15f, 4f);
                    if (choreoComp != null) choreoComp.add_damage();
                }
            }

            if (frame == endFrame && myturn) {
                frame = 1;
                if (choreoComp != null) choreoComp.cstate += 1;
            }
        }
    }

    void SetFrameSprite() {
        var seq = bounds[state];
        int endFrame = seq.Item2;

        if (!spriteCache.TryGetValue(state, out var arr) || arr == null || arr.Length <= endFrame) {
            var newArr = new Sprite[endFrame + 1];
            if (arr != null) {
                int copyLen = Mathf.Min(arr.Length, newArr.Length);
                for (int i = 0; i < copyLen; i++) newArr[i] = arr[i];
            }
            spriteCache[state] = arr = newArr;
        }

        var s = arr[frame];
        if (s == null) {
            string spritePath = paths[state] + PadToLength3(frame);
            s = Resources.Load<Sprite>(spritePath);
//            Debug.Log("Loaded sprite: " + spritePath);
            arr[frame] = s;

        }

        if (sr.sprite != s) sr.sprite = s;
        timer = 0f;

        // Reset white state and cache the original references (no deep copy)
        isWhite = false;
        white_timer = 0f;
        originalSprite = s;
        originalTexture = (s != null) ? s.texture : null;

        if (whiteTextureCurrent != null) {
            Destroy(whiteTextureCurrent);
            whiteTextureCurrent = null;
        }
    }

    string PadToLength3(int a) {
        if ((uint)a < 1000u) return PAD3[a];
        return a.ToString().PadLeft(3, '0');
    }

    public void whiten() {
        TurnWhite();
    }

    // White flash with exact alignment:
    // - Sample the sprite's exact rect on its source texture
    // - Create a same-sized texture filled with white, preserving alpha
    // - Recreate sprite with pivot normalized by that rect
    public void TurnWhite() {
        if (isWhite || sr.sprite == null || originalTexture == null)
            return;

        // Use the sprite's rect, which is defined in the source texture's pixel space.
        Rect r = originalSprite.rect;

        // Integer region to sample
        int x = Mathf.RoundToInt(r.x);
        int y = Mathf.RoundToInt(r.y);
        int w = Mathf.RoundToInt(r.width);
        int h = Mathf.RoundToInt(r.height);

        // Clamp to valid bounds to avoid OOB or negative sizes due to rounding
        int texW = originalTexture.width;
        int texH = originalTexture.height;

        if (w <= 0 || h <= 0 || x >= texW || y >= texH) {
            // Nothing sensible to sample, bail out gracefully
            return;
        }

        if (x < 0) { w += x; x = 0; }
        if (y < 0) { h += y; y = 0; }
        if (x + w > texW) w = texW - x;
        if (y + h > texH) h = texH - y;
        if (w <= 0 || h <= 0) return;

        // Guard pathological allocations
        const int MaxPixels = 4096 * 4096; // 16,777,216
        if (w * h > MaxPixels) return;

        // Read only the sprite's pixels
        Color[] pixels = originalTexture.GetPixels(x, y, w, h);

        // Whiten RGB, preserve alpha
        for (int i = 0; i < pixels.Length; ++i) {
            var c = pixels[i];
            pixels[i] = new Color(1f, 1f, 1f, c.a);
        }

        // Build new texture the same size
        var whiteTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        whiteTex.SetPixels(pixels);
        whiteTex.filterMode = FilterMode.Point;
        whiteTex.Apply();
        whiteTextureCurrent = whiteTex;

        // Normalize pivot by this rect so the world position stays identical
        Vector2 pivot01 = new Vector2(
            (w > 0) ? (originalSprite.pivot.x / r.width) : 0.5f,
            (h > 0) ? (originalSprite.pivot.y / r.height) : 0.5f
        );

        var whiteSprite = Sprite.Create(
            whiteTex,
            new Rect(0, 0, w, h),
            pivot01,
            originalSprite.pixelsPerUnit,
            0,
            SpriteMeshType.FullRect,
            originalSprite.border
        );

        sr.sprite = whiteSprite;
        isWhite = true;
    }

    public void UndoWhite() {
        if (!isWhite || originalSprite == null)
            return;

        if (whiteTextureCurrent != null) {
            Destroy(whiteTextureCurrent);
            whiteTextureCurrent = null;
        }

        sr.sprite = originalSprite;
        isWhite = false;
    }

    // ===== Helpers: static table builders and pad cache =====
    private static void BuildTables(out Dictionary<string, (int,int)> b, out Dictionary<string, string> p) {
        b = new Dictionary<string, (int,int)>();
        p = new Dictionary<string, string>();

        b["dragon_attack_nocrit"] = (80,116); p["dragon_attack_nocrit"] = "BattleAnimations/Dragon/attack_nocrit/IMG_";
        b["dragon_attack_frozen"] = (2,1); p["dragon_attack_frozen"] = "BattleAnimations/Dragon/attack_nocrit/IMG_";

        b["hector_axe_nocrit"] = (66,127); p["hector_axe_nocrit"] = "BattleAnimations/Hector/axe_nocrit/hector_Axe_";
        b["hector_axe_crit"]   = (68,129); p["hector_axe_crit"]   = "BattleAnimations/Hector/axe_crit/hector_Axe_";
        b["hector_axe_frozen"] = ( 2,  1); p["hector_axe_frozen"] = "BattleAnimations/Hector/axe_frozen/hector_Axe_";

        b["florina_lance_nocrit"] = (57,153); p["florina_lance_nocrit"] = "BattleAnimations/Florina/lance_nocrit/florina_Lance_";
        b["florina_lance_crit"]   = (110,204); p["florina_lance_crit"]  = "BattleAnimations/Florina/lance_critical/florina_Lance_";
        b["florina_lance_frozen"] = (  2,  1); p["florina_lance_frozen"]= "BattleAnimations/Florina/lance_frozen/florina_Lance_";

        b["florina_sword_nocrit"] = (57,157); p["florina_sword_nocrit"] = "BattleAnimations/Florina/sword_nocrit/florina_Sword_";
        b["florina_sword_crit"]   = (110,206); p["florina_sword_crit"]  = "BattleAnimations/Florina/sword_crit/florina_Sword_";
        b["florina_sword_frozen"] = (  2,  1); p["florina_sword_frozen"]= "BattleAnimations/Florina/sword_frozen/florina_Sword_";

        b["eliwood_sword_nocrit"] = (77,142); p["eliwood_sword_nocrit"] = "BattleAnimations/Eliwood/sword_nocrit/eliwood_Sword_";
        b["eliwood_sword_crit"]   = (95,164); p["eliwood_sword_crit"]   = "BattleAnimations/Eliwood/sword_crit/eliwood_Sword_";
        b["eliwood_sword_frozen"] = ( 2,  1); p["eliwood_sword_frozen"] = "BattleAnimations/Eliwood/sword_frozen/eliwood_Sword_";

        b["eliwood_lance_nocrit"] = (77,144); p["eliwood_lance_nocrit"] = "BattleAnimations/Eliwood/lance_nocrit/eliwood_Lance_";
        b["eliwood_lance_crit"]   = (97,162); p["eliwood_lance_crit"]   = "BattleAnimations/Eliwood/lance_crit/eliwood_Lance_";
        b["eliwood_lance_frozen"] = ( 2,  1); p["eliwood_lance_frozen"] = "BattleAnimations/Eliwood/lance_frozen/eliwood_Lance_";

        b["lyn_sword_nocrit"] = (82,147); p["lyn_sword_nocrit"] = "BattleAnimations/Lyn/sword_nocrit/lyn_Sword_";
        b["lyn_sword_crit"]   = (186,231); p["lyn_sword_crit"]  = "BattleAnimations/Lyn/sword_crit/lyn_Sword_";
        b["lyn_sword_frozen"] = (  2,  1); p["lyn_sword_frozen"]= "BattleAnimations/Lyn/sword_frozen/lyn_Sword_";

        b["lyn_bow_nocrit"] = (53, 61); p["lyn_bow_nocrit"] = "BattleAnimations/Lyn/bow_nocrit/lyn_Sword_";
        b["lyn_bow_crit"]   = (72, 80); p["lyn_bow_crit"]   = "BattleAnimations/Lyn/bow_crit/lyn_Sword_";
        b["lyn_bow_frozen"] = ( 2,  1); p["lyn_bow_frozen"] = "BattleAnimations/Lyn/bow_frozen/lyn_Sword_";

        b["sain_sword_nocrit"] = ( 47,114); p["sain_sword_nocrit"] = "BattleAnimations/Sain/sword_nocrit/sain_Sword_";
        b["sain_sword_crit"]   = (148,232); p["sain_sword_crit"]   = "BattleAnimations/Sain/sword_crit/sain_Sword_";
        b["sain_sword_frozen"] = (  2,  1); p["sain_sword_frozen"] = "BattleAnimations/Sain/sword_frozen/sain_Sword_";

        b["sain_lance_nocrit"] = ( 32,104); p["sain_lance_nocrit"] = "BattleAnimations/Sain/lance_nocrit/sain_Lance_";
        b["sain_lance_crit"]   = (163,238); p["sain_lance_crit"]   = "BattleAnimations/Sain/lance_crit/sain_Lance_";
        b["sain_lance_frozen"] = (  2,  1); p["sain_lance_frozen"] = "BattleAnimations/Sain/lance_frozen/sain_Lance_";

        b["sain_axe_nocrit"] = ( 36,117); p["sain_axe_nocrit"] = "BattleAnimations/Sain/axe_nocrit/sain_Axe_";
        b["sain_axe_crit"]   = (172,243); p["sain_axe_crit"]   = "BattleAnimations/Sain/axe_crit/sain_Axe_";
        b["sain_axe_frozen"] = (  2,  1); p["sain_axe_frozen"] = "BattleAnimations/Sain/axe_frozen/sain_Axe_";

        // Kent (reuse Sain assets)
b["kent_sword_nocrit"] = ( 47,114); p["kent_sword_nocrit"] = "BattleAnimations/Sain/sword_nocrit/sain_Sword_";
b["kent_sword_crit"]   = (148,232); p["kent_sword_crit"]   = "BattleAnimations/Sain/sword_crit/sain_Sword_";
b["kent_sword_frozen"] = (  2,  1); p["kent_sword_frozen"] = "BattleAnimations/Sain/sword_frozen/sain_Sword_";

b["kent_lance_nocrit"] = ( 32,104); p["kent_lance_nocrit"] = "BattleAnimations/Sain/lance_nocrit/sain_Lance_";
b["kent_lance_crit"]   = (163,238); p["kent_lance_crit"]   = "BattleAnimations/Sain/lance_crit/sain_Lance_";
b["kent_lance_frozen"] = (  2,  1); p["kent_lance_frozen"] = "BattleAnimations/Sain/lance_frozen/sain_Lance_";

b["kent_axe_nocrit"] = ( 36,117); p["kent_axe_nocrit"] = "BattleAnimations/Sain/axe_nocrit/sain_Axe_";
b["kent_axe_crit"]   = (172,243); p["kent_axe_crit"]   = "BattleAnimations/Sain/axe_crit/sain_Axe_";
b["kent_axe_frozen"] = (  2,  1); p["kent_axe_frozen"] = "BattleAnimations/Sain/axe_frozen/sain_Axe_";

// Lowen (reuse Sain assets)
b["lowen_sword_nocrit"] = ( 47,114); p["lowen_sword_nocrit"] = "BattleAnimations/Sain/sword_nocrit/sain_Sword_";
b["lowen_sword_crit"]   = (148,232); p["lowen_sword_crit"]   = "BattleAnimations/Sain/sword_crit/sain_Sword_";
b["lowen_sword_frozen"] = (  2,  1); p["lowen_sword_frozen"] = "BattleAnimations/Sain/sword_frozen/sain_Sword_";

b["lowen_lance_nocrit"] = ( 32,104); p["lowen_lance_nocrit"] = "BattleAnimations/Sain/lance_nocrit/sain_Lance_";
b["lowen_lance_crit"]   = (163,238); p["lowen_lance_crit"]   = "BattleAnimations/Sain/lance_crit/sain_Lance_";
b["lowen_lance_frozen"] = (  2,  1); p["lowen_lance_frozen"] = "BattleAnimations/Sain/lance_frozen/sain_Lance_";

b["lowen_axe_nocrit"] = ( 36,117); p["lowen_axe_nocrit"] = "BattleAnimations/Sain/axe_nocrit/sain_Axe_";
b["lowen_axe_crit"]   = (172,243); p["lowen_axe_crit"]   = "BattleAnimations/Sain/axe_crit/sain_Axe_";
b["lowen_axe_frozen"] = (  2,  1); p["lowen_axe_frozen"] = "BattleAnimations/Sain/axe_frozen/sain_Axe_";


        b["oswin_lance_nocrit"] = ( 77,207); p["oswin_lance_nocrit"] = "BattleAnimations/Oswin/lance_nocrit/oswin_Lance_";
        b["oswin_lance_crit"]   = (105,255); p["oswin_lance_crit"]   = "BattleAnimations/Oswin/lance_crit/oswin_Lance_";
        b["oswin_lance_frozen"] = (  2,  1); p["oswin_lance_frozen"] = "BattleAnimations/Oswin/lance_frozen/oswin_Lance_";

        b["oswin_axe_nocrit"] = ( 76,206); p["oswin_axe_nocrit"] = "BattleAnimations/Oswin/axe_nocrit/oswin_Axe_";
        b["oswin_axe_crit"]   = (100,277); p["oswin_axe_crit"]   = "BattleAnimations/Oswin/axe_crit/oswin_Axe_";
        b["oswin_axe_frozen"] = (  2,  1); p["oswin_axe_frozen"] = "BattleAnimations/Oswin/axe_frozen/oswin_Axe_";

        b["heath_sword_nocrit"] = (49,142); p["heath_sword_nocrit"] = "BattleAnimations/Heath/sword_nocrit/heath_Sword_";
        b["heath_sword_crit"]   = (82,171); p["heath_sword_crit"]   = "BattleAnimations/Heath/sword_crit/heath_Sword_";
        b["heath_sword_frozen"] = ( 2,  1); p["heath_sword_frozen"] = "BattleAnimations/Heath/sword_frozen/heath_Sword_";

        b["heath_lance_nocrit"] = (49,142); p["heath_lance_nocrit"] = "BattleAnimations/Heath/lance_nocrit/heath_Sword_";
        b["heath_lance_crit"]   = (82,181); p["heath_lance_crit"]   = "BattleAnimations/Heath/lance_crit/heath_Sword_";
        b["heath_lance_frozen"] = ( 2,  1); p["heath_lance_frozen"] = "BattleAnimations/Heath/lance_frozen/heath_Sword_";

        b["priscilla_magic_nocrit"] = (70, 95); p["priscilla_magic_nocrit"] = "BattleAnimations/Priscilla/magic_nocrit/heath_Sword_";
        b["priscilla_magic_crit"]   = (80,105); p["priscilla_magic_crit"]   = "BattleAnimations/Priscilla/magic_crit/heath_Sword_";
        b["priscilla_magic_frozen"] = ( 2,  1); p["priscilla_magic_frozen"] = "BattleAnimations/Priscilla/magic_frozen/heath_Sword_";

        b["priscilla_staff"] = (58,64); p["priscilla_staff"] = "BattleAnimations/Priscilla/staff/heath_Sword_";

        b["nils_refresh"] = (131,198); p["nils_refresh"] = "BattleAnimations/Nils/refresh/IMG_";
        b["nils_frozen"]  = (  2,  1); p["nils_frozen"]  = "BattleAnimations/Nils/frozen/IMG_";

        b["wil_bow_nocrit"] = (53, 63); p["wil_bow_nocrit"] = "BattleAnimations/Wil/bow_nocrit/IMG_";
        b["wil_bow_crit"]   = (133,144); p["wil_bow_crit"]  = "BattleAnimations/Wil/bow_crit/IMG_";
        b["wil_bow_frozen"] = (  2,  1); p["wil_bow_frozen"]= "BattleAnimations/Wil/bow_frozen/IMG_";

        b["canas_magic_nocrit"] = (280,344); p["canas_magic_nocrit"] = "BattleAnimations/Canas/magic_nocrit/IMG_";
        b["canas_magic_crit"]   = (300,369); p["canas_magic_crit"]   = "BattleAnimations/Canas/magic_crit/IMG_";
        b["canas_magic_frozen"] = (  2,  1); p["canas_magic_frozen"] = "BattleAnimations/Canas/magic_frozen/IMG_";

        b["lucius_magic_nocrit"] = (31, 47); p["lucius_magic_nocrit"] = "BattleAnimations/Lucius/magic_nocrit/IMG_";
        b["lucius_magic_crit"]   = (78,101); p["lucius_magic_crit"]   = "BattleAnimations/Lucius/magic_crit/IMG_";
        b["lucius_magic_frozen"] = ( 2,  1); p["lucius_magic_frozen"] = "BattleAnimations/Lucius/magic_frozen/IMG_";

        b["serra_magic_nocrit"] = (31, 47); p["serra_magic_nocrit"] = "BattleAnimations/Lucius/magic_nocrit/IMG_";
b["serra_magic_crit"]   = (78,101); p["serra_magic_crit"]   = "BattleAnimations/Lucius/magic_crit/IMG_";
b["serra_magic_frozen"] = ( 2,  1); p["serra_magic_frozen"] = "BattleAnimations/Lucius/magic_frozen/IMG_";

        b["pent_magic_nocrit"] = ( 50,112); p["pent_magic_nocrit"] = "BattleAnimations/Pent/magic_nocrit/IMG_";
        b["pent_magic_crit"]   = (153,216); p["pent_magic_crit"]   = "BattleAnimations/Pent/magic_crit/IMG_";
        b["pent_magic_frozen"] = (  2,  1); p["pent_magic_frozen"] = "BattleAnimations/Pent/magic_frozen/IMG_";

        b["raven_sword_nocrit"] = ( 35,110); p["raven_sword_nocrit"] = "BattleAnimations/Raven/sword_nocrit/IMG_";
        b["raven_sword_crit"]   = ( 81,248); p["raven_sword_crit"]   = "BattleAnimations/Raven/sword_crit/IMG_";
        b["raven_sword_frozen"] = (  2,  1); p["raven_sword_frozen"] = "BattleAnimations/Raven/sword_frozen/IMG_";

        b["raven_axe_nocrit"] = ( 37,120); p["raven_axe_nocrit"] = "BattleAnimations/Raven/axe_nocrit/IMG_";
        b["raven_axe_crit"]   = ( 59,234); p["raven_axe_crit"]   = "BattleAnimations/Raven/axe_crit/IMG_";
        b["raven_axe_frozen"] = (  2,  1); p["raven_axe_frozen"] = "BattleAnimations/Raven/axe_frozen/IMG_";

        b["rath_sword_nocrit"] = ( 36, 84); p["rath_sword_nocrit"] = "BattleAnimations/Rath/sword_nocrit/IMG_";
        b["rath_sword_crit"]   = (119,171); p["rath_sword_crit"]   = "BattleAnimations/Rath/sword_crit/IMG_";
        b["rath_sword_frozen"] = (  2,  1); p["rath_sword_frozen"] = "BattleAnimations/Rath/sword_frozen/IMG_";

        b["rath_bow_nocrit"] = ( 61,135); p["rath_bow_nocrit"] = "BattleAnimations/Rath/bow_nocrit/IMG_";
        b["rath_bow_crit"]   = ( 89,158); p["rath_bow_crit"]   = "BattleAnimations/Rath/bow_crit/IMG_";
        b["rath_bow_frozen"] = (  2,  1); p["rath_bow_frozen"] = "BattleAnimations/Rath/bow_frozen/IMG_";
    }

    private static string[] BuildPad3() {
        var arr = new string[1000];
        for (int i = 0; i < 1000; i++) {
            if (i < 10) arr[i] = "00" + i;
            else if (i < 100) arr[i] = "0" + i;
            else arr[i] = i.ToString();
        }
        return arr;
    }
}
