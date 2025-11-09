using UnityEngine;

public class AccountMenuController : MonoBehaviour {

    public GameObject ti1, ti2, b3;
    int ci = 0;

    void Start()
    {
        unselect_all();
        select(0);
    }

    public void unselect_all() {
        ti1.GetComponent<inputactive>().state = "unselected";
        ti2.GetComponent<inputactive>().state = "unselected";
        b3.GetComponent<button>().state = "unselected";
    }

    public void select(int i)
    {
        if (i == 0)
        {
            ti1.GetComponent<inputactive>().state = "selected";
        }
        else if (i == 1)
        {
            ti2.GetComponent<inputactive>().state = "selected";
        }
        else if (i == 2)
        {
            b3.GetComponent<button>().state = "selected";
        }
        ci = i;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            unselect_all();
            ci = (ci + 1) % 3;
            select(ci);
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            unselect_all();
            ci = (ci + 2) % 3;
            select(ci);
        }
        else if (Input.GetKeyDown(KeyCode.X) && ci == 2)
        {
            b3.GetComponent<button>().go();
        }
        else if (Input.GetKeyDown(KeyCode.Z) && ci == 2)
        {
            TransitionService.LoadScene("StartMenu");
        }
    }
}
