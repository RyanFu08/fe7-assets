using UnityEngine;
using UnityEngine.UI;    // LayoutElement
using System;            // for Math
public class infopanel : MonoBehaviour {
    private int hi_y = 44;
    private int lo_y = 20;
    public string dir = "stop";
    private float time_since_down = 0f;
    [SerializeField] private float timer = 0f, threshold = 0.01f, dist = 0.5f;
    void Start() {

    }

    void Update()
    {
        if (dir == "stop" && transform.localPosition.y <= lo_y) {
            time_since_down += Time.deltaTime;
            if (time_since_down >= 4f) {
                dir = "up";
                time_since_down = 0f;
            }
        }
        if (dir == "stop") return;
        if (dir == "up")
        {
            timer += Time.deltaTime;
            if (timer >= threshold)
            {
                timer = 0f;
                transform.localPosition = new Vector3(transform.localPosition.x, Math.Min(transform.localPosition.y + dist, hi_y), transform.localPosition.z);
                if (transform.localPosition.y >= hi_y) dir = "stop";
            }
        }
        else if (dir == "down")
        {
            timer += Time.deltaTime;
            if (timer >= threshold)
            {
                timer = 0f;
                transform.localPosition = new Vector3(transform.localPosition.x, Math.Max(transform.localPosition.y - dist, lo_y), transform.localPosition.z);
                if (transform.localPosition.y <= lo_y) dir = "stop";
            }
        }
    }

}
