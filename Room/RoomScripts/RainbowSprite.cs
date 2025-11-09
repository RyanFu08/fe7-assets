using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class RainbowSprite : MonoBehaviour
{
    public float cyclesPerSecond = 0.2f;
    [Range(0,1)] public float hueOffset = 0f;
    [Range(0,1)] public float saturation = 1f;
    [Range(0,1)] public float value = 1f;

    private SpriteRenderer sr;
    private Sprite originalSprite;
    private Sprite whiteMaskSprite;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        originalSprite = sr.sprite;
        whiteMaskSprite = MakeWhiteMaskSprite(originalSprite);
        if (whiteMaskSprite != null) sr.sprite = whiteMaskSprite;
    }

    void Update()
    {
        float hue = Mathf.Repeat(hueOffset + Time.time * cyclesPerSecond, 1f);
        sr.color = Color.HSVToRGB(hue, saturation, value);
    }

    Sprite MakeWhiteMaskSprite(Sprite src)
    {
        if (src == null || src.texture == null) return null;

        var tex = src.texture;
        if (!tex.isReadable)
        {
            Debug.LogWarning("Enable Read/Write on the sprite texture for RainbowMaskSprite.");
            return null;
        }

        // Use the actual area used by the sprite on the texture (works for atlases)
        Rect r = src.textureRect;
        int x = Mathf.RoundToInt(r.x);
        int y = Mathf.RoundToInt(r.y);
        int w = Mathf.RoundToInt(r.width);
        int h = Mathf.RoundToInt(r.height);

        // Get region as Color[] (rect overload exists only for GetPixels)
        Color[] region = tex.GetPixels(x, y, w, h);

        // Convert to white RGB, preserve alpha
        var pixels32 = new Color32[region.Length];
        for (int i = 0; i < region.Length; i++)
        {
            byte a = (byte)Mathf.RoundToInt(region[i].a * 255f);
            pixels32[i] = new Color32(255, 255, 255, a);
        }

        // Build a new texture the size of the sprite’s rect
        var outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        outTex.filterMode = tex.filterMode;
        outTex.wrapMode = TextureWrapMode.Clamp; // typical for sprites
        outTex.SetPixels32(pixels32);
        outTex.Apply();

        // Preserve pivot and PPU
        Vector2 pivot01 = new Vector2(
            src.pivot.x / src.rect.width,
            src.pivot.y / src.rect.height
        );

        return Sprite.Create(outTex,
                             new Rect(0, 0, w, h),
                             pivot01,
                             src.pixelsPerUnit,
                             0,
                             SpriteMeshType.Tight,
                             src.border);
    }

    void OnDestroy()
    {
        // Clean up generated assets
        if (whiteMaskSprite != null)
        {
            if (whiteMaskSprite.texture != null) Destroy(whiteMaskSprite.texture);
            Destroy(whiteMaskSprite);
        }
        if (sr != null) sr.sprite = originalSprite; // restore original if this component is removed
    }
}
