using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class CleanVideoBootstrap : MonoBehaviour
{
    [Header("Assign either a VideoClip, or place an MP4 in Assets/StreamingAssets/")]
    public VideoClip clip;                      // optional: assign in Inspector
    public string streamingAssetsFileName = "demo.mp4"; // used if clip is null

    private AspectRatioFitter fitter;

    void Start()
    {
        // 1) RenderTexture target (starts black)
        var rt = new RenderTexture(1280, 720, 0) { name = "VideoRT" };
        rt.Create();

        // 2) Full-screen Canvas + RawImage
        var canvasGO = new GameObject("VideoCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var rawGO = new GameObject("VideoImage", typeof(RawImage));
        rawGO.transform.SetParent(canvasGO.transform, false);
        var raw = rawGO.GetComponent<RawImage>();
        raw.texture = rt;               // shows black until frames arrive
        raw.raycastTarget = false;

        var rect = raw.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;  rect.offsetMax = Vector2.zero;

        fitter = rawGO.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

        // 3) VideoPlayer -> RenderTexture
        var vp = new GameObject("VideoPlayer", typeof(VideoPlayer)).GetComponent<VideoPlayer>();
        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.targetTexture = rt;
        vp.isLooping = true;
        vp.playOnAwake = false;
        vp.waitForFirstFrame = true;
        vp.aspectRatio = VideoAspectRatio.NoScaling;  // UI fitter handles aspect
        vp.audioOutputMode = VideoAudioOutputMode.None; // set to AudioSource if you want sound

        // Logs + prepare/play
        vp.errorReceived += (_, e) => Debug.LogError("[Video] " + e);
        vp.prepareCompleted += _ => {
            Debug.Log("[Video] Prepared, playing");
            if (vp.texture && vp.texture.height != 0)
                fitter.aspectRatio = (float)vp.texture.width / vp.texture.height;
            vp.Play();
        };

        // Source selection: VideoClip first, else URL from StreamingAssets
        if (clip != null) {
            vp.source = VideoSource.VideoClip;
            vp.clip = clip;
            Debug.Log("[Video] Using VideoClip: " + clip.name);
        } else {
            var url = System.IO.Path.Combine(Application.streamingAssetsPath, streamingAssetsFileName);
            vp.source = VideoSource.Url;
            vp.url = url;
            Debug.Log("[Video] Using URL: " + url);
        }

        vp.Prepare();
    }
}
