using System;
using UnityEngine;
using System.Collections.Generic;

public class UnitControl : MonoBehaviour
{
    GameObject cursor;
    cursor_movement cursor_move;
    cursor_animator cursor_anim;

    private SpriteRenderer sr;
    private Transform tf;

    private static readonly string[] classes = new string[] {
        "Bishop", "Druid", "Eliwood", "Florina", "General", "Hector",
        "Hero", "Lyn", "Nils", "NomadTrooper", "Paladin", "Sage",
        "Sniper", "Valkyrie", "WyvernLord", "Dragon"
    };
    private static readonly HashSet<string> classesSet = new HashSet<string>(classes);

    private static readonly string[] states = new string[] {"stand", "ready", "up", "down", "left", "right", "hide"};

    private static Dictionary<string, Dictionary<string, int[]>> XO_CACHE;
    private static Dictionary<string, Dictionary<string, int[]>> YO_CACHE;

    private Dictionary<string, Dictionary<string, int[]>> xo = new Dictionary<string, Dictionary<string, int[]>>();
    private Dictionary<string, Dictionary<string, int[]>> yo = new Dictionary<string, Dictionary<string, int[]>>();

    private Dictionary<string, float[]> timings = new Dictionary<string, float[]>{
        {"stand", new float[]{0.4f,0.1f,0.4f,0.1f}},
        {"ready", new float[]{0.4f,0.1f,0.4f,0.1f}},
        {"up", new float[]{0.3f,0.1f,0.25f,0.1f}},
        {"down", new float[]{0.3f,0.1f,0.25f,0.1f}},
        {"left", new float[]{0.3f,0.1f,0.25f,0.1f}},
        {"right", new float[]{0.3f,0.1f,0.25f,0.1f}}
    };

    public string ctype = "none", cstate = "stand";
    private Dictionary<string, Sprite[]> anims = new Dictionary<string, Sprite[]>();
    private int cframe = 0;
    private float anim_timer = 0f;

    private float move_speed = 120f;
    public LinkedList<(int, float)> queued_moves = new LinkedList<(int, float)>(); //(direction, amount), 0=up, 1=down, 2=left, 3=right

    private float x_off = 0, y_off = 0;

    // Hot references for the current state/class to avoid per-frame dictionary lookups
    private Sprite[] currentSprites;     // anims[cstate]
    private float[] currentTimings;      // timings[cstate]
    private int[] currentXOffsets;       // xo[ctype][cstate]
    private int[] currentYOffsets;       // yo[ctype][cstate]

    // Track textures we allocate so we can safely destroy them when replacing recolors
    private readonly HashSet<Texture2D> recoloredTextures = new HashSet<Texture2D>();

    // Keep originals from Resources so recolors always start from untouched sprites
    private Dictionary<string, Sprite[]> originals = new Dictionary<string, Sprite[]>();


    void Update() {
        if (ctype == "none") return;
        UpdateAnimation();
        UpdateMovement();
    }

    public void kill_offset() {
        if (x_off == 0f && y_off == 0f) return;
        Vector3 pos = tf.position;
        pos.x -= x_off;
        pos.y -= y_off;
        tf.position = pos;
        x_off = 0f;
        y_off = 0f;
    }

    void UpdateMovement() {
        if (queued_moves.Count == 0) return;

        cursor_anim.hide();

        float move_amt = Time.deltaTime * move_speed;

        var (dir, amt) = queued_moves.First.Value;
        queued_moves.RemoveFirst();

        if (move_amt < amt) {
            queued_moves.AddFirst((dir, amt - move_amt));
        } else {
            move_amt = amt;
        }

        Vector3 pos = tf.position;

        // Keep original behavior for direction mapping
        switch (dir) {
            case 0: // up
                if (cstate != "up") { LoadState("up"); }
                pos.y += move_amt;
                break;
            case 1: // down
                if (cstate != "down") { LoadState("down"); }
                pos.y -= move_amt;
                break;
            case 3: // left -> +x in original code
                if (cstate != "left") { LoadState("left"); }
                pos.x += move_amt;
                break;
            case 2: // right -> -x in original code
                if (cstate != "right") { LoadState("right"); }
                pos.x -= move_amt;
                break;
        }

        tf.position = pos;
        cursor_move.Teleport(tf.position);

        if (queued_moves.Count == 0) {
            kill_offset();
            cursor_move.Teleport(tf.position);
            cursor_move.SnapTargetToGrid16();
            LoadState("stand");
            cursor_anim.show();
        }
    }

    void UpdateAnimation() {
        if (cstate == "hide") {
            if (sr.enabled) sr.enabled = false;
            return;
        }
        if (!sr.enabled) sr.enabled = true;

        anim_timer += Time.deltaTime;

        // Use cached arrays when available to avoid dictionary lookups
        var t = currentTimings ?? timings[cstate];
        var sprites = currentSprites ?? anims[cstate];
        var xarr = currentXOffsets ?? xo[ctype][cstate];
        var yarr = currentYOffsets ?? yo[ctype][cstate];

        int guard = 0;
        while (anim_timer >= t[cframe] && guard++ < 16) {
            // remove old offsets
            kill_offset();

            anim_timer -= t[cframe];
            cframe = (cframe + 1) & 3; // %4

            var nextSprite = sprites[cframe];
            if (sr.sprite != nextSprite) sr.sprite = nextSprite;

            x_off = xarr[cframe];
            y_off = yarr[cframe];

            Vector3 pos = tf.position;
            pos.x += x_off;
            pos.y += y_off;
            tf.position = pos;
        }
    }

    /***PUBLIC ACCESS***/
    [ContextMenu("Make Grayscale")]
    public void MakeGrayscale()
    {
        foreach (var kv in anims)
        {
            Sprite[] frames = kv.Value;
            for (int i = 0; i < frames.Length; ++i)
            {
                var old = frames[i];
                if (old == null) continue;

                // If we previously created this texture, free it
                if (old.texture != null && recoloredTextures.Contains(old.texture)) {
                    Destroy(old.texture);
                    recoloredTextures.Remove(old.texture);
                }

                frames[i] = RecolorSpriteToGray(old, recoloredTextures);
            }
        }

        if (anims.TryGetValue(cstate, out var fr) && fr[cframe] != null)
            sr.sprite = fr[cframe];
    }

    public void MakeBlue() {
        if (ctype == "none") return;

        string ctl = ctype.ToLower();

        foreach (string s in states)
        {
            if (!anims.ContainsKey(s) || anims[s] == null || anims[s].Length != 4)
                anims[s] = new Sprite[4];
            if (!originals.ContainsKey(s) || originals[s] == null || originals[s].Length != 4)
                originals[s] = new Sprite[4];

            for (int i = 1; i <= 4; i++)
            {
                // Load originals back from Resources
                var sprite = Resources.Load<Sprite>($"map_units/{ctype}/{ctl}_{s}_{i}");
                // If previous was a recolored runtime sprite, free its texture
                var old = anims[s][i - 1];
                if (old != null && old.texture != null && recoloredTextures.Contains(old.texture)) {
                    Destroy(old.texture);
                    recoloredTextures.Remove(old.texture);
                }
                anims[s][i - 1] = sprite;
                originals[s][i - 1] = sprite; // keep a mirror of true originals
            }
        }

        // Refresh current cached arrays for this state
        if (ctype != "none") {
            currentSprites  = anims.ContainsKey(cstate) ? anims[cstate] : null;
            currentTimings  = timings.ContainsKey(cstate) ? timings[cstate] : null;
            if (xo.ContainsKey(ctype) && xo[ctype].ContainsKey(cstate)) currentXOffsets = xo[ctype][cstate];
            if (yo.ContainsKey(ctype) && yo[ctype].ContainsKey(cstate)) currentYOffsets = yo[ctype][cstate];
        }

        if (anims.TryGetValue(cstate, out var fr) && fr != null && cframe >= 0 && cframe < fr.Length && fr[cframe] != null) {
            sr.sprite = fr[cframe];
        }
    }

    /*  Converts every non-transparent pixel to luminance grey.  */
    private static Sprite RecolorSpriteToGray(Sprite original, HashSet<Texture2D> pool)
    {
        if (original == null) return null;

        Rect r   = original.textureRect;
        int  texW = (int)r.width, texH = (int)r.height;
        Color[] src = original.texture.GetPixels((int)r.x, (int)r.y, texW, texH);

        for (int p = 0; p < src.Length; ++p)
        {
            Color c = src[p];
            if (c.a < 0.01f) continue;

            float lum = c.r * 0.2126f + c.g * 0.7152f + c.b * 0.0722f;
            src[p] = new Color(lum, lum, lum, c.a);
        }

        Texture2D tex = new Texture2D(texW, texH, TextureFormat.ARGB32, false);
        tex.SetPixels(src);
        tex.Apply();

        tex.filterMode = original.texture.filterMode;
        tex.wrapMode   = original.texture.wrapMode;

        pool.Add(tex);

        return Sprite.Create(tex, new Rect(0, 0, texW, texH), new Vector2(0.5f, 0.5f), original.pixelsPerUnit);
    }

    public void MakeRed()
    {
        // Recolor FROM originals into anims so grayscale does not interfere
        foreach (string s in states)
        {
            if (!anims.ContainsKey(s) || anims[s] == null || anims[s].Length != 4)
                anims[s] = new Sprite[4];

            // If we don't have originals yet for this state, skip gracefully
            if (!originals.ContainsKey(s) || originals[s] == null) continue;

            Sprite[] srcFrames = originals[s];
            for (int i = 0; i < srcFrames.Length; i++)
            {
                var srcSprite = srcFrames[i];
                if (srcSprite == null) continue;

                // Clean up any existing recolored texture currently in anims
                var old = anims[s][i];
                if (old != null && old.texture != null && recoloredTextures.Contains(old.texture)) {
                    Destroy(old.texture);
                    recoloredTextures.Remove(old.texture);
                }

                anims[s][i] = RecolorSpriteToRed(srcSprite, recoloredTextures);
            }
        }

        // Ensure the animator references the updated frames
        currentSprites = anims.ContainsKey(cstate) ? anims[cstate] : null;

        if (currentSprites != null && cframe >= 0 && cframe < currentSprites.Length && currentSprites[cframe] != null)
            sr.sprite = currentSprites[cframe];
    }

    private static Sprite RecolorSpriteToRed(Sprite original, HashSet<Texture2D> pool)
    {
        if (original == null) return null;

        Rect r = original.textureRect;
        int  texW = (int)r.width;
        int  texH = (int)r.height;
        Color[] src = original.texture.GetPixels((int)r.x, (int)r.y, texW, texH);

        const float blueMinHue = 0.50f;   // 180°
        const float blueMaxHue = 0.72f;   // 260°
        const float minSat     = 0.20f;   // ignore greys / whites

        for (int p = 0; p < src.Length; ++p)
        {
            Color c = src[p];
            if (c.a < 0.01f) continue;

            Color.RGBToHSV(c, out float hue, out float sat, out float val);

            if (sat >= minSat && hue >= blueMinHue && hue <= blueMaxHue)
                hue = 0f; // red

            Color outC = Color.HSVToRGB(hue, sat, val);
            outC.a = c.a;
            src[p] = outC;
        }

        var tex = new Texture2D(texW, texH, TextureFormat.ARGB32, false);
        tex.SetPixels(src);
        tex.Apply();
        tex.filterMode = original.texture.filterMode;
        tex.wrapMode   = original.texture.wrapMode;

        pool.Add(tex);

        return Sprite.Create(tex, new Rect(0, 0, texW, texH), new Vector2(0.5f, 0.5f), original.pixelsPerUnit);
    }

    // ===== Make Purple: ONLY modify blue-ish pixels; leave others untouched =====
    [ContextMenu("Make Purple")]
    public void MakePurple()
    {
        foreach (string s in states)
        {
            if (!anims.ContainsKey(s) || anims[s] == null || anims[s].Length != 4)
                anims[s] = new Sprite[4];

            if (!originals.ContainsKey(s) || originals[s] == null) continue;

            Sprite[] srcFrames = originals[s];
            for (int i = 0; i < srcFrames.Length; i++)
            {
                var srcSprite = srcFrames[i];
                if (srcSprite == null) continue;

                var old = anims[s][i];
                if (old != null && old.texture != null && recoloredTextures.Contains(old.texture)) {
                    Destroy(old.texture);
                    recoloredTextures.Remove(old.texture);
                }

                anims[s][i] = RecolorSpriteToPurple(srcSprite, recoloredTextures);
            }
        }

        currentSprites = anims.ContainsKey(cstate) ? anims[cstate] : null;

        if (currentSprites != null && cframe >= 0 && cframe < currentSprites.Length && currentSprites[cframe] != null)
            sr.sprite = currentSprites[cframe];
    }

    private static Sprite RecolorSpriteToPurple(Sprite original, HashSet<Texture2D> pool)
    {
        if (original == null) return null;

        Rect r = original.textureRect;
        int  texW = (int)r.width;
        int  texH = (int)r.height;
        Color[] src = original.texture.GetPixels((int)r.x, (int)r.y, texW, texH);

        // Treat "blue" as ~180°–260° in HSV with sufficient saturation.
        const float blueMinHue = 0.50f;   // 180°
        const float blueMaxHue = 0.72f;   // 260°
        const float minSatIn   = 0.20f;   // only recolor genuinely colored pixels
        const float purpleHue  = 0.78f;   // ~280°
        const float minSatOut  = 0.45f;   // ensure visible purple where source is dull

        for (int p = 0; p < src.Length; ++p)
        {
            Color c = src[p];
            if (c.a < 0.01f) continue; // keep transparent pixels unchanged

            Color.RGBToHSV(c, out float h, out float s, out float v);

            // Only retint if the pixel is blue-ish and not grey/white
            if (s >= minSatIn && h >= blueMinHue && h <= blueMaxHue)
            {
                h = purpleHue;
                s = Mathf.Max(s, minSatOut);
                Color outC = Color.HSVToRGB(h, s, v);
                outC.a = c.a;
                src[p] = outC;
            }
        }

        var tex = new Texture2D(texW, texH, TextureFormat.ARGB32, false);
        tex.SetPixels(src);
        tex.Apply();
        tex.filterMode = original.texture.filterMode;
        tex.wrapMode   = original.texture.wrapMode;

        pool.Add(tex);

        return Sprite.Create(tex, new Rect(0, 0, texW, texH), new Vector2(0.5f, 0.5f), original.pixelsPerUnit);
    }

    // ===== Make Orange: ONLY modify blue-ish pixels; leave others untouched =====
    [ContextMenu("Make Orange")]
    public void MakeOrange()
    {
        foreach (string s in states)
        {
            if (!anims.ContainsKey(s) || anims[s] == null || anims[s].Length != 4)
                anims[s] = new Sprite[4];

            if (!originals.ContainsKey(s) || originals[s] == null) continue;

            Sprite[] srcFrames = originals[s];
            for (int i = 0; i < srcFrames.Length; i++)
            {
                var srcSprite = srcFrames[i];
                if (srcSprite == null) continue;

                var old = anims[s][i];
                if (old != null && old.texture != null && recoloredTextures.Contains(old.texture)) {
                    Destroy(old.texture);
                    recoloredTextures.Remove(old.texture);
                }

                anims[s][i] = RecolorSpriteToOrange(srcSprite, recoloredTextures);
            }
        }

        currentSprites = anims.ContainsKey(cstate) ? anims[cstate] : null;

        if (currentSprites != null && cframe >= 0 && cframe < currentSprites.Length && currentSprites[cframe] != null)
            sr.sprite = currentSprites[cframe];
    }

    private static Sprite RecolorSpriteToOrange(Sprite original, HashSet<Texture2D> pool)
    {
        if (original == null) return null;

        Rect r = original.textureRect;
        int  texW = (int)r.width;
        int  texH = (int)r.height;
        Color[] src = original.texture.GetPixels((int)r.x, (int)r.y, texW, texH);

        // Same blue detection as in Red/Purple.
        const float blueMinHue = 0.50f;   // 180°
        const float blueMaxHue = 0.72f;   // 260°
        const float minSatIn   = 0.20f;   // ignore greys / whites

        // Target orange hue ~30°.
        const float orangeHue  = 30f / 360f; // ~0.0833
        const float minSatOut  = 0.55f;      // push saturation to read as orange

        for (int p = 0; p < src.Length; ++p)
        {
            Color c = src[p];
            if (c.a < 0.01f) continue;

            Color.RGBToHSV(c, out float h, out float s, out float v);

            // Only retint blue-ish pixels
            if (s >= minSatIn && h >= blueMinHue && h <= blueMaxHue)
            {
                h = orangeHue;
                s = Mathf.Max(s, minSatOut);
                Color outC = Color.HSVToRGB(h, s, v);
                outC.a = c.a;
                src[p] = outC;
            }
        }

        var tex = new Texture2D(texW, texH, TextureFormat.ARGB32, false);
        tex.SetPixels(src);
        tex.Apply();
        tex.filterMode = original.texture.filterMode;
        tex.wrapMode   = original.texture.wrapMode;

        pool.Add(tex);

        return Sprite.Create(tex, new Rect(0, 0, texW, texH), new Vector2(0.5f, 0.5f), original.pixelsPerUnit);
    }

    public void LoadState(string st) {
        kill_offset();

        cstate = st;
        cframe = 0;
        anim_timer = 0f;

        if (ctype == "none") { sr.enabled = false; return; }

        sr.enabled = true;

        // Cache references for this state to minimize per-frame dictionary lookups
        currentSprites  = anims.ContainsKey(cstate) ? anims[cstate] : null;
        currentTimings  = timings.ContainsKey(cstate) ? timings[cstate] : null;
        currentXOffsets = (xo.ContainsKey(ctype) && xo[ctype].ContainsKey(cstate)) ? xo[ctype][cstate] : null;
        currentYOffsets = (yo.ContainsKey(ctype) && yo[ctype].ContainsKey(cstate)) ? yo[ctype][cstate] : null;

        if (currentSprites != null) sr.sprite = currentSprites[0];

        // Apply frame-0 offsets immediately
        if (currentXOffsets != null && currentYOffsets != null) {
            x_off = currentXOffsets[0];
            y_off = currentYOffsets[0];
            Vector3 pos = tf.position;
            pos.x += x_off;
            pos.y += y_off;
            tf.position = pos;
        } else {
            x_off = 0f; y_off = 0f;
        }
    }

    public void Load(string ct, string st = "stand") {
        if (!classesSet.Contains(ct)) {
            Debug.Log("Class `" + ct + "` could not be loaded!");
            return;
        }
        string ctl = ct.ToLower();
        foreach (string s in states) {
            anims[s] = new Sprite[4];
            if (!originals.ContainsKey(s) || originals[s] == null || originals[s].Length != 4)
                originals[s] = new Sprite[4];
            for (int i=1; i<=4; i++) {
                var sp = Resources.Load<Sprite>("map_units/"+ct+"/"+ctl+"_"+s+"_"+i);
                anims[s][i-1] = sp;
                originals[s][i-1] = sp; // mirror originals on load
            }
        }
        ctype = ct;

        // Ensure xo/yo caches are available before using LoadState
        if (XO_CACHE == null) XO_CACHE = BuildXO();
        if (YO_CACHE == null) YO_CACHE = BuildYO();
        xo = XO_CACHE;
        yo = YO_CACHE;

        LoadState(st);
    }

    public void QueueMove(int dir, float amt) {
        queued_moves.AddLast((dir,amt));
    }

    public void Teleport(int x, int y) {
        kill_offset();
        Vector3 pos = tf.position;
        pos.x = x;
        pos.y = y;
        tf.position = pos;
        anim_timer = 0f;
    }

    public void Awake() {
        sr = GetComponent<SpriteRenderer>();
        tf = transform;

        cursor = GameObject.Find("Cursor");
        cursor_move = cursor.GetComponent<cursor_movement>();
        cursor_anim = cursor.GetComponent<cursor_animator>();

        // Build shared offset tables once for all instances
        if (XO_CACHE == null) XO_CACHE = BuildXO();
        if (YO_CACHE == null) YO_CACHE = BuildYO();
        xo = XO_CACHE;
        yo = YO_CACHE;
    }

    // ===== Static builders so these large tables are allocated once, not per instance =====
    private static Dictionary<string, Dictionary<string, int[]>> BuildXO() {
        var d = new Dictionary<string, Dictionary<string, int[]>>();

        d["Dragon"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {0, 0, 0, 0},
            ["ready"] = new int[] {-2, -2, -1, -2},
            ["up"]    = new int[] {0, 0, 0, 0},
            ["down"]  = new int[] {0, 0, 0, 0},
            ["left"]  = new int[] {0, 0, 0, 0},
            ["right"] = new int[] {0, 0, 0, 0}
        };

        d["Bishop"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {0, 0, 1, 0},
            ["ready"] = new int[] {-2, -2, -1, -2},
            ["up"]    = new int[] {0, 0, 0, 0},
            ["down"]  = new int[] {0, 0, 0, 0},
            ["left"]  = new int[] {0, 0, 0, 0},
            ["right"] = new int[] {0, 0, 0, 0}
        };
        d["Druid"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {0, 0, 0, 0},
            ["ready"] = new int[] {0, 0, 0, 0},
            ["up"]    = new int[] {0, 0, 0, 0},
            ["down"]  = new int[] {0, 0, 0, 0},
            ["left"]  = new int[] {0, 0, 0, 0},
            ["right"] = new int[] {0, 0, 0, 0}
        };
        d["Eliwood"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {0, 0, 0, 0},
            ["ready"] = new int[] {0, 0, 0, 0},
            ["up"]    = new int[] {0, 0, 0, 0},
            ["down"]  = new int[] {0, 0, 0, 0},
            ["left"]  = new int[] {0, 0, 0, 0},
            ["right"] = new int[] {0, 0, 0, 0}
        };
        d["Florina"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {0, 0, 0, 0},
            ["ready"] = new int[] {0, 0, 0, 0},
            ["up"]    = new int[] {0, 0, 0, 0},
            ["down"]  = new int[] {0, 0, 0, 0}, // keep structure identical
            ["left"]  = new int[] {0, 0, 0, 0},
            ["right"] = new int[] {0, 0, 0, 0}
        };
        d["General"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {0, 0, 0, 0},
            ["ready"] = new int[] {-6, -5, -5, -5},
            ["up"]    = new int[] {0, 0, 0, 0},
            ["down"]  = new int[] {0, 0, 0, 0},
            ["left"]  = new int[] {0, 0, 0, 0},
            ["right"] = new int[] {0, 0, 0, 0}
        };
        d["Hector"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {0, 0, 0, 0},
            ["ready"] = new int[] {0, -5, -5, -5},
            ["up"]    = new int[] {0, 0, 0, 0},
            ["down"]  = new int[] {0, 0, 0, 0},
            ["left"]  = new int[] {0, 0, 0, 0},
            ["right"] = new int[] {0, 0, 0, 0}
        };
        d["Hero"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {0, 0, 0, 0},
            ["ready"] = new int[] {-2, -3, -3, -3},
            ["up"]    = new int[] {0, 0, 0, 0},
            ["down"]  = new int[] {0, 0, 0, 0},
            ["left"]  = new int[] {0, 0, 0, 0},
            ["right"] = new int[] {0, 0, 0, 0}
        };
        d["Lyn"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {0, 2, 1, 2},
            ["ready"] = new int[] {0, 0, 0, 0},
            ["up"]    = new int[] {0, 0, 0, 0},
            ["down"]  = new int[] {0, 0, 0, 0},
            ["left"]  = new int[] {0, 0, 0, 0},
            ["right"] = new int[] {0, 0, 0, 0}
        };
        d["Nils"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {0, 0, -1, 0},
            ["ready"] = new int[] {0, 0, 0, 0},
            ["up"]    = new int[] {0, 0, 0, 0},
            ["down"]  = new int[] {0, 0, 0, 0},
            ["left"]  = new int[] {0, 0, 0, 0},
            ["right"] = new int[] {0, 0, 0, 0}
        };
        d["NomadTrooper"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {0, 0, 0, 0},
            ["ready"] = new int[] {0, 0, 0, 0},
            ["up"]    = new int[] {0, 0, 0, 0},
            ["down"]  = new int[] {0, 0, 0, 0},
            ["left"]  = new int[] {0, 0, 0, 0},
            ["right"] = new int[] {0, 0, 0, 0}
        };
        d["Paladin"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {0, 0, 0, 0},
            ["ready"] = new int[] {0, 0, 0, 0},
            ["up"]    = new int[] {0, 0, 0, 0},
            ["down"]  = new int[] {0, 0, 0, 0},
            ["left"]  = new int[] {0, 0, 0, 0},
            ["right"] = new int[] {0, 0, 0, 0}
        };
        d["Sage"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {0, 0, 0, 0},
            ["ready"] = new int[] {3, 2, 2, 2},
            ["up"]    = new int[] {0, 0, 0, 0},
            ["down"]  = new int[] {0, 0, 0, 0},
            ["left"]  = new int[] {0, 0, 0, 0},
            ["right"] = new int[] {0, 0, 0, 0}
        };
        d["Sniper"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {-1, 0, 0, 0},
            ["ready"] = new int[] {2, 2, 2, 2},
            ["up"]    = new int[] {0, 0, 0, 0},
            ["down"]  = new int[] {0, 0, 0, 0},
            ["left"]  = new int[] {0, 0, 0, 0},
            ["right"] = new int[] {0, 0, 0, 0}
        };
        d["Valkyrie"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {0, 0, 0, 0},
            ["ready"] = new int[] {0, 0, 0, 0},
            ["up"]    = new int[] {0, 0, 0, 0},
            ["down"]  = new int[] {0, 0, 0, 0},
            ["left"]  = new int[] {0, 0, 0, 0},
            ["right"] = new int[] {0, 0, 0, 0}
        };
        d["WyvernLord"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {0, 0, 0, 0},
            ["ready"] = new int[] {0, 0, -3, 0},
            ["up"]    = new int[] {0, 0, 0, 0},
            ["down"]  = new int[] {0, 0, 0, 0},
            ["left"]  = new int[] {0, 0, 0, 0},
            ["right"] = new int[] {0, 0, 0, 0}
        };

        return d;
    }

    private static Dictionary<string, Dictionary<string, int[]>> BuildYO() {
        var d = new Dictionary<string, Dictionary<string, int[]>>();

        d["Dragon"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {0, 0, 0, 0},
            ["ready"] = new int[] {8, 9, 12, 9},
            ["up"]    = new int[] {0, 0, 0, 0},
            ["down"]  = new int[] {0, 0, 0, 0},
            ["left"]  = new int[] {0, 0, 0, 0},
            ["right"] = new int[] {0, 0, 0, 0}
        };

        d["Bishop"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {2, 2, 2, 2},
            ["ready"] = new int[] {8, 9, 12, 9},
            ["up"]    = new int[] {0, 0, 0, 0},
            ["down"]  = new int[] {0, 0, 0, 0},
            ["left"]  = new int[] {0, 0, 0, 0},
            ["right"] = new int[] {0, 0, 0, 0}
        };
        d["Druid"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {0, 0, 1, 0},
            ["ready"] = new int[] {2, 2, 2, 2},
            ["up"]    = new int[] {0, 0, 0, 0},
            ["down"]  = new int[] {0, 0, 0, 0},
            ["left"]  = new int[] {0, 0, 0, 0},
            ["right"] = new int[] {0, 0, 0, 0}
        };
        d["Eliwood"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {3, 3, 3, 3},
            ["ready"] = new int[] {8, 8, 8, 8},
            ["up"]    = new int[] {5, 5, 5, 5},
            ["down"]  = new int[] {5, 5, 5, 5},
            ["left"]  = new int[] {5, 5, 5, 5},
            ["right"] = new int[] {5, 5, 5, 5}
        };
        d["Florina"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {3, 3, 3, 3},
            ["ready"] = new int[] {8, 8, 8, 8},
            ["up"]    = new int[] {5, 5, 5, 5},
            ["down"]  = new int[] {5, 5, 5, 5},
            ["left"]  = new int[] {5, 5, 5, 5},
            ["right"] = new int[] {5, 5, 5, 5}
        };
        d["General"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {3, 3, 3, 3},
            ["ready"] = new int[] {5, 6, 6, 6},
            ["up"]    = new int[] {5, 5, 5, 5},
            ["down"]  = new int[] {5, 5, 5, 5},
            ["left"]  = new int[] {5, 5, 5, 5},
            ["right"] = new int[] {5, 5, 5, 5}
        };
        d["Hector"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {3, 3, 3, 3},
            ["ready"] = new int[] {5, 5, 5, 5},
            ["up"]    = new int[] {5, 5, 5, 5},
            ["down"]  = new int[] {5, 5, 5, 5},
            ["left"]  = new int[] {5, 5, 5, 5},
            ["right"] = new int[] {5, 5, 5, 5}
        };
        d["Hero"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {2, 2, 2, 2},
            ["ready"] = new int[] {7, 7, 9, 7},
            ["up"]    = new int[] {0, 0, 0, 0},
            ["down"]  = new int[] {0, 0, 0, 0},
            ["left"]  = new int[] {0, 0, 0, 0},
            ["right"] = new int[] {0, 0, 0, 0}
        };
        d["Lyn"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {2, 2, 1, 2},
            ["ready"] = new int[] {5, 5, 5, 5},
            ["up"]    = new int[] {5, 5, 5, 5},
            ["down"]  = new int[] {0, 0, 0, 0},
            ["left"]  = new int[] {0, 0, 0, 0},
            ["right"] = new int[] {1, 1, 1, 1}
        };
        d["Nils"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {0, 0, 0, 0},
            ["ready"] = new int[] {0, 0, 0, 0},
            ["up"]    = new int[] {0, 0, 0, 0},
            ["down"]  = new int[] {0, 0, 0, 0},
            ["left"]  = new int[] {0, 0, 0, 0},
            ["right"] = new int[] {0, 0, 0, 0}
        };
        d["NomadTrooper"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {3, 3, 3, 3},
            ["ready"] = new int[] {8, 8, 8, 8},
            ["up"]    = new int[] {5, 5, 5, 5},
            ["down"]  = new int[] {5, 5, 5, 5},
            ["left"]  = new int[] {5, 5, 5, 5},
            ["right"] = new int[] {5, 5, 5, 5}
        };
        d["Paladin"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {3, 3, 3, 3},
            ["ready"] = new int[] {7, 7, 7, 7},
            ["up"]    = new int[] {5, 5, 5, 5},
            ["down"]  = new int[] {5, 5, 5, 5},
            ["left"]  = new int[] {5, 5, 5, 5},
            ["right"] = new int[] {5, 5, 5, 5}
        };
        d["Sage"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {2, 2, 2, 2},
            ["ready"] = new int[] {2, 2, 3, 2},
            ["up"]    = new int[] {0, 0, 0, 0},
            ["down"]  = new int[] {0, 0, 0, 0},
            ["left"]  = new int[] {0, 0, 0, 0},
            ["right"] = new int[] {0, 0, 0, 0}
        };
        d["Sniper"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {0, 0, 0, 0},
            ["ready"] = new int[] {0, 0, 0, 0},
            ["up"]    = new int[] {0, 0, 0, 0},
            ["down"]  = new int[] {0, 0, 0, 0},
            ["left"]  = new int[] {0, 0, 0, 0},
            ["right"] = new int[] {0, 0, 0, 0}
        };
        d["Valkyrie"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {3, 3, 3, 3},
            ["ready"] = new int[] {8, 8, 8, 8},
            ["up"]    = new int[] {5, 5, 5, 5},
            ["down"]  = new int[] {5, 5, 5, 5},
            ["left"]  = new int[] {5, 5, 5, 5},
            ["right"] = new int[] {5, 5, 5, 5}
        };
        d["WyvernLord"] = new Dictionary<string, int[]> {
            ["stand"] = new int[] {3, 3, 3, 3},
            ["ready"] = new int[] {5, 0, 0, 0},
            ["up"]    = new int[] {5, 0, 0, 0},
            ["down"]  = new int[] {5, 0, 0, 0},
            ["left"]  = new int[] {5, 0, 0, 0},
            ["right"] = new int[] {5, 0, 0, 0}
        };

        return d;
    }
}
