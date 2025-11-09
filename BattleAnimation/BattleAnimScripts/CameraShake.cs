using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [Tooltip("The Transform that should be shaken")]
    public Transform target;

    private Vector3 _initialPos;
    private float   _duration;
    private float   _magnitude;
    private float   _dampingSpeed = 1.0f;

    void OnEnable()
    {
        if (target == null) target = Camera.main.transform;
        _initialPos = target.localPosition;
    }

    public void Shake(float duration, float magnitude)
    {
        _duration  = duration;
        _magnitude = magnitude;
        StopAllCoroutines();
        StartCoroutine(DoShake());
    }

    private IEnumerator DoShake()
    {
        float elapsed = 0f;
        while (elapsed < _duration)
        {
            Vector3 offset = Random.insideUnitSphere * _magnitude;
            target.localPosition = _initialPos + offset;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Smooth back
        float t = 0f;
        Vector3 start = target.localPosition;
        while (t < 1f)
        {
            target.localPosition = Vector3.Lerp(start, _initialPos, t);
            t += Time.deltaTime * _dampingSpeed;
            yield return null;
        }
        target.localPosition = _initialPos;
    }
}
