using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Transform))]
public class blinker : MonoBehaviour
{
    [SerializeField] float blinkIntervalSeconds = 0.10f; // per step
    [SerializeField] float fadeDurationSeconds  = 1.50f; // to zero
    [SerializeField] float xOffsetFromCam       = -75f;

    public GameObject cam;

    // Sequencing
    int epoch = 0;            // increments on every trigger
    bool runnerStarted = false;

    void Start()
    {
        SetAlphaAll(0f, includeInactive: true);
        StartRunner();
    }

    void OnEnable()  { StartRunner(); }
    void OnDisable() { SetAlphaAll(0f, includeInactive: true); }

    // Public trigger: call this anytime
    public void s()
    {
        unchecked { epoch++; }  // wrap-safe
        StartRunner();
    }

    void StartRunner()
    {
        if (runnerStarted) return;
        runnerStarted = true;
        StartCoroutine(RunForever());
    }

    IEnumerator RunForever()
    {
        // Runs as long as the component is enabled.
        while (enabled && gameObject.activeInHierarchy)
        {
            // Wait until something requests a blink
            int seen = epoch;
            while (seen == epoch)
                yield return null; // idle

            // We observed a new request. Perform a sequence,
            // but if epoch changes mid-sequence, restart from step 1
            // and only finish when a full run completes without changes.
            yield return StartCoroutine(RunSequenceUntilStable());
        }

        runnerStarted = false; // allow restart if re-enabled
    }

    IEnumerator RunSequenceUntilStable()
    {
        for (;;)
        {
            int myEpoch = epoch;

            // Position sync
            if (cam != null)
            {
                var cp = cam.transform.position;
                transform.position = new Vector3(cp.x + xOffsetFromCam, cp.y, 3f);
            }

            // Cache current sprite renderers
            var srs = GetAllSpriteRenderers(includeInactive: true);

            // Hard reset alpha
            SetAlphaAll(0f, includeInactive: true, cached: srs);
            yield return null; // ensure frame applies

            // Blink pattern: 1,0,1,0,1,0,1,0,1
            for (int i = 0; i < 9; i++)
            {
                if (epoch != myEpoch) goto RESTART; // new trigger, restart from top
                float a = (i % 2 == 0) ? 1f : 0f;
                SetAlphaAll(a, includeInactive: true, cached: srs);
                yield return new WaitForSecondsRealtime(blinkIntervalSeconds);
            }

            // Fade to 0 with realtime
            float t = 0f;
            var starts = new float[srs.Count];
            for (int i = 0; i < srs.Count; i++) starts[i] = srs[i] ? srs[i].color.a : 0f;

            while (t < fadeDurationSeconds)
            {
                if (epoch != myEpoch) goto RESTART; // new trigger, restart
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / fadeDurationSeconds);
                for (int i = 0; i < srs.Count; i++)
                {
                    var sr = srs[i];
                    if (!sr) continue;
                    var c = sr.color;
                    c.a = Mathf.Lerp(starts[i], 0f, u);
                    sr.color = c;
                }
                yield return null;
            }

            // Finalize and exit only if no new trigger arrived
            SetAlphaAll(0f, includeInactive: true);
            yield break;

        RESTART:
            // A new trigger landed mid-sequence.
            // Reset alpha and loop to replay from the beginning.
            SetAlphaAll(0f, includeInactive: true);
            yield return null; // give one frame to apply before restarting
            // loop continues to for(;;) top which captures new epoch and restarts
        }
    }

    // Helpers
    List<SpriteRenderer> GetAllSpriteRenderers(bool includeInactive)
    {
        return new List<SpriteRenderer>(
            transform.GetComponentsInChildren<SpriteRenderer>(includeInactive)
        );
    }

    void SetAlphaAll(float a, bool includeInactive, List<SpriteRenderer> cached = null)
    {
        var list = cached ?? GetAllSpriteRenderers(includeInactive);
        for (int i = 0; i < list.Count; i++)
        {
            var sr = list[i];
            if (!sr) continue;
            var c = sr.color; c.a = a; sr.color = c;
        }
    }
}
