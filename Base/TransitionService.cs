using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-5000)]
[DisallowMultipleComponent]
public class TransitionService : MonoBehaviour
{
    [Header("Fade Settings")]
    [Min(0.01f)] public float fadeOutDuration = 0.6f;   // to black
    [Min(0.00f)] public float blackHoldSeconds = 0.15f; // hold fully black
    [Min(0.01f)] public float fadeInDuration  = 0.6f;   // from black
    public Color fadeColor = Color.black;
    public bool blockInputDuringFade = true;

    [Header("Stability")]
    [Tooltip("Wait this many rendered frames after a scene load before starting the fade-in.")]
    [Range(0, 4)] public int waitFramesBeforeFadeIn = 2;

    // ------- Singleton (prefers your scene instance) -------
    private static TransitionService _instance;
    public static TransitionService Instance
    {
        get
        {
            if (_instance != null) return _instance;

            _instance = FindObjectOfType<TransitionService>(true);
            if (_instance != null)
            {
                if (_instance._canvas == null) _instance.BuildOverlay();
                DontDestroyOnLoad(_instance.gameObject);
                return _instance;
            }

            var go = new GameObject("~TransitionService");
            _instance = go.AddComponent<TransitionService>();
            DontDestroyOnLoad(go);
            _instance.BuildOverlay();
            return _instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() { _ = Instance; }

    // ------- Public API (use instead of SceneManager.* to include fade-out) -------
    public static void LoadScene(string sceneName) =>
        Instance.StartCoroutine(Instance.FadeLoad(() => SceneManager.LoadSceneAsync(sceneName)));

    public static void LoadScene(int buildIndex) =>
        Instance.StartCoroutine(Instance.FadeLoad(() => SceneManager.LoadSceneAsync(buildIndex)));

    public static void LoadScene(string sceneName, LoadSceneMode mode) =>
        Instance.StartCoroutine(Instance.FadeLoad(() => SceneManager.LoadSceneAsync(sceneName, mode)));

    // ------- Lifecycle -------
    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        BuildOverlay();
        ApplyVisualSettingsNow();
        Debug.Log($"[TransitionService] Active instance: {gameObject.scene.name}/{name}");
    }

    private void OnEnable()  { SceneManager.activeSceneChanged += OnActiveSceneChanged; }
    private void OnDisable() { SceneManager.activeSceneChanged -= OnActiveSceneChanged; }

    private void OnActiveSceneChanged(Scene from, Scene to)
    {
        if (_inFullTransition) return;   // our own flow will handle the fade-in
        StartCoroutine(RevealOnly());    // handle direct SceneManager.LoadScene calls
    }

    // Push inspector changes live (in Edit mode and at runtime)
    private void OnValidate() { ApplyVisualSettingsNow(); }

    /// <summary>Apply current inspector values to the live overlay immediately.</summary>
    [ContextMenu("Apply Visual Settings Now")]
    public void ApplyVisualSettingsNow()
    {
        if (_img != null) _img.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
        if (_cg != null)
        {
            _cg.blocksRaycasts = blockInputDuringFade;
            _cg.interactable   = blockInputDuringFade;
        }
    }

    // ------- Overlay -------
    private Canvas _canvas;
    private Image  _img;
    private CanvasGroup _cg;
    private int _visibleRef = 0;
    private bool _inFullTransition;

    private void BuildOverlay()
    {
        if (_canvas != null && _img != null && _cg != null)
        {
            ApplyVisualSettingsNow();
            return;
        }

        var canvasGO = new GameObject("~TransitionCanvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 32760; // on top

        _cg = canvasGO.AddComponent<CanvasGroup>();
        if (blockInputDuringFade) canvasGO.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasGO);

        var imgGO = new GameObject("~FadeOverlay");
        imgGO.transform.SetParent(canvasGO.transform, false);
        _img = imgGO.AddComponent<Image>();
        _img.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
        _img.raycastTarget = blockInputDuringFade;

        var rt = _img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        _canvas.enabled = false;
        _cg.alpha = 0f;
        _cg.blocksRaycasts = blockInputDuringFade;
        _cg.interactable   = blockInputDuringFade;
    }

    // ------- Camera forcing (prevents white flash) -------
    private struct CamState
    {
        public Camera cam;
        public CameraClearFlags flags;
        public Color bg;
    }

    private List<CamState> ForceCamerasBlack()
    {
        var list = new List<CamState>(Camera.allCamerasCount);
        Camera.GetAllCameras(_tmpCams);
        for (int i = 0; i < _tmpCamsCount; i++)
        {
            var c = _tmpCams[i];
            if (c == null || !c.enabled) continue;
            var s = new CamState { cam = c, flags = c.clearFlags, bg = c.backgroundColor };
            list.Add(s);
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = Color.black;
        }
        return list;
    }

    private void RestoreCameras(List<CamState> states)
    {
        if (states == null) return;
        foreach (var s in states)
        {
            if (s.cam == null) continue;
            s.cam.clearFlags = s.flags;
            s.cam.backgroundColor = s.bg;
        }
    }

    // cache to avoid alloc in ForceCamerasBlack
    private static Camera[] _tmpCams = new Camera[32];
    private static int _tmpCamsCount => Camera.GetAllCameras(_tmpCams);

    // ------- Core flows -------
    private IEnumerator FadeLoad(Func<AsyncOperation> loader)
    {
        _inFullTransition = true;
        BuildOverlay();
        ApplyVisualSettingsNow();

        // Fade out to black
        yield return Fade(0f, 1f, fadeOutDuration);

        if (blackHoldSeconds > 0f)
            yield return new WaitForSeconds(blackHoldSeconds);

        // Load while black
        var op = loader();
        while (!op.isDone) yield return null;

        // Force cameras to black to avoid white clears during reveal
        var saved = ForceCamerasBlack();

        // Let the new scene render a couple of frames before we fade in
        for (int i = 0; i < waitFramesBeforeFadeIn; i++)
            yield return new WaitForEndOfFrame();

        // Fade in from black
        yield return Fade(1f, 0f, fadeInDuration);

        // Restore camera settings
        RestoreCameras(saved);

        _inFullTransition = false;
    }

    private IEnumerator RevealOnly()
    {
        BuildOverlay();
        ApplyVisualSettingsNow();

        // Start fully black over the new scene
        _cg.alpha = 1f;
        ShowOverlay(true);

        // Force cameras to black to avoid white clears
        var saved = ForceCamerasBlack();

        // Let the scene render a frame or two before we fade in
        for (int i = 0; i < Mathf.Max(1, waitFramesBeforeFadeIn); i++)
            yield return new WaitForEndOfFrame();

        // Then fade in
        yield return Fade(1f, 0f, fadeInDuration);

        // Restore camera settings
        RestoreCameras(saved);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        BuildOverlay();
        ApplyVisualSettingsNow();
        ShowOverlay(true);

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;     // unaffected by timescale
            float u = Mathf.Clamp01(t / duration);
            u = u * u * (3f - 2f * u);      // SmoothStep easing
            _cg.alpha = Mathf.Lerp(from, to, u);
            yield return null;
        }
        _cg.alpha = to;

        if (Mathf.Approximately(to, 0f))
            ShowOverlay(false);
    }

    private void ShowOverlay(bool show)
    {
        if (_canvas == null || _cg == null) { BuildOverlay(); if (_canvas == null) return; }

        if (show)
        {
            _visibleRef++;
            if (!_canvas.enabled) _canvas.enabled = true;
            _cg.blocksRaycasts = blockInputDuringFade;
            _cg.interactable   = blockInputDuringFade;
        }
        else
        {
            _visibleRef = Mathf.Max(0, _visibleRef - 1);
            if (_visibleRef == 0)
            {
                _canvas.enabled = false;
                _cg.blocksRaycasts = false;
                _cg.interactable   = false;
            }
        }
    }
}
