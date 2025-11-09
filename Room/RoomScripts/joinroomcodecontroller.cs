using UnityEngine;

public class joinroomcodecontroller : MonoBehaviour {

    public GameObject ti1, b2;
    int ci = 0;

    void Start()
    {
        unselect_all();
        select(0);
    }

    public void unselect_all() {
        ti1.GetComponent<inputactive>().state = "unselected";
        b2.GetComponent<button>().state = "unselected";
    }

    public void select(int i)
    {
        if (i == 0)
        {
            ti1.GetComponent<inputactive>().state = "selected";
        }
        else if (i == 1)
        {
            b2.GetComponent<button>().state = "selected";
        }
        ci = i;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            unselect_all();
            ci = (ci + 1) % 2;
            select(ci);
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            unselect_all();
            ci = (ci + 1) % 2;
            select(ci);
        }
        else if (Input.GetKeyDown(KeyCode.X) && ci == 1)
        {
            b2.GetComponent<button>().go();
        }
        else if (Input.GetKeyDown(KeyCode.Z) && ci == 1)
        {
            TransitionService.LoadScene("PlaySelect");
        }
    }
}
