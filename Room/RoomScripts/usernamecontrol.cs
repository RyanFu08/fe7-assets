using UnityEngine;
using UnityEngine.UI;    // LayoutElement (optional)
using System;            // for Math
using System.Collections;

public class usernamecontrol : MonoBehaviour
{
    public Sprite red, green, blue;
    [SerializeField] private int hi_y;
    [SerializeField] private int lo_y;
    public string dir = "stop";
    private float time_since_down = 0f;
    [SerializeField] private float timer = 0f, threshold = 0.01f, dist = 0.5f;


    private bool isSpinning = false;

    void Start()
    {
    }

    void Update()
    {


        // 2) Movement logic (can early-return here safely).
        if (dir == "stop") return;

        timer += Time.deltaTime;
        if (timer < threshold) return;

        timer = 0f;

        if (dir == "up")
        {
            transform.localPosition = new Vector3(
                transform.localPosition.x,
                Math.Min(transform.localPosition.y + dist, hi_y),
                transform.localPosition.z
            );
            if (transform.localPosition.y >= hi_y) dir = "stop";
        }
        else if (dir == "down")
        {
            transform.localPosition = new Vector3(
                transform.localPosition.x,
                Math.Max(transform.localPosition.y - dist, lo_y),
                transform.localPosition.z
            );
            if (transform.localPosition.y <= lo_y) dir = "stop";
        }
    }
    public void setcolor(string color)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (color == "red") sr.sprite = red;
        else if (color == "green") sr.sprite = green;
        else if (color == "blue") sr.sprite = blue;
    }
}
