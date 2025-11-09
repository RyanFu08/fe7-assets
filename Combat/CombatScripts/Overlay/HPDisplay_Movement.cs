using UnityEngine;

public class HPDisplay_Movement : MonoBehaviour
{
    [SerializeField] private float show_x = -70f;
    [SerializeField] private float hide_x = -140f;
    [SerializeField] private float glide_speed = 80f;

    [SerializeField] private GameObject name_;
    [SerializeField] private GameObject stat;

    private string state = "hidden"; // "hidden", "shown", "to_hidden", "to_shown"

    void Update()
    {
        RectTransform rt = GetComponent<RectTransform>();

        if (state == "to_hidden")
        {
            Vector2 target = new Vector2(hide_x, rt.anchoredPosition.y);
            rt.anchoredPosition = Vector2.MoveTowards(rt.anchoredPosition, target, glide_speed * Time.deltaTime);
            if (rt.anchoredPosition.x <= hide_x) state = "hidden";
        }
        else if (state == "to_shown")
        {
            Vector2 target = new Vector2(show_x, rt.anchoredPosition.y);
            rt.anchoredPosition = Vector2.MoveTowards(rt.anchoredPosition, target, glide_speed * Time.deltaTime);
            if (rt.anchoredPosition.x >= show_x) state = "shown";
        }
    }

    /*** PUBLIC METHODS ***/
    public void hide()
    {
        state = "to_hidden";
    }

    public void show()
    {
        state = "to_shown";
    }

    public void set_name_(string s)
    {
        name_.GetComponent<StringControl>().s = s;
    }

    public void set_stat(int num, int denom)
    {
        stat.GetComponent<StringControl>().s = num + "/" + denom;
    }
}
