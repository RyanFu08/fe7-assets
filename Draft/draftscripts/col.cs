using UnityEngine;
using UnityEngine.UI;    // LayoutElement
using System;            // for Math
using System.Collections.Generic;

public class col : MonoBehaviour
{
  public string status = "invis";
  public Sprite red, blue;
  public float timer = 0.0f;
  [SerializeField] private float update_duration = 0.2f;

  void Start()
  {
    SpriteRenderer sr = GetComponent<SpriteRenderer>();
    double opacity = 0.0;
    Color new_color = new Color(sr.color.r, sr.color.g, sr.color.b, (float)opacity);
    sr.color = new_color;
  }

  void Update()
  {
    if (status == "on")
    {
      SpriteRenderer sr = GetComponent<SpriteRenderer>();
      double opacity = 1.0;
      Color new_color = new Color(sr.color.r, sr.color.g, sr.color.b, (float)opacity);
      sr.color = new_color;
      status = "fading";
      return;
    }

    if (status == "fading")
    {
      timer += Time.deltaTime;
      if (timer >= update_duration)
      {
        timer -= update_duration;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        double opacity = sr.color.a; opacity -= .05;
        if (opacity <= 0.0)
        {
          opacity = 0.0;
          status = "invis";
        }
        Color new_color = new Color(sr.color.r, sr.color.g, sr.color.b,
                                    (float)opacity);
        sr.color = new_color;
      }
    }
  }

  void set_red()
  {
    SpriteRenderer sr = GetComponent<SpriteRenderer>();
    sr.sprite = red;
  }
  void set_blue()
  {
    SpriteRenderer sr = GetComponent<SpriteRenderer>();
    sr.sprite = blue;
  }

  public void trigger_on(int x, int y, string color)
  {
    transform.position = new Vector3(x, y, 0);
    if (color == "red")
    {
      set_red();
    } else
    {
      set_blue();
    }
    status = "on";
  }

}