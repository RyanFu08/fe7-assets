// PX.cs
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;   // URP’s PixelPerfectCamera

[RequireComponent(typeof(PixelPerfectCamera), typeof(Camera))]
public class PX : MonoBehaviour
{
    [Header("Reference Resolutions (px)")]
    public int w = 240;
    public int h = 160;

    [Header("State Machine")]
    public string state = "static";    // "static", "increase", or "decrease"
    public float timer = 0f;
    public float t = 1f;               // duration of zoom in seconds

    private PixelPerfectCamera _ppc;
    private Camera            _cam;

    // these will be sampled at Start()
    private float minOrthoSize;
    private float maxOrthoSize;

    void Awake()
    {
        _ppc = GetComponent<PixelPerfectCamera>();
        _cam = GetComponent<Camera>();
        if (_ppc == null) Debug.LogError("PX: no PixelPerfectCamera!", this);
        if (_cam == null) Debug.LogError("PX: no Camera!", this);
    }

    IEnumerator Start()
    {
        // 1) Ensure PPC is enabled and at the small res
        _ppc.enabled = true;
        ApplyReferenceResolution(240, 160);
        yield return null;             // wait a frame for the camera to update
        minOrthoSize = _cam.orthographicSize;

        // 2) Temporarily switch to large res and sample again
        ApplyReferenceResolution(480, 320);
        yield return null;
        maxOrthoSize = _cam.orthographicSize;

        // 3) Restore initial
        ApplyReferenceResolution(240, 160);
        _cam.orthographicSize = minOrthoSize;
    }

    void Update()
    {
        // — TRANSITION PHASE —
        if (state != "static")
        {
            timer += Time.deltaTime;

            // disable pixel-perfect while tweening
            _ppc.enabled = false;

            float tNorm = Mathf.Clamp01(timer / t);

            if (state == "increase")
                _cam.orthographicSize = Mathf.Lerp(minOrthoSize, maxOrthoSize, tNorm);
            else // "decrease"
                _cam.orthographicSize = Mathf.Lerp(maxOrthoSize, minOrthoSize, tNorm);

            if (timer >= t)
            {
                // finalize logical w/h
                if (state == "increase") { w = 480; h = 320; }
                else                      { w = 240; h = 160; }

                // end transition
                state = "static";
                timer = 0f;

                // re-enable pixel-perfect and apply final res
                _ppc.enabled = true;
                ApplyReferenceResolution(w, h);
            }
        }
        // — IDLE PHASE —
        else if (Input.GetKeyDown(KeyCode.C))
        {
            state = (w == 240) ? "increase" : "decrease";
            timer = 0f;
        }
    }

    /// <summary>
    /// Apply the reference resolution to the URP PixelPerfectCamera.
    /// </summary>
    public void ApplyReferenceResolution(int width, int height)
    {
        if (_ppc == null) return;
        _ppc.refResolutionX = Mathf.Max(1, width);
        _ppc.refResolutionY = Mathf.Max(1, height);
    }
}
