using UnityEngine;

public class inputactive : MonoBehaviour {
    public string state = "unselected";
    private string prev_state = "none";
    [SerializeField] private GameObject w1;

    void Start() {
        if (state == "unselected") {
            w1.GetComponent<WingScript>().set_inactive();
            GetComponent<InputControl>().is_selected = false;
        } else {
            w1.GetComponent<WingScript>().set_active();
            GetComponent<InputControl>().is_selected = true;
        }
    }

    void Update() {
        if (prev_state != state) {
            if (state == "unselected") {
                w1.GetComponent<WingScript>().set_inactive();
                GetComponent<InputControl>().is_selected = false;
            } else if (state == "selected") {
                GetComponent<InputControl>().is_selected = true;
                w1.GetComponent<WingScript>().set_active();
            }
        }
        prev_state = state;   
    }
}
