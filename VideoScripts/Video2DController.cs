using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(RawImage))]
[RequireComponent(typeof(VideoPlayer))]
[RequireComponent(typeof(AspectRatioFitter))]
public class Video2DController : MonoBehaviour
{
    [Header("Source")]
    public bool useURL = true;            // false if you assign a VideoClip in the inspector
    public string fileName = "video.mp4"; // located in Assets/StreamingAssets
    public VideoClip clip;                // assign if useURL = false

    [Header("Playback")]
    public bool playOnStart = true;
    public bool loop = true;
    public bool mute = true;

    [Header("Scene Transition (optional)")]
    public bool loadSceneOnEnd = false;      // set true to enable scene load on end
    public string nextSceneName = "";        // preferred: load by name
    public int nextSceneBuildIndex = -1;     // fallback: load by index if name is empty
    public float loadDelay = 0f;             // seconds to wait before loading
    public bool allowSkipWithAnyKey = false; // allow user to skip to next scene with any key

    private VideoPlayer vp;
    private RawImage raw;
    private AspectRatioFitter fitter;
    private RenderTexture rt;

    void Awake()
    {
        raw = GetComponent<RawImage>();
        vp = GetComponent<VideoPlayer>();
        fitter = GetComponent<AspectRatioFitter>();

        vp.isLooping = loop;
        vp.audioOutputMode = mute ? VideoAudioOutputMode.None : VideoAudioOutputMode.AudioSource;

        if (useURL)
        {
            vp.source = VideoSource.Url;
            vp.url = Path.Combine(Application.streamingAssetsPath, fileName);
        }
        else
        {
            vp.source = VideoSource.VideoClip;
            vp.clip = clip;
        }

        vp.errorReceived += (s, msg) => Debug.LogError("Video error: " + msg);
        vp.prepareCompleted += OnPrepared;
        vp.loopPointReached += OnVideoFinished; // fires when playback reaches end (only if not looping)
        vp.Prepare(); // async prepare
    }

    void OnPrepared(VideoPlayer source)
    {
        int w = Mathf.Max(2, (int)vp.width);
        int h = Mathf.Max(2, (int)vp.height);

        if (rt != null) { rt.Release(); Destroy(rt); }
        rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
        rt.Create();

        vp.targetTexture = rt;
        raw.texture = rt;

        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = (float)w / h; // preserves aspect and letterboxes/pillarboxes

        if (playOnStart) vp.Play();
    }

    void Update()
    {
        if (allowSkipWithAnyKey && loadSceneOnEnd && Input.anyKeyDown)
        {
            // Optional: skip immediately on key press
            OnVideoFinished(vp);
        }
    }

    public void Play()  { if (vp.isPrepared) vp.Play(); }
    public void Pause() { vp.Pause(); }
    public void StopVideo() { vp.Stop(); }
    public void Seek(double t) { if (vp.canSetTime) vp.time = Mathf.Clamp((float)t, 0f, (float)vp.length); }

    private void OnVideoFinished(VideoPlayer source)
    {
        if (!loadSceneOnEnd) return;

        // If looping is on, this callback is not invoked. Disable looping to use this trigger.
        if (vp.isLooping)
        {
            Debug.LogWarning("Video is set to loop. loopPointReached won't fire. Disable 'loop' to load next scene on end.");
            return;
        }

        StartCoroutine(LoadNextSceneAfterDelay());
    }

    private IEnumerator LoadNextSceneAfterDelay()
    {
        if (loadDelay > 0f) yield return new WaitForSeconds(loadDelay);

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            TransitionService.LoadScene(nextSceneName);
        }
        else if (nextSceneBuildIndex >= 0)
        {
            TransitionService.LoadScene(nextSceneBuildIndex);
        }
        else
        {
            Debug.LogWarning("No next scene specified. Set 'nextSceneName' or 'nextSceneBuildIndex'.");
        }
    }

    void OnDisable()
    {
        if (vp != null) vp.Stop();
    }

    void OnDestroy()
    {
        if (vp != null)
        {
            vp.prepareCompleted -= OnPrepared;
            vp.loopPointReached -= OnVideoFinished;
            vp.errorReceived -= (s, msg) => Debug.LogError("Video error: " + msg); // no-op unsubscribe placeholder
        }

        if (rt != null) { rt.Release(); Destroy(rt); }
    }
}
