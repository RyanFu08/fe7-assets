// LoopingStreamingVideo2D_Quad.cs
// 2D-friendly: renders full video onto a MeshRenderer quad in XY.
// No Canvas. No RectTransform. Plays from StreamingAssets, loops forever.
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;

public class videoscript : MonoBehaviour
{
    public enum FitMode { Contain, Cover, Stretch }

    [Header("Source")]
    public string videoFileName = "demo.mp4";

    [Header("Surface & Sorting")]
    public Renderer targetRenderer;                 // optional; if null a child quad is created
    public string sortingLayerName = "Default";
    public int sortingOrder = 0;

    [Header("Sizing (world units)")]
    [Tooltip("Desired width in world units if Fit = Contain or Stretch. For Cover, this is the minimum width.")]
    public float width = 4f;
    public FitMode fit = FitMode.Contain;           // full-frame by default, no cropping
    public bool maintainAspect = true;

    [Header("Audio")]
    [Range(0f,1f)] public float volume = 1f;
    public bool mute = false;

    private VideoPlayer _vp;
    private AudioSource _audio;
    private RenderTexture _rt;
    private Transform _surfaceRoot;

    void Awake()
    {
        EnsureSurface();
        EnsureVideoPlayer();
        EnsureAudio();
    }

    void Start()
    {
        StartCoroutine(PrepareAndPlay());
    }

    private void EnsureSurface()
    {
        if (targetRenderer != null) return;

        _surfaceRoot = new GameObject("VideoSurface2D_Quad").transform;
        _surfaceRoot.SetParent(transform, false);
        _surfaceRoot.localPosition = Vector3.zero;
        _surfaceRoot.localRotation = Quaternion.identity;

        var mf = _surfaceRoot.gameObject.AddComponent<MeshFilter>();
        var mr = _surfaceRoot.gameObject.AddComponent<MeshRenderer>();
        mf.sharedMesh = CreateQuadMeshXY(); // 1x1 quad in XY, facing +Z

        // Unlit shader works in all pipelines (URP/HDRP/Built-in)
        Shader unlit =
            Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("HDRP/Unlit") ??
            Shader.Find("Unlit/Texture");
        mr.sharedMaterial = new Material(unlit);

        // Make it “2D-like” in draw order
        TrySetSorting(mr, sortingLayerName, sortingOrder);

        targetRenderer = mr;
    }

    private void EnsureVideoPlayer()
    {
        _vp = gameObject.GetComponent<VideoPlayer>();
        if (_vp == null) _vp = gameObject.AddComponent<VideoPlayer>();

        _vp.playOnAwake = false;
        _vp.waitForFirstFrame = true;
        _vp.isLooping = true;
        _vp.renderMode = VideoRenderMode.RenderTexture; // pipe into a RT
        _vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
        _vp.EnableAudioTrack(0, true);
        _vp.prepareCompleted += OnPrepared;
    }

    private void EnsureAudio()
    {
        _audio = gameObject.GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _audio.loop = true;
        _vp.SetTargetAudioSource(0, _audio);
    }

    private IEnumerator PrepareAndPlay()
    {
        string resolvedUrl = null;

#if UNITY_ANDROID && !UNITY_EDITOR
        string saPath = Path.Combine(Application.streamingAssetsPath, videoFileName);
        using (var req = UnityWebRequest.Get(saPath))
        {
            yield return req.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            { Debug.LogError($"Failed to read StreamingAssets video: {req.error}"); yield break; }

            string outPath = Path.Combine(Application.persistentDataPath, videoFileName);
            try { File.WriteAllBytes(outPath, req.downloadHandler.data); }
            catch (System.Exception e) { Debug.LogError($"Write failed: {e.Message}"); yield break; }
            resolvedUrl = "file://" + outPath;
        }
#else
        string p = Path.Combine(Application.streamingAssetsPath, videoFileName);
        if (!File.Exists(p)) { Debug.LogError($"Video not found at: {p}"); yield break; }
        resolvedUrl = p;
#endif

        _audio.mute = mute;
        _audio.volume = volume;

        _vp.source = VideoSource.Url;
        _vp.url = resolvedUrl;
        _vp.Prepare();

        float timeout = 10f;
        while (!_vp.isPrepared && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }
        if (!_vp.isPrepared) { Debug.LogError("VideoPlayer prepare timed out."); yield break; }

        SetupRTAndSizing();

        _vp.Play();
        if (!mute) _audio.Play();
    }

    private void OnPrepared(VideoPlayer vp)
    {
        SetupRTAndSizing();
        if (!vp.isPlaying) vp.Play();
        if (!_audio.isPlaying && !mute) _audio.Play();
    }

    private void SetupRTAndSizing()
    {
        int vw = Mathf.Max(2, (int)_vp.width);
        int vh = Mathf.Max(2, (int)_vp.height);

        if (_rt != null && (_rt.width != vw || _rt.height != vh))
        {
            _vp.targetTexture = null;
            _rt.Release();
            Destroy(_rt);
            _rt = null;
        }
        if (_rt == null)
        {
            _rt = new RenderTexture(vw, vh, 0, RenderTextureFormat.ARGB32)
            { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        }
        _vp.targetTexture = _rt;

        // Bind RT to material
        if (targetRenderer != null)
        {
            // Unique material instance to avoid side effects
            if (!targetRenderer.material || targetRenderer.sharedMaterial == targetRenderer.material)
                targetRenderer.material = new Material(targetRenderer.sharedMaterial);
            targetRenderer.material.mainTexture = _rt;
        }

        ApplySizing();
    }

    private void ApplySizing()
    {
        float w = Mathf.Max(0.01f, width);
        float h = w;

        if (_vp.isPrepared && maintainAspect)
        {
            float aspect = Mathf.Max(0.01f, (float)_vp.width / Mathf.Max(1f, _vp.height));
            switch (fit)
            {
                case FitMode.Contain:
                    h = w / aspect;           // full frame, letterbox if needed
                    break;
                case FitMode.Cover:
                    // fill height at least as large as width/aspect
                    h = Mathf.Max(w / aspect, h);
                    break;
                case FitMode.Stretch:
                    // keep h = w (no aspect lock)
                    break;
            }
        }

        var t = targetRenderer != null ? targetRenderer.transform : transform;
        t.localScale = new Vector3(w, h, 1f); // XY plane quad scaled to world units
    }

    private static Mesh CreateQuadMeshXY()
    {
        var m = new Mesh { name = "QuadXY" };
        m.vertices = new[]
        {
            new Vector3(-0.5f,-0.5f,0f),
            new Vector3( 0.5f,-0.5f,0f),
            new Vector3(-0.5f, 0.5f,0f),
            new Vector3( 0.5f, 0.5f,0f),
        };
        m.uv = new[] { new Vector2(0,0), new Vector2(1,0), new Vector2(0,1), new Vector2(1,1) };
        m.triangles = new[] { 0,2,1, 2,3,1 };
        m.RecalculateNormals();
        return m;
    }

    private static void TrySetSorting(Renderer r, string layer, int order)
    {
        // MeshRenderer doesn’t expose sorting directly until 5.6+, but these fields exist
        r.sortingLayerName = layer;
        r.sortingOrder = order;
    }

    void OnDestroy()
    {
        if (_rt != null)
        {
            if (_vp) _vp.targetTexture = null;
            _rt.Release();
            Destroy(_rt);
        }
    }

    // Simple controls
    public void SetVolume(float v) { volume = Mathf.Clamp01(v); if (_audio) _audio.volume = volume; }
    public void SetMute(bool m)    { mute = m; if (_audio) _audio.mute = mute; }
}
